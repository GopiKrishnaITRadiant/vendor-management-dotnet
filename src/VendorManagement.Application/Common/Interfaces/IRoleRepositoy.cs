using VendorManagement.Domain.Common;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Application.Common.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByIdWithPermissionsAsync(int id);
    Task<Role?> GetByNameAsync(string name);
}