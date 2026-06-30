namespace RaceManager.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "RaceManager ID";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(UserName) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(FromEmail) &&
        !UserName.Contains("your-", StringComparison.OrdinalIgnoreCase) &&
        !Password.Contains("your-", StringComparison.OrdinalIgnoreCase) &&
        !FromEmail.Contains("your-", StringComparison.OrdinalIgnoreCase);
}
