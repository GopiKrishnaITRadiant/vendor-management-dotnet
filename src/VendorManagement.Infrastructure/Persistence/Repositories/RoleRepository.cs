using Microsoft.EntityFrameworkCore;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Infrastructure.Persistence.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Role?> GetByIdWithPermissionsAsync(int id) =>
        await _dbSet
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Role?> GetByNameAsync(string name) =>
        await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
}