using VendorManagement.Application.Common.Interfaces;

namespace VendorManagement.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string value) =>
        BCrypt.Net.BCrypt.HashPassword(value, workFactor: 12);

    public bool Verify(string plain, string hash) =>
        BCrypt.Net.BCrypt.Verify(plain, hash);
}