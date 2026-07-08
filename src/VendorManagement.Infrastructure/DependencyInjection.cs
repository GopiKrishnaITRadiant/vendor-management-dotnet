using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Application.Features.Auth;
using VendorManagement.Application.Features.Users;
using VendorManagement.Domain.Common;
using VendorManagement.Infrastructure.Persistence;
using VendorManagement.Infrastructure.Persistence.Repositories;
using VendorManagement.Infrastructure.Persistence.Seed;
using VendorManagement.Infrastructure.Services;
using VendorManagement.Infrastructure.Services.Mail;

namespace VendorManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Generic repository (covers Permission and anything else with no special needs)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Feature-specific repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();


        // Seeder
        services.AddScoped<DatabaseSeeder>();
        services.AddHttpContextAccessor();

        // TODO: register these once you've written their implementations
        services.AddScoped<IPasswordHasher, PasswordHasher>();
         services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}