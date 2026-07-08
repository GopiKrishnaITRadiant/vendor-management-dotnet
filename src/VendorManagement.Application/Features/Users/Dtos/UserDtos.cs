using VendorManagement.Domain.Enums;

namespace VendorManagement.Application.Features.Users.Dtos;

public record CreateUserDto(
    string FirstName, string LastName, string Email,
    string Password, int RoleId, string? SapVendorId, UserStatus? Status);

public record UpdateUserDto(
    string? FirstName, string? LastName, string? Email,
    int? RoleId, UserStatus? Status);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);

public record SetupVendorDto(string Email, string Password);

public record UserResponseDto(
    int Id, string FirstName, string LastName, string FullName,
    string Email, string RoleName, UserStatus Status,
    bool IsEmailVerified, bool IsTwoFactorEnabled, string? SapVendorId,
    DateTime CreatedAt);

public record FindUsersQuery(
    int Page = 1, int Limit = 20, string? Search = null,
    int? Role = null, UserStatus? Status = null, string? ViewType = null);

public record PaginatedUsersDto(
    List<UserResponseDto> Data, int Total, int Page, int Limit, int TotalPages);