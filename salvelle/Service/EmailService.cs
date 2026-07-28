using System.Net;
using System.Net.Mail;
using System.Text;
using Configuration;
using Microsoft.Extensions.Options;

namespace Service;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30 segundos
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(toEmail, toName));

            if (!string.IsNullOrEmpty(_settings.ReplyToEmail))
            {
                message.ReplyToList.Add(new MailAddress(_settings.ReplyToEmail));
            }

            // Usar AlternateViews para garantir renderização correta
            // Versão texto (fallback)
            if (!string.IsNullOrEmpty(textBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(
                    textBody,
                    Encoding.UTF8,
                    "text/plain"
                );
                message.AlternateViews.Add(textView);
            }

            // Versão HTML (principal)
            var htmlView = AlternateView.CreateAlternateViewFromString(
                htmlBody,
                Encoding.UTF8,
                "text/html"
            );
            message.AlternateViews.Add(htmlView);

            await client.SendMailAsync(message);

            _logger.LogInformation("Email enviado com sucesso para {ToEmail}: {Subject}", toEmail?.Length > 5 ? toEmail[..2] + "***" + toEmail[toEmail.IndexOf('@')..] : "***", subject);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Erro SMTP ao enviar email para {ToEmail}: {Message}", toEmail?.Length > 5 ? toEmail[..2] + "***" + toEmail[toEmail.IndexOf('@')..] : "***", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email para {ToEmail}", toEmail?.Length > 5 ? toEmail[..2] + "***" + toEmail[toEmail.IndexOf('@')..] : "***");
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string toName, string resetUrl)
    {
        var subject = "Salvelle - Recuperação de Senha";

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #f4f4f4;"">
        <tr>
            <td style=""padding: 40px 20px;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""600"" style=""margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%); padding: 30px 40px; border-radius: 8px 8px 0 0; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;"">Salvelle</h1>
                            <p style=""color: #a0a0a0; margin: 10px 0 0 0; font-size: 14px;"">Painel Administrativo</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px;"">
                            <h2 style=""color: #1a1a2e; margin: 0 0 20px 0; font-size: 22px;"">Recuperação de Senha</h2>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">Olá <strong>{toName}</strong>,</p>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">Recebemos uma solicitação para redefinir a senha da sua conta de administrador no Salvelle.</p>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6; margin: 0 0 30px 0;"">Clique no botão abaixo para criar uma nova senha:</p>
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 auto 30px auto;"">
                                <tr>
                                    <td style=""border-radius: 8px; background-color: #0d6efd;"">
                                        <a href=""{resetUrl}"" target=""_blank"" style=""display: inline-block; padding: 16px 40px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 8px;"">Redefinir Minha Senha</a>
                                    </td>
                                </tr>
                            </table>
                            <div style=""background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 6px; padding: 15px; margin-bottom: 25px;"">
                                <p style=""color: #856404; font-size: 14px; margin: 0;""><strong>Atenção:</strong> Este link expira em <strong>1 hora</strong>. Se você não solicitou esta redefinição, ignore este email.</p>
                            </div>
                            <p style=""color: #888888; font-size: 13px; line-height: 1.6; margin: 0 0 10px 0;"">Se o botão não funcionar, copie e cole o link abaixo no seu navegador:</p>
                            <p style=""color: #0d6efd; font-size: 12px; word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px; margin: 0;"">{resetUrl}</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 25px 40px; border-radius: 0 0 8px 8px; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #888888; font-size: 12px; margin: 0 0 10px 0; text-align: center;"">Este email foi enviado automaticamente pelo sistema Salvelle.</p>
                            <p style=""color: #888888; font-size: 12px; margin: 0; text-align: center;"">© {DateTime.Now.Year} Salvelle - D&W Consultoria em Ltda</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = $@"Salvelle - Recuperação de Senha

Olá {toName},

Recebemos uma solicitação para redefinir a senha da sua conta de administrador no Salvelle.

Para criar uma nova senha, acesse o link abaixo:
{resetUrl}

ATENÇÃO: Este link expira em 1 hora. Se você não solicitou esta redefinição, ignore este email.

---
Este email foi enviado automaticamente pelo sistema Salvelle.
© {DateTime.Now.Year} Salvelle - D&W Consultoria em Ltda";

        return await SendEmailAsync(toEmail, toName, subject, htmlBody, textBody);
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string toName, string loginUrl)
    {
        var subject = "Bem-vindo ao Salvelle!";

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #f4f4f4;"">
        <tr>
            <td style=""padding: 40px 20px;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""600"" style=""margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%); padding: 30px 40px; border-radius: 8px 8px 0 0; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">Bem-vindo ao Salvelle!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">Olá <strong>{toName}</strong>,</p>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">Sua conta de administrador foi criada com sucesso no Salvelle.</p>
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px auto;"">
                                <tr>
                                    <td style=""border-radius: 8px; background-color: #198754;"">
                                        <a href=""{loginUrl}"" style=""display: inline-block; padding: 16px 40px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600;"">Acessar o Painel</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 25px 40px; border-radius: 0 0 8px 8px; text-align: center;"">
                            <p style=""color: #888888; font-size: 12px; margin: 0;"">© {DateTime.Now.Year} Salvelle</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = $@"Bem-vindo ao Salvelle!

Olá {toName},

Sua conta de administrador foi criada com sucesso no Salvelle.

Acesse o painel em: {loginUrl}

© {DateTime.Now.Year} Salvelle";

        return await SendEmailAsync(toEmail, toName, subject, htmlBody, textBody);
    }

    public async Task<bool> SendPasswordChangedNotificationAsync(string toEmail, string toName)
    {
        var subject = "Salvelle - Sua senha foi alterada";

        var htmlBody = $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #f4f4f4;"">
        <tr>
            <td style=""padding: 40px 20px;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""600"" style=""margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%); padding: 30px 40px; border-radius: 8px 8px 0 0; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">Salvelle</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px;"">
                            <h2 style=""color: #1a1a2e; margin: 0 0 20px 0;"">Senha Alterada com Sucesso</h2>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">Olá <strong>{toName}</strong>,</p>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6;"">A senha da sua conta de administrador no Salvelle foi alterada em <strong>{Helpers.BrazilTime.Now:dd/MM/yyyy HH:mm}</strong>.</p>
                            <div style=""background-color: #f8d7da; border: 1px solid #f5c6cb; border-radius: 6px; padding: 15px; margin: 20px 0;"">
                                <p style=""color: #721c24; font-size: 14px; margin: 0;""><strong>Atenção:</strong> Se você não realizou esta alteração, entre em contato imediatamente com o suporte.</p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 25px 40px; border-radius: 0 0 8px 8px; text-align: center;"">
                            <p style=""color: #888888; font-size: 12px; margin: 0;"">© {DateTime.Now.Year} Salvelle</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var textBody = $@"Salvelle - Sua senha foi alterada

Olá {toName},

A senha da sua conta de administrador no Salvelle foi alterada em {Helpers.BrazilTime.Now:dd/MM/yyyy HH:mm}.

ATENÇÃO: Se você não realizou esta alteração, entre em contato imediatamente com o suporte.

© {DateTime.Now.Year} Salvelle";

        return await SendEmailAsync(toEmail, toName, subject, htmlBody, textBody);
    }
}