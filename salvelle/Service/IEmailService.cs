namespace Service;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string toName, string loginUrl);
    Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string toName);
}
