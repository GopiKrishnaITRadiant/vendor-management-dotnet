using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Application.Features.Auth.Dtos;
using VendorManagement.Domain.Entities;
using VendorManagement.Domain.Enums;
using VendorManagement.Application.Common.Exceptions;

namespace VendorManagement.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService _jwtService;
    private readonly IMailService _mailService;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    private const int OtpResendCooldownSeconds = 60;
    private const int OtpMaxAttempts = 5;
    private const int OtpExpiryMinutes = 10;

    public AuthService(
        IUserRepository userRepo,
        IJwtTokenService jwtService,
        IMailService mailService,
        IPasswordHasher hasher,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _mailService = mailService;
        _hasher = hasher;
        _config = config;
    }

    public async Task<object> LoginAsync(LoginDto dto, string ip)
    {
        var user = await _userRepo.GetByEmailWithPasswordAsync(dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Invalid email");

        if (user.IsLocked())
        {
            var wait = Math.Ceiling((user.LockedUntil!.Value - DateTime.UtcNow).TotalSeconds);
            throw new ForbiddenException($"Account locked. Try again in {wait}s.");
        }

        if (!_hasher.Verify(dto.Password, user.Password))
        {
            await RecordFailedAttemptAsync(user);
            throw new UnauthorizedAccessException("Invalid password");
        }

        ValidateAccountStatus(user);

        if (user.IsTwoFactorEnabled)
            return await StartOtpChallengeAsync(user.Id, user.Email, user.FirstName);

        await RecordSuccessfulLoginAsync(user.Id, ip);
        var tokens = await IssueTokensAsync(user);
        return new LoginResponseDto(tokens.AccessToken, tokens.RefreshToken, ToAuthUser(user));
    }

    public async Task<object> SendOtpAsync(SendOtpDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLower())
            ?? throw new BadRequestException("No account found with this email.");

        if (user.Status == UserStatus.Suspended)
            throw new ForbiddenException("Account suspended. Contact support.");

        if (user.IsLocked())
        {
            var wait = Math.Ceiling((user.LockedUntil!.Value - DateTime.UtcNow).TotalSeconds);
            throw new ForbiddenException($"Account locked. Try again in {wait}s.");
        }

        return await StartOtpChallengeAsync(user.Id, user.Email, user.FirstName);
    }

    public async Task<LoginResponseDto> VerifyOtpAsync(VerifyOtpDto dto, string ip)
    {
        var userId = VerifyOtpToken(dto.OtpToken);
        await VerifyOtpCodeAsync(userId, dto.Otp);

        var user = await _userRepo.GetByIdWithRoleAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found");

        if (user.Status is UserStatus.Suspended)
            throw new ForbiddenException("Account suspended. Contact support.");
        if (user.Status is UserStatus.Inactive)
            throw new ForbiddenException("Account is inactive.");

        if (user.Status == UserStatus.PendingVerification)
        {
            user.Status = UserStatus.Active;
            user.IsEmailVerified = true;
            await _userRepo.UpdateAsync(user);
        }

        await RecordSuccessfulLoginAsync(userId, ip);
        var tokens = await IssueTokensAsync(user);
        return new LoginResponseDto(tokens.AccessToken, tokens.RefreshToken, ToAuthUser(user));
    }

    public async Task<object> ResendOtpAsync(ResendOtpDto dto)
    {
        var userId = VerifyOtpToken(dto.OtpToken);
        var user = await _userRepo.GetByIdWithRoleAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found");

        if (user.IsLocked())
        {
            var wait = Math.Ceiling((user.LockedUntil!.Value - DateTime.UtcNow).TotalSeconds);
            throw new ForbiddenException($"Account locked. Try again in {wait}s.");
        }

        return await StartOtpChallengeAsync(userId, user.Email, user.FirstName);
    }

    public async Task LogoutAsync(int userId) =>
        await _userRepo.UpdateRefreshTokenHashAsync(userId, string.Empty);

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLower());
        if (user is null) return; // don't reveal existence

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _userRepo.UpdateAsync(user);

        await _mailService.SendPasswordResetAsync(user.Email, user.FirstName, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userRepo.GetByPasswordResetTokenAsync(dto.Token)
            ?? throw new BadRequestException("Invalid or expired reset token.");

        if (user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Reset token expired. Request a new one.");

        user.Password = _hasher.Hash(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.RefreshTokenHash = string.Empty;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        await _userRepo.UpdateAsync(user);
    }

    public async Task<LoginResponseDto> RefreshAsync(string refreshToken)
    {
        var principal = _jwtService.ValidateRefreshToken(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepo.GetByIdWithRefreshHashAsync(userId);

        if (user?.RefreshTokenHash is null || user.RefreshTokenHash == string.Empty)
            throw new UnauthorizedAccessException("Session not found.");

        if (!_hasher.Verify(refreshToken, user.RefreshTokenHash))
        {
            await _userRepo.UpdateRefreshTokenHashAsync(user.Id, string.Empty);
            throw new UnauthorizedAccessException("Refresh token reuse detected. All sessions revoked.");
        }

        var tokens = await IssueTokensAsync(user);
        return new LoginResponseDto(tokens.AccessToken, tokens.RefreshToken, ToAuthUser(user));
    }

    public async Task<object> UpdateTwoFactorAsync(int userId, bool enabled)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found");

        user.IsTwoFactorEnabled = enabled;
        await _userRepo.UpdateAsync(user);

        return new
        {
            message = enabled
                ? "Two-factor authentication enabled successfully"
                : "Two-factor authentication disabled successfully",
            isTwoFactorEnabled = user.IsTwoFactorEnabled
        };
    }

    // ---- Private helpers ----
    private void ValidateAccountStatus(User user)
    {
        if (user.Status == UserStatus.Suspended)
            throw new ForbiddenException("Account suspended. Contact support.");
        if (user.Status == UserStatus.Inactive)
            throw new ForbiddenException("Account is inactive.");
        if (user.Status == UserStatus.PendingVerification)
            throw new ForbiddenException("Account setup is not complete. Contact your administrator.");
    }

    private async Task<OtpRequiredResponseDto> StartOtpChallengeAsync(int userId, string email, string firstName)
    {
        await EnforceResendCooldownAsync(userId);
        var otp = await GenerateAndStoreOtpAsync(userId);
        await _mailService.SendOtpAsync(email, firstName, otp, OtpExpiryMinutes);

        return new OtpRequiredResponseDto(
            true,
            SignOtpToken(userId),
            $"A one-time password has been sent to {MaskEmail(email)}");
    }

    private async Task<string> GenerateAndStoreOtpAsync(int userId)
    {
        var otp = Random.Shared.Next(100_000, 999_999).ToString();
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("User not found");

        user.Otp = _hasher.Hash(otp);
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        user.OtpAttempts = 0;
        await _userRepo.UpdateAsync(user);

        return otp;
    }

    private async Task VerifyOtpCodeAsync(int userId, string submittedOtp)
    {
        var user = await _userRepo.GetByIdWithOtpAsync(userId)
            ?? throw new BadRequestException("No OTP found. Please request a new one.");

        if (string.IsNullOrEmpty(user.Otp))
            throw new BadRequestException("No OTP found. Please request a new one.");

        if (user.OtpAttempts >= OtpMaxAttempts)
            throw new ForbiddenException("Too many incorrect attempts. Please request a new OTP.");

        if (user.OtpExpiresAt is null || user.OtpExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("OTP expired. Please request a new one.");

        if (!_hasher.Verify(submittedOtp, user.Otp))
        {
            user.OtpAttempts += 1;
            await _userRepo.UpdateAsync(user);
            var remaining = OtpMaxAttempts - user.OtpAttempts;
            throw new UnauthorizedAccessException($"Incorrect OTP. {remaining} attempt(s) remaining.");
        }

        user.Otp = null;
        user.OtpExpiresAt = null;
        user.OtpAttempts = 0;
        await _userRepo.UpdateAsync(user);
    }

    private async Task EnforceResendCooldownAsync(int userId)
    {
        var user = await _userRepo.GetByIdWithOtpAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found");

        if (user.OtpExpiresAt is null) return;

        var issuedAt = user.OtpExpiresAt.Value.AddMinutes(-OtpExpiryMinutes);
        var secondsSinceIssue = (DateTime.UtcNow - issuedAt).TotalSeconds;

        if (secondsSinceIssue < OtpResendCooldownSeconds)
        {
            var wait = Math.Ceiling(OtpResendCooldownSeconds - secondsSinceIssue);
            throw new BadRequestException($"Please wait {wait}s before requesting a new OTP.");
        }
    }

    private async Task<AuthTokensDto> IssueTokensAsync(User user)
    {
        var userWithPerms = await _userRepo.GetByIdWithRoleAndPermissionsAsync(user.Id);
        var permissions = userWithPerms?.Role?.Permissions?.Select(p => p.Name).ToList() ?? new List<string>();

        var accessToken = _jwtService.GenerateAccessToken(user, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

        await _userRepo.UpdateRefreshTokenHashAsync(user.Id, _hasher.Hash(refreshToken));

        return new AuthTokensDto(accessToken, refreshToken);
    }

    private string SignOtpToken(int userId) => _jwtService.GenerateOtpToken(userId);

    private int VerifyOtpToken(string token)
    {
        var userId = _jwtService.ValidateOtpToken(token);
        return userId ?? throw new UnauthorizedAccessException("OTP session expired. Please request a new OTP.");
    }

    private async Task RecordFailedAttemptAsync(User user)
    {
        user.FailedLoginAttempts += 1;
        if (user.FailedLoginAttempts >= 5)
            user.LockedUntil = DateTime.UtcNow.AddMinutes(15);

        await _userRepo.UpdateAsync(user);
    }

    private async Task RecordSuccessfulLoginAsync(int userId, string ip)
    {
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new NotFoundException("User not found");
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ip;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await _userRepo.UpdateAsync(user);
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        return $"{parts[0][..Math.Min(2, parts[0].Length)]}***@{parts[1]}";
    }

    private static AuthUserDto ToAuthUser(User user) => new(
        user.Id, user.FullName, user.Email,
        new RoleDto(user.Role.Id, user.Role.Name),
        user.Status.ToString(), user.IsLocked(),
        user.IsEmailVerified, user.IsTwoFactorEnabled, user.SapVendorId);
}