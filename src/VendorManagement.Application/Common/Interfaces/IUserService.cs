using VendorManagement.Application.Features.Users.Dtos;

namespace VendorManagement.Application.Common.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);
    Task<PaginatedUsersDto> FindAllAsync(FindUsersQuery query);
    Task<UserResponseDto> FindByIdAsync(int id);
    Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto, int requesterId, string requesterRole);
    Task ChangePasswordAsync(int id, ChangePasswordDto dto);
    Task RemoveAsync(int id);
    Task<object> GetStatusCountsAsync(string? roleName);
    Task<UserResponseDto> SetupVendorAsync(int id, SetupVendorDto dto);
    Task ResendVerificationAsync(string email);
}