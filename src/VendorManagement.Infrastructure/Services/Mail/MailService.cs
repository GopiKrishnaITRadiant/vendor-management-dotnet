using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using VendorManagement.Application.Common.Interfaces;

namespace VendorManagement.Infrastructure.Services.Mail;

public class MailService : IMailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MailService> _logger;
    private readonly string _frontendUrl;

    public MailService(IConfiguration config, ILogger<MailService> logger)
    {
        _config = config;
        _logger = logger;
        _frontendUrl = _config["Frontend:Url"] ?? "http://localhost:8000";
    }

    public async Task SendOtpAsync(string email, string firstName, string otp, int expiryMinutes)
    {
        var html = MailTemplates.Otp(firstName, otp, _frontendUrl, expiryMinutes);
        await SendAsync(email, "Your One-Time Password", html);
    }

    public async Task SendPasswordResetAsync(string email, string firstName, string token)
    {
        var resetLink = $"{_frontendUrl}/reset-password?token={token}";
        var html = MailTemplates.PasswordReset(firstName, resetLink, _frontendUrl);
        await SendAsync(email, "Reset Your Password", html);
    }

    public async Task SendVendorCredentialsAsync(string email, string sapVendorId, string password)
    {
        var loginLink = $"{_frontendUrl}/login";
        var html = MailTemplates.VendorCredentialsReady(sapVendorId, email, password, loginLink, _frontendUrl);
        await SendAsync(email, "Your Vendor Management Account is Ready", html);
    }

    private async Task SendAsync(string to, string subject, string html)
    {
        try
        {
            var host = _config["Smtp:Host"]
                ?? throw new InvalidOperationException("Smtp:Host is not configured");
            var username = _config["Smtp:Username"]
                ?? throw new InvalidOperationException("Smtp:Username is not configured");
            var password = _config["Smtp:Password"]
                ?? throw new InvalidOperationException("Smtp:Password is not configured");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["Smtp:FromAddress"] ?? "no-reply@vendormanagement.com"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                host,
                int.Parse(_config["Smtp:Port"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }
}