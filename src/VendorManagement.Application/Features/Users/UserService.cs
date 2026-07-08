using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Application.Features.Users.Dtos;
using VendorManagement.Domain.Entities;
using VendorManagement.Domain.Enums;
using VendorManagement.Application.Common.Exceptions;

namespace VendorManagement.Application.Features.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IMailService _mailService;
    private readonly IPasswordHasher _hasher;

    public UserService(
        IUserRepository userRepo, IRoleRepository roleRepo,
        IMailService mailService, IPasswordHasher hasher)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _mailService = mailService;
        _hasher = hasher;
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        if (await _userRepo.GetByEmailAsync(dto.Email.ToLower()) is not null)
            throw new ConflictException("Email already registered");

        var role = await _roleRepo.GetByIdAsync(dto.RoleId)
            ?? throw new NotFoundException("Role not found");

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email.ToLower(),
            Password = _hasher.Hash(dto.Password),
            RoleId = role.Id,
            Role = role,
            SapVendorId = dto.SapVendorId,
            Status = dto.Status ?? UserStatus.PendingVerification,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var saved = await _userRepo.AddAsync(user);

        await _mailService.SendVendorCredentialsAsync(
            dto.Email.ToLower(), dto.SapVendorId ?? "—", dto.Password);

        return ToResponse(saved);
    }

    public async Task<PaginatedUsersDto> FindAllAsync(FindUsersQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
            return await _userRepo.SearchAsync(query);

        return await _userRepo.FindAllAsync(query);
    }

    public async Task<UserResponseDto> FindByIdAsync(int id)
    {
        var user = await _userRepo.GetByIdWithRoleAsync(id)
            ?? throw new NotFoundException("User not found");
        return ToResponse(user);
    }

    public async Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto, int requesterId, string requesterRole)
    {
        var user = await _userRepo.GetByIdWithRoleAsync(id)
            ?? throw new NotFoundException("User not found");

        if (requesterRole == "vendor" && user.Id != requesterId)
            throw new BadRequestException("You dont have permission.");

        if (dto.FirstName is not null) user.FirstName = dto.FirstName;
        if (dto.LastName is not null) user.LastName = dto.LastName;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.RoleId is not null)
        {
            var role = await _roleRepo.GetByIdAsync(dto.RoleId.Value)
                ?? throw new NotFoundException("Role not found");
            user.RoleId = role.Id;
            user.Role = role;
        }
        if (dto.Status is not null) user.Status = dto.Status.Value;

        await _userRepo.UpdateAsync(user);
        return ToResponse(user);
    }

    public async Task ChangePasswordAsync(int id, ChangePasswordDto dto)
    {
        var user = await _userRepo.GetByIdWithPasswordAsync(id)
            ?? throw new NotFoundException("User not found");

        if (!_hasher.Verify(dto.CurrentPassword, user.Password))
            throw new BadRequestException("Current password is incorrect");

        if (dto.CurrentPassword == dto.NewPassword)
            throw new BadRequestException("New password must differ from the current password");

        user.Password = _hasher.Hash(dto.NewPassword);
        user.RefreshTokenHash = string.Empty;
        await _userRepo.UpdateAsync(user);
    }

    public async Task RemoveAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("User not found");
        await _userRepo.SoftDeleteAsync(user);
    }

    public async Task<object> GetStatusCountsAsync(string? roleName) =>
        await _userRepo.GetStatusCountsAsync(roleName);

    public async Task<UserResponseDto> SetupVendorAsync(int id, SetupVendorDto dto)
    {
        var user = await _userRepo.GetByIdWithRoleAsync(id)
            ?? throw new NotFoundException("User not found");

        if (user.Role?.Name != "vendor")
            throw new BadRequestException("This endpoint is only for vendor accounts");

        var existing = await _userRepo.GetByEmailAsync(dto.Email.ToLower());
        if (existing is not null && existing.Id != id)
            throw new ConflictException("Email already in use by another account");

        user.Email = dto.Email.ToLower();
        user.Password = _hasher.Hash(dto.Password);
        user.Status = UserStatus.Active;
        user.IsFirstLoginVerified = false;

        await _userRepo.UpdateAsync(user);
        await _mailService.SendVendorCredentialsAsync(user.Email, user.SapVendorId ?? "—", dto.Password);

        return ToResponse(user);
    }

    public async Task ResendVerificationAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLower());
        if (user is null) return; // silent

        if (user.IsEmailVerified)
            throw new BadRequestException("Email already verified");

        user.EmailVerificationToken = Guid.NewGuid().ToString("N");
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await _userRepo.UpdateAsync(user);
    }

    private static UserResponseDto ToResponse(User user) => new(
        user.Id, user.FirstName, user.LastName, user.FullName,
        user.Email, user.Role?.Name ?? string.Empty, user.Status,
        user.IsEmailVerified, user.IsTwoFactorEnabled, user.SapVendorId,
        user.CreatedAt);
}