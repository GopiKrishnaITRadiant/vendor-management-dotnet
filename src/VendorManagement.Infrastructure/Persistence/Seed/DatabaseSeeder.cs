using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Domain.Entities;
using VendorManagement.Domain.Enums;

namespace VendorManagement.Infrastructure.Persistence.Seed;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher hasher,
        IConfiguration config,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _hasher = hasher;
        _config = config;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedPermissionsAsync();
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        _logger.LogInformation("Seed complete");
    }

    // 1. Upsert permissions
    private async Task SeedPermissionsAsync()
    {
        _logger.LogInformation("Seeding permissions...");

        foreach (var (name, description) in PermissionsSeedData.Permissions)
        {
            var exists = await _context.Permissions.AnyAsync(p => p.Name == name);
            if (!exists)
            {
                _context.Permissions.Add(new Permission { Name = name, Description = description });
                _logger.LogInformation("  + {Name}", name);
            }
            else
            {
                _logger.LogInformation("  ~ {Name} (already exists)", name);
            }
        }

        await _context.SaveChangesAsync();
    }

    // 2. Upsert roles with permissions
    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var allPermissions = await _context.Permissions.ToListAsync();
        var permissionMap = allPermissions.ToDictionary(p => p.Name, p => p);

        foreach (var (roleName, permissionNames) in PermissionsSeedData.RolePermissions)
        {
            var permissions = permissionNames
                .Where(name => permissionMap.ContainsKey(name))
                .Select(name => permissionMap[name])
                .ToList();

            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role is null)
            {
                role = new Role { Name = roleName, Permissions = permissions };
                _context.Roles.Add(role);
                _logger.LogInformation("  role: {Role} ({Count} permissions)", roleName, permissions.Count);
            }
            else
            {
                role.Permissions = permissions;
                _logger.LogInformation("  role: {Role} updated ({Count} permissions)", roleName, permissions.Count);
            }
        }

        await _context.SaveChangesAsync();
    }

    // 3. Seed super-admin user
    private async Task SeedAdminUserAsync()
    {
        _logger.LogInformation("Seeding admin user...");

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
        if (adminRole is null)
            throw new InvalidOperationException("Admin role not found after seeding");

        var adminEmail = _config["Seed:AdminEmail"] ?? "admin@vendormanagement.com";
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existing is null)
        {
            var password = _config["Seed:AdminPassword"] ?? "Admin@1234";

            var admin = new User
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = adminEmail,
                Password = _hasher.Hash(password),
                RoleId = adminRole.Id,
                Role = adminRole,
                Status = UserStatus.Active,
                IsEmailVerified = true,
                IsFirstLoginVerified = true
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            _logger.LogInformation("  admin user created: {Email}", adminEmail);
        }
        else
        {
            _logger.LogInformation("  admin user already exists: {Email}", adminEmail);
        }
    }
}