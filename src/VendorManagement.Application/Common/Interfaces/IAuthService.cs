using VendorManagement.Application.Features.Auth.Dtos;

namespace VendorManagement.Application.Common.Interfaces;

public interface IAuthService
{
    Task<object> LoginAsync(LoginDto dto, string ip);
    Task<object> SendOtpAsync(SendOtpDto dto);
    Task<LoginResponseDto> VerifyOtpAsync(VerifyOtpDto dto, string ip);
    Task<object> ResendOtpAsync(ResendOtpDto dto);
    Task LogoutAsync(int userId);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task<LoginResponseDto> RefreshAsync(string refreshToken);
    Task<object> UpdateTwoFactorAsync(int userId, bool enabled);
}