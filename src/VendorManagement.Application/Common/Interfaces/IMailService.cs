namespace VendorManagement.Application.Common.Interfaces;

public interface IMailService
{
    Task SendOtpAsync(string email, string firstName, string otp, int expiryMinutes);
    Task SendPasswordResetAsync(string email, string firstName, string token);
    Task SendVendorCredentialsAsync(string email, string sapVendorId, string password);
}