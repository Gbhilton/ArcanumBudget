namespace ArcanumBudget.Api.Services;

// Dev-only stand-in: logs the verification link instead of emailing it.
// Swap for a real IEmailService implementation (SMTP/SendGrid/etc.) before going to real users.
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly IConfiguration _config;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task SendHouseholdInviteAsync(string toEmail, string verificationToken)
    {
        var baseUrl = _config["AppBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl}/household/verify?token={verificationToken}";

        _logger.LogInformation(
            "[DEV EMAIL] Household invite for {Email}: {Link}", toEmail, link);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string resetToken)
    {
        var baseUrl = _config["AppBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";

        _logger.LogInformation(
            "[DEV EMAIL] Password reset for {Email}: {Link}", toEmail, link);

        return Task.CompletedTask;
    }
}
