using Microsoft.EntityFrameworkCore;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Application.Features.Users.Dtos;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByIdWithRoleAsync(int id) =>
        await _dbSet.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByIdWithPasswordAsync(int id) =>
        await _dbSet.IgnoreQueryFilters()
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync();

    public async Task<User?> GetByIdWithOtpAsync(int id) =>
        await _dbSet.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByIdWithRefreshHashAsync(int id) =>
        await _dbSet.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByIdWithRoleAndPermissionsAsync(int id) =>
        await _dbSet
            .Include(u => u.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _dbSet.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByEmailWithPasswordAsync(string email) =>
        await _dbSet.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByPasswordResetTokenAsync(string token) =>
        await _dbSet.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

    public async Task UpdateRefreshTokenHashAsync(int id, string hash)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return;
        user.RefreshTokenHash = hash;
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedUsersDto> FindAllAsync(FindUsersQuery query)
    {
        var qb = _dbSet.Include(u => u.Role).AsQueryable();

        if (query.Role is not null)
            qb = qb.Where(u => u.RoleId == query.Role);

        if (query.Status is not null)
            qb = qb.Where(u => u.Status == query.Status);

        if (query.ViewType == "vendor")
            qb = qb.Where(u => u.Role.Name == "vendor");

        var total = await qb.CountAsync();

        var users = await qb
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync();

        return MapToPaginated(users, total, query.Page, query.Limit);
    }

    public async Task<PaginatedUsersDto> SearchAsync(FindUsersQuery query)
    {
        var search = query.Search!;
        var qb = _dbSet.Include(u => u.Role)
            .Where(u =>
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search));

        if (query.Role is not null)
            qb = qb.Where(u => u.RoleId == query.Role);

        if (query.Status is not null)
            qb = qb.Where(u => u.Status == query.Status);

        var total = await qb.CountAsync();

        var users = await qb
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync();

        return MapToPaginated(users, total, query.Page, query.Limit);
    }

    public async Task<object> GetStatusCountsAsync(string? roleName)
    {
        var qb = _dbSet.Include(u => u.Role).AsQueryable();
        if (roleName is not null)
            qb = qb.Where(u => u.Role.Name == roleName);

        var total = await qb.CountAsync();
        var admins = await _dbSet.CountAsync(u => u.Role.Name == "admin");
        var vendors = await _dbSet.CountAsync(u => u.Role.Name == "vendor");
        var active = await qb.CountAsync(u => u.Status == Domain.Enums.UserStatus.Active);
        var inactive = await qb.CountAsync(u => u.Status == Domain.Enums.UserStatus.Inactive);
        var suspended = await qb.CountAsync(u => u.Status == Domain.Enums.UserStatus.Suspended);
        var pending = await qb.CountAsync(u => u.Status == Domain.Enums.UserStatus.PendingVerification);

        return new { total, admins, vendors, active, inactive, suspended, pending };
    }

    private static PaginatedUsersDto MapToPaginated(List<User> users, int total, int page, int limit)
    {
        var data = users.Select(u => new UserResponseDto(
            u.Id, u.FirstName, u.LastName, u.FullName, u.Email,
            u.Role?.Name ?? string.Empty, u.Status, u.IsEmailVerified,
            u.IsTwoFactorEnabled, u.SapVendorId, u.CreatedAt)).ToList();

        return new PaginatedUsersDto(data, total, page, limit, (int)Math.Ceiling(total / (double)limit));
    }
}