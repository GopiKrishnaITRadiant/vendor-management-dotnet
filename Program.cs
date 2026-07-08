using Microsoft.EntityFrameworkCore;
using VendorManagement.Infrastructure;
using VendorManagement.Infrastructure.Persistence;
using VendorManagement.Infrastructure.Persistence.Seed;
using VendorManagement.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Apply migrations and check database connection
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Applies pending migrations and creates the database if it doesn't exist
        await dbContext.Database.MigrateAsync();

        Console.WriteLine("✅ Database connected successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed.");
        Console.WriteLine(ex);
        throw;
    }
}

// Seed database only when running:
// dotnet run -- --seed
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

    await seeder.SeedAsync();

    Console.WriteLine("✅ Database seeding completed.");

    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();