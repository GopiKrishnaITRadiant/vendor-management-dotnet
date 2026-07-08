using VendorManagement.Application.Features.Users.Dtos;
using VendorManagement.Domain.Common;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdWithRoleAsync(int id);
    Task<User?> GetByIdWithPasswordAsync(int id);
    Task<User?> GetByIdWithOtpAsync(int id);
    Task<User?> GetByIdWithRefreshHashAsync(int id);
    Task<User?> GetByIdWithRoleAndPermissionsAsync(int id);

    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailWithPasswordAsync(string email);
    Task<User?> GetByPasswordResetTokenAsync(string token);

    Task UpdateRefreshTokenHashAsync(int id, string hash);

    Task<PaginatedUsersDto> FindAllAsync(FindUsersQuery query);
    Task<PaginatedUsersDto> SearchAsync(FindUsersQuery query);
    Task<object> GetStatusCountsAsync(string? roleName);
}