using System.Globalization;

namespace VendorManagement.Infrastructure.Services.Mail;

public static class MailTemplates
{
    private const string Primary = "#1a56db";
    private const string Success = "#16a34a";
    private const string Warning = "#f97316";
    private const string Danger = "#dc2626";
    private const string Text = "#111827";
    private const string TextMuted = "#6b7280";
    private const string TextLight = "#9ca3af";
    private const string Bg = "#f4f6f9";
    private const string Surface = "#ffffff";
    private const string SurfaceAlt = "#f8fafc";
    private const string SurfaceBlue = "#eff6ff";
    private const string Border = "#e5e7eb";
    private const string Mono = "'Courier New', Courier, monospace";
    private const string Font = "'Segoe UI', Arial, Helvetica, sans-serif";

    private static string Layout(string title, string body, string frontendUrl) => $@"
    <!DOCTYPE html>
    <html lang=""en"">
    <head>
      <meta charset=""UTF-8""/>
      <meta name=""viewport"" content=""width=device-width,initial-scale=1.0""/>
      <title>{title}</title>
    </head>
    <body style=""margin:0;padding:0;background:{Bg};font-family:{Font};"">
      <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:{Bg};padding:40px 16px;"">
        <tr><td align=""center"">
          <table width=""600"" cellpadding=""0"" cellspacing=""0""
                style=""background:{Surface};border-radius:10px;box-shadow:0 2px 12px rgba(0,0,0,0.08);max-width:600px;width:100%;overflow:hidden;"">
            <tr>
              <td style=""background:{Primary};padding:28px 40px;text-align:center;"">
                <p style=""margin:0 0 4px;color:rgba(255,255,255,0.75);font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;"">Vendor Management Portal</p>
                <h1 style=""margin:0;color:#ffffff;font-size:22px;font-weight:700;letter-spacing:0.3px;"">{title}</h1>
              </td>
            </tr>
            <tr>
              <td style=""padding:40px 40px 32px;"">
                {body}
              </td>
            </tr>
            <tr>
              <td style=""background:{SurfaceAlt};padding:20px 40px;border-top:1px solid {Border};text-align:center;"">
                <p style=""margin:0 0 4px;color:{TextLight};font-size:12px;"">This is an automated message — please do not reply directly.</p>
                <p style=""margin:0;color:{TextLight};font-size:12px;"">
                  <a href=""{frontendUrl}"" style=""color:{Primary};text-decoration:none;font-weight:500;"">Vendor Management Portal</a>
                  &nbsp;·&nbsp; © {DateTime.UtcNow.Year} All rights reserved.
                </p>
              </td>
            </tr>
          </table>
        </td></tr>
      </table>
    </body>
    </html>";

    private static string Heading(string text) =>
        $@"<h2 style=""margin:0 0 8px;font-size:20px;font-weight:700;color:{Text};line-height:1.3;"">{text}</h2>";

    private static string Subtext(string text) =>
        $@"<p style=""margin:0 0 24px;font-size:14px;color:{TextMuted};line-height:1.6;"">{text}</p>";

    private static string Para(string text) =>
        $@"<p style=""margin:0 0 16px;font-size:15px;color:#374151;line-height:1.7;"">{text}</p>";

    private static string Button(string label, string href, string color = Primary) => $@"
    <table cellpadding=""0"" cellspacing=""0"" style=""margin:24px 0;"">
      <tr>
        <td style=""background:{color};border-radius:6px;"">
          <a href=""{href}"" style=""display:inline-block;padding:13px 30px;color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;border-radius:6px;letter-spacing:0.2px;"">
            {label}
          </a>
        </td>
      </tr>
    </table>";

    private static string OtpBox(string otp) => $@"
    <div style=""text-align:center;margin:28px 0;"">
      <div style=""display:inline-block;background:{SurfaceBlue};border:2px dashed {Primary};border-radius:8px;padding:18px 48px;"">
        <p style=""margin:0 0 4px;font-size:11px;font-weight:600;color:{TextMuted};letter-spacing:2px;text-transform:uppercase;"">One-Time Password</p>
        <span style=""font-size:38px;font-weight:700;letter-spacing:12px;color:{Primary};font-family:{Mono};"">{otp}</span>
      </div>
    </div>";

