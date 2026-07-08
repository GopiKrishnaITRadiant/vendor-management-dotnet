using VendorManagement.Domain.Common;
using VendorManagement.Domain.Enums;

namespace VendorManagement.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // never selected by default — enforce via query projection

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public bool IsEmailVerified { get; set; }

    public string? SapVendorId { get; set; }

    // OTP / 2FA
    public string? Otp { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public int OtpAttempts { get; set; }
    public bool IsFirstLoginVerified { get; set; }
    public bool IsTwoFactorEnabled { get; set; } = true;

    // Password reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // Email verification
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    // Lockout
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    public string? RefreshTokenHash { get; set; }

    // Helpers
    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    public bool IsActive() => Status == UserStatus.Active;
    public string FullName => $"{FirstName} {LastName}";
}