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
        var baseUrl = _config["AppBaseUrl"] ?? "https://localhost:5001";
        var link = $"{baseUrl}/household/verify?token={verificationToken}";

        _logger.LogInformation(
            "[DEV EMAIL] Household invite for {Email}: {Link}", toEmail, link);

        return Task.CompletedTask;
    }
}
