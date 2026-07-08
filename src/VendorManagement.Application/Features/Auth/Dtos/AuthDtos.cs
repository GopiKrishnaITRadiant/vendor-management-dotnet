namespace VendorManagement.Application.Features.Auth.Dtos;

public record LoginDto(string Email, string Password);
public record SendOtpDto(string Email);
public record VerifyOtpDto(string OtpToken, string Otp);
public record ResendOtpDto(string OtpToken);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Token, string NewPassword);
public record RefreshTokenDto(string RefreshToken);
public record UpdateTwoFactorDto(bool Enabled);

public record AuthTokensDto(string AccessToken, string RefreshToken);

public record AuthUserDto(
    int Id, string FullName, string Email,
    RoleDto Role, string Status, bool IsLocked,
    bool IsEmailVerified, bool IsTwoFactorEnabled, string? SapVendorId);

public record RoleDto(int Id, string Name);

public record LoginResponseDto(string AccessToken, string RefreshToken, AuthUserDto User);

public record OtpRequiredResponseDto(bool RequiresOtp, string OtpToken, string Message);