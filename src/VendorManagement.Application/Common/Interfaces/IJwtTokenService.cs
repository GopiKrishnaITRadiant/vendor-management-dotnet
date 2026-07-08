using System.Security.Claims;
using VendorManagement.Domain.Entities;

namespace VendorManagement.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, List<string> permissions);
    string GenerateRefreshToken(int userId);
    string GenerateOtpToken(int userId);
    int? ValidateOtpToken(string token);
    ClaimsPrincipal? ValidateRefreshToken(string token);
}