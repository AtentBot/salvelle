using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Data;
using Models.Pharmacy;
using System.Text;

namespace Service.Prescriptions;

public class QuoteEmailService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuoteEmailService> _logger;

    public QuoteEmailService(
        AppDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<QuoteEmailService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SendQuoteEmailAsync(
        Guid quoteId,
        string toEmail,
        string? customMessage,
        Guid establishmentId)
    {
        try
        {
            // Buscar orçamento com dados relacionados
            var quote = await _context.Set<PrescriptionQuote>()
                .Include(q => q.Establishment)
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado");

            if (string.IsNullOrWhiteSpace(toEmail))
                return (false, "E-mail do destinatário não informado");

            // Montar link público
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://app.salvelle.com";
            var quoteLink = $"{baseUrl}/Prescriptions/Quote/{quote.PublicToken}";

            // Montar conteúdo do email
            var establishmentName = quote.Establishment?.NomeFantasia ?? "Farmácia";
            var customerName = !string.IsNullOrEmpty(quote.CustomerName) ? quote.CustomerName.Split(' ')[0] : "Cliente";
            var subject = $"Orçamento {quote.Code} - {establishmentName}";
            
            var htmlBody = BuildEmailHtml(quote, quoteLink, customMessage);
            var textBody = BuildEmailText(quote, quoteLink, customMessage);

            // Enviar usando o serviço existente
            var success = await _emailService.SendEmailAsync(toEmail, customerName, subject, htmlBody, textBody);

            if (!success)
                return (false, "Falha ao enviar e-mail");

            // Atualizar registro de envio
            quote.EmailSent = true;
            quote.EmailSentAt = DateTime.UtcNow;
            quote.EmailSentTo = toEmail;
            await _context.SaveChangesAsync();

            _logger.LogInformation("E-mail do orçamento {QuoteCode} enviado para {Email}", quote.Code, toEmail?.Length > 5 ? toEmail[..2] + "***" + toEmail[toEmail.IndexOf('@')..] : "***");

            return (true, "E-mail enviado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail do orçamento {QuoteId}", quoteId);
            return (false, $"Erro ao enviar e-mail: {ex.Message}");
        }
    }

    private string BuildEmailHtml(PrescriptionQuote quote, string quoteLink, string? customMessage)
    {
        var establishmentName = quote.Establishment?.NomeFantasia ?? "Farmácia";
        var establishmentPhone = quote.Establishment?.WhatsApp ?? quote.Establishment?.Phone ?? "";
        var customerName = !string.IsNullOrEmpty(quote.CustomerName) ? quote.CustomerName.Split(' ')[0] : "Cliente";

        return $@"<!DOCTYPE html>
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
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #198754; padding: 30px 40px; border-radius: 8px 8px 0 0; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 700;"">{establishmentName}</h1>
                            <p style=""color: #e9ecef; margin: 10px 0 0 0; font-size: 16px;"">Orçamento #{quote.Code}</p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">Olá <strong>{customerName}</strong>,</p>
                            <p style=""color: #555555; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;"">{(string.IsNullOrWhiteSpace(customMessage) ? "Segue o orçamento solicitado da sua fórmula manipulada." : customMessage)}</p>
                            
                            <!-- Quote Box -->
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #f8f9fa; border-radius: 8px; margin: 25px 0;"">
                                <tr>
                                    <td style=""padding: 25px;"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                                            <tr>
                                                <td style=""vertical-align: top;"">
                                                    <p style=""margin: 0; color: #666; font-size: 14px;"">Valor Total</p>
                                                    <p style=""margin: 5px 0 0 0; font-size: 32px; font-weight: bold; color: #198754;"">R$ {quote.FinalPrice:N2}</p>
                                                </td>
                                                <td style=""vertical-align: top; text-align: right;"">
                                                    <p style=""margin: 0; color: #666; font-size: 14px;"">Prazo</p>
                                                    <p style=""margin: 5px 0 0 0; font-size: 20px; font-weight: bold; color: #333;"">{quote.EstimatedDays} dias</p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""color: #666; font-size: 14px; text-align: center; margin: 0 0 30px 0;"">Válido até: <strong>{quote.ValidUntil:dd/MM/yyyy}</strong></p>
                            
                            <!-- CTA Button -->
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 auto 30px auto;"">
                                <tr>
                                    <td style=""border-radius: 8px; background-color: #198754;"">
                                        <!--[if mso]>
                                        <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{quoteLink}"" style=""height:56px;v-text-anchor:middle;width:280px;"" arcsize=""14%"" strokecolor=""#157347"" fillcolor=""#198754"">
                                        <w:anchorlock/>
                                        <center style=""color:#ffffff;font-family:sans-serif;font-size:16px;font-weight:bold;"">Ver Orçamento Completo</center>
                                        </v:roundrect>
                                        <![endif]-->
                                        <!--[if !mso]><!-->
                                        <a href=""{quoteLink}"" target=""_blank"" style=""display: inline-block; padding: 18px 50px; color: #ffffff !important; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 8px; background-color: #198754; border: 2px solid #157347; mso-hide: all;"">Ver Orçamento Completo</a>
                                        <!--<![endif]-->
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""color: #888; font-size: 14px; text-align: center; margin: 0;"">Clique no botão acima para visualizar os detalhes e aprovar ou recusar o orçamento.</p>
                            
                            <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
                            
                            <p style=""color: #999; font-size: 13px; text-align: center; margin: 0;"">Dúvidas? Entre em contato conosco{(string.IsNullOrEmpty(establishmentPhone) ? "" : $": {establishmentPhone}")}</p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 25px 40px; border-radius: 0 0 8px 8px; border-top: 1px solid #e9ecef;"">
                            <p style=""color: #888888; font-size: 12px; margin: 0; text-align: center;"">Este e-mail foi enviado automaticamente pelo sistema Salvelle.</p>
                            <p style=""color: #888888; font-size: 12px; margin: 10px 0 0 0; text-align: center;"">© {DateTime.Now.Year} Salvelle - Sistema de Gestão para Farmácias de Manipulação</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private string BuildEmailText(PrescriptionQuote quote, string quoteLink, string? customMessage)
    {
        var establishmentName = quote.Establishment?.NomeFantasia ?? "Farmácia";
        var customerName = !string.IsNullOrEmpty(quote.CustomerName) ? quote.CustomerName.Split(' ')[0] : "Cliente";

        return $@"{establishmentName}
Orçamento #{quote.Code}

Olá {customerName},

{(string.IsNullOrWhiteSpace(customMessage) ? "Segue o orçamento solicitado da sua fórmula manipulada." : customMessage)}

Valor Total: R$ {quote.FinalPrice:N2}
Prazo de Produção: {quote.EstimatedDays} dias
Válido até: {quote.ValidUntil:dd/MM/yyyy}

Acesse o link abaixo para ver o orçamento completo e aprovar ou recusar:
{quoteLink}

---
Este e-mail foi enviado automaticamente pelo sistema Salvelle.
© {DateTime.Now.Year} Salvelle - Sistema de Gestão para Farmácias de Manipulação";
    }
}
