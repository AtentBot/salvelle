namespace Configuration;

public class EmailSettings
{
    public string SmtpHost { get; set; } = default!;
    public int SmtpPort { get; set; }
    public string SmtpUser { get; set; } = default!;
    public string SmtpPass { get; set; } = default!;
    public bool EnableSsl { get; set; }
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = default!;
    public string? ReplyToEmail { get; set; }
}