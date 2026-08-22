using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ArcanumBudget.Api.Services;

// Real email delivery over SMTP. Configure "Smtp:Host/Port/Username/Password"
// (and optionally "Smtp:FromEmail"/"Smtp:FromName") to activate this — see
// appsettings.Development.json.example. Falls back to ConsoleEmailService
// in Program.cs when SMTP isn't configured, so local dev keeps working
// without real credentials.
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task SendHouseholdInviteAsync(string toEmail, string verificationToken)
    {
        var baseUrl = _config["AppBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl}/household/verify?token={verificationToken}";

        return SendAsync(
            toEmail,
            "You've been invited to link households on Arcanum Budget",
            "You've been invited to link your accounts on Arcanum Budget so your " +
                "spending shows up on one shared dashboard.\n\n" +
                $"Confirm the link here:\n{link}\n\n" +
                "If you weren't expecting this, you can safely ignore this email.");
    }

    public Task SendPasswordResetAsync(string toEmail, string resetToken)
    {
        var baseUrl = _config["AppBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";

        return SendAsync(
            toEmail,
            "Reset your Arcanum Budget password",
            "We received a request to reset your Arcanum Budget password.\n\n" +
                $"Choose a new password here:\n{link}\n\n" +
                "If you didn't request this, you can safely ignore this email — your password won't change.");
    }

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = _config["Smtp:Host"]
            ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        var username = _config["Smtp:Username"]
            ?? throw new InvalidOperationException("Smtp:Username is not configured.");
        var password = _config["Smtp:Password"]
            ?? throw new InvalidOperationException("Smtp:Password is not configured.");
        var port = int.Parse(_config["Smtp:Port"] ?? "587");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Smtp:FromName"] ?? "Arcanum Budget",
            _config["Smtp:FromEmail"] ?? username));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);

        _logger.LogInformation("Sent email to {Email}: {Subject}", toEmail, subject);
    }
}