    private static string InfoBox(params (string Label, string Value)[] rows)
    {
        var cells = string.Join("", rows.Select((r, i) => $@"
        <tr style=""background:{(i % 2 == 0 ? SurfaceAlt : Surface)};"">
          <td style=""padding:11px 16px;font-size:13px;color:{TextMuted};font-weight:600;width:42%;border-bottom:1px solid {Border};"">{r.Label}</td>
          <td style=""padding:11px 16px;font-size:13px;color:{Text};border-bottom:1px solid {Border};"">{r.Value}</td>
        </tr>"));

        return $@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid {Border};border-radius:7px;overflow:hidden;margin:20px 0;"">{cells}</table>";
    }

    private static string AlertBox(string text, string type = "warning")
    {
        var (bg, border, textColor, icon) = type switch
        {
            "danger" => ("#fef2f2", Danger, "#991b1b", "🚫"),
            "info" => (SurfaceBlue, Primary, "#1e40af", "ℹ️"),
            _ => ("#fff7ed", Warning, "#92400e", "⚠️"),
        };

        return $@"
        <div style=""background:{bg};border-left:4px solid {border};border-radius:4px;padding:12px 16px;margin:20px 0;"">
          <p style=""margin:0;font-size:13px;color:{textColor};line-height:1.6;"">{icon}&nbsp; {text}</p>
        </div>";
    }

    private static string FallbackLink(string href) => $@"
    <p style=""margin:16px 0 0;font-size:13px;color:{TextMuted};"">
      If the button doesn't work, copy this link into your browser:<br/>
      <a href=""{href}"" style=""color:{Primary};word-break:break-all;text-decoration:none;"">{href}</a>
    </p>";

    private static string MonoValue(string value) =>
        $@"<span style=""font-family:{Mono};font-size:15px;font-weight:700;color:{Primary};background:{SurfaceBlue};padding:2px 8px;border-radius:4px;"">{value}</span>";

    // ---- Public templates ----

    public static string Otp(string firstName, string otp, string frontendUrl, int expiryMinutes = 10)
    {
        var body = Heading($"Hi {firstName}, here's your OTP")
            + Subtext($"Use the code below to complete your login. It expires in <strong>{expiryMinutes} minutes</strong>.")
            + OtpBox(otp)
            + AlertBox("Never share this code with anyone. Our team will never ask for your OTP.")
            + Para("If you did not request this, you can safely ignore this email.");

        return Layout("Your One-Time Password", body, frontendUrl);
    }

    public static string PasswordReset(string firstName, string resetLink, string frontendUrl)
    {
        var body = Heading($"Hi {firstName}, reset your password")
            + Para("We received a request to reset the password on your account. Click below to set a new one.")
            + Button("Reset Password", resetLink)
            + AlertBox("This link expires in <strong>1 hour</strong>. If you did not request a reset, you can ignore this email — your password will remain unchanged.")
            + FallbackLink(resetLink);

        return Layout("Reset Your Password", body, frontendUrl);
    }

    public static string EmailVerification(string firstName, string verifyLink, string frontendUrl)
    {
        var body = Heading($"Hi {firstName}, verify your email")
            + Para("Thanks for signing up. Please verify your email address to activate your account.")
            + Button("Verify Email Address", verifyLink, Success)
            + AlertBox("This link expires in <strong>24 hours</strong>. If you did not create an account, you can ignore this email.", "info")
            + FallbackLink(verifyLink);

        return Layout("Verify Your Email", body, frontendUrl);
    }

    public static string VendorCredentialsReady(string vendorNo, string email, string tempPassword, string loginLink, string frontendUrl)
    {
        var body = Heading("Your Vendor Account is Ready")
            + Para("Your vendor account has been activated in the Vendor Management Portal. Use the credentials below to log in.")
            + InfoBox(
                ("Vendor No.", $"<strong>{vendorNo}</strong>"),
                ("Email", email),
                ("Temporary Password", MonoValue(tempPassword)))
            + AlertBox("You will be asked to verify your identity with a one-time password (OTP) on first login. Please change your password immediately after.")
            + Button("Log In Now", loginLink);

        return Layout("Your Vendor Account is Ready", body, frontendUrl);
    }
}