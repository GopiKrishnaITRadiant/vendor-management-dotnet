namespace VendorManagement.Infrastructure.Persistence.Seed;

public static class PermissionsSeedData
{
    public static readonly List<(string Name, string Description)> Permissions = new()
    {
        ("users.create", "Create users"),
        ("users.read", "View users"),
        ("users.update", "Update users"),
        ("users.delete", "Delete users"),
        ("roles.manage", "Manage roles and permissions"),
        ("vendors.manage", "Manage vendor accounts"),
        // Add the rest of your actual permissions here
    };

    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        ["admin"] = new[]
        {
            "users.create", "users.read", "users.update", "users.delete",
            "roles.manage", "vendors.manage"
        },
        ["vendor"] = new[]
        {
            "users.read"
        }
        // Add other roles here
    };
}