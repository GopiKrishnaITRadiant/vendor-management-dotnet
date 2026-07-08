using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VendorManagement.Application.Common.Interfaces;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(User user, List<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        return GenerateToken(claims, _config["Jwt:AccessSecret"]!, 15);
    }

    public string GenerateRefreshToken(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        return GenerateToken(claims, _config["Jwt:RefreshSecret"]!, 60 * 24 * 7); // 7 days
    }

    public string GenerateOtpToken(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("type", "otp_pending")
        };
        return GenerateToken(claims, _config["Jwt:OtpSecret"]!, 15);
    }

    public int? ValidateOtpToken(string token)
    {
        var principal = ValidateToken(token, _config["Jwt:OtpSecret"]!);
        if (principal is null) return null;

        var type = principal.FindFirstValue("type");
        if (type != "otp_pending") return null;

        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return idClaim is not null ? int.Parse(idClaim) : null;
    }

    public ClaimsPrincipal? ValidateRefreshToken(string token) =>
        ValidateToken(token, _config["Jwt:RefreshSecret"]!);

    private static string GenerateToken(List<Claim> claims, string secret, int expiryMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ClaimsPrincipal? ValidateToken(string token, string secret)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}