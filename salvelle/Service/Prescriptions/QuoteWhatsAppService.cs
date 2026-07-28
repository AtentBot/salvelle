using System.Text;
using System.Text.Json;
using Data;
using DTOs.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.Pharmacy;

namespace Service.Prescriptions;

/// <summary>
/// Serviço para envio de orçamentos via WhatsApp usando AtentBot API
/// </summary>
public class QuoteWhatsAppService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuoteWhatsAppService> _logger;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _baseUrl;

    public QuoteWhatsAppService(
        AppDbContext context,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<QuoteWhatsAppService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["AtentBot:ApiKey"]
            ?? throw new InvalidOperationException("AtentBot:ApiKey não configurada.");
        _apiUrl = configuration["AtentBot:ApiUrl"] ?? "https://api.atentbot.com/message/sendText/pharm";
        _baseUrl = configuration["App:BaseUrl"] ?? "https://salvelle.atentbot.com";
    }

    /// <summary>
    /// Envia orçamento por WhatsApp
    /// </summary>
    public async Task<(bool Success, string Message)> SendQuoteAsync(Guid quoteId, Guid establishmentId)
    {
        try
        {
            var quote = await _context.Set<PrescriptionQuote>()
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado");

            if (string.IsNullOrEmpty(quote.CustomerPhone))
                return (false, "Cliente sem telefone cadastrado");

            // Formatar número
            var phone = FormatPhoneNumber(quote.CustomerPhone);
            if (string.IsNullOrEmpty(phone))
                return (false, "Número de telefone inválido");

            // Buscar dados da farmácia
            var establishment = await _context.Establishments
                .FirstOrDefaultAsync(e => e.Id == establishmentId);

            var pharmacyName = establishment?.NomeFantasia ?? establishment?.RazaoSocial ?? "Farmácia";

            // Montar mensagem
            var message = BuildQuoteMessage(quote, pharmacyName);
            var publicUrl = $"{_baseUrl}/orcamento/{quote.PublicToken}";

            // Enviar via AtentBot API
            var result = await SendWhatsAppMessageAsync(phone, message);

            if (result.Success)
            {
                // Atualizar quote
                quote.WhatsAppSent = true;
                quote.WhatsAppSentAt = DateTime.UtcNow;
                quote.WhatsAppMessageId = result.MessageId;
                quote.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Orçamento {Code} enviado por WhatsApp para {Phone}",
                    quote.Code, phone?.Length > 6 ? phone[..4] + "****" + phone[^2..] : "***");
            }

            return (result.Success, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar orçamento por WhatsApp");
            return (false, $"Erro: {ex.Message}");
        }
    }

    /// <summary>
    /// Envia lembrete de orçamento pendente
    /// </summary>
    public async Task<(bool Success, string Message)> SendReminderAsync(Guid quoteId, Guid establishmentId)
    {
        try
        {
            var quote = await _context.Set<PrescriptionQuote>()
                .FirstOrDefaultAsync(q => q.Id == quoteId &&
                                         q.EstablishmentId == establishmentId &&
                                         q.Status == "PENDENTE");

            if (quote == null)
                return (false, "Orçamento não encontrado ou já processado");

            if (string.IsNullOrEmpty(quote.CustomerPhone))
                return (false, "Cliente sem telefone");

            var phone = FormatPhoneNumber(quote.CustomerPhone);
            var publicUrl = $"{_baseUrl}/orcamento/{quote.PublicToken}";
            var daysLeft = (quote.ValidUntil - DateTime.UtcNow).Days;

            var message = $@"⏰ *Lembrete de Orçamento*

Olá {quote.CustomerName}!

Seu orçamento *{quote.Code}* ainda está aguardando aprovação.

💊 Fórmula: {quote.PharmaceuticalForm} {quote.TotalQuantity}
💰 Valor: R$ {quote.FinalPrice:N2}
📅 Válido por mais {daysLeft} dia(s)

👉 Acesse e aprove: {publicUrl}

Qualquer dúvida, estamos à disposição!";

            var result = await SendWhatsAppMessageAsync(phone, message);
            return (result.Success, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar lembrete");
            return (false, $"Erro: {ex.Message}");
        }
    }

    /// <summary>
    /// Notifica cliente sobre aprovação e início da produção
    /// </summary>
    public async Task<(bool Success, string Message)> SendApprovalConfirmationAsync(
        Guid quoteId,
        Guid establishmentId,
        string manipulationOrderCode)
    {
        try
        {
            var quote = await _context.Set<PrescriptionQuote>()
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado");

            if (string.IsNullOrEmpty(quote.CustomerPhone))
                return (false, "Cliente sem telefone");

            var phone = FormatPhoneNumber(quote.CustomerPhone);

            var message = $@"✅ *Pedido Confirmado!*

Olá {quote.CustomerName}!

Recebemos sua aprovação e sua fórmula já está em produção!

📋 Ordem de Manipulação: *{manipulationOrderCode}*
💊 Fórmula: {quote.PharmaceuticalForm} {quote.TotalQuantity}
📅 Previsão de entrega: {DateTime.Today.AddDays(quote.EstimatedDays):dd/MM/yyyy}

Avisaremos quando estiver pronto para retirada!

Obrigado pela preferência! 🙏";

            var result = await SendWhatsAppMessageAsync(phone, message);
            return (result.Success, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar confirmação");
            return (false, $"Erro: {ex.Message}");
        }
    }

    /// <summary>
    /// Notifica cliente que fórmula está pronta
    /// </summary>
    public async Task<(bool Success, string Message)> SendReadyForPickupAsync(
        Guid quoteId,
        Guid establishmentId)
    {
        try
        {
            var quote = await _context.Set<PrescriptionQuote>()
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado");

            if (string.IsNullOrEmpty(quote.CustomerPhone))
                return (false, "Cliente sem telefone");

            var phone = FormatPhoneNumber(quote.CustomerPhone);

            var establishment = await _context.Establishments
                .FirstOrDefaultAsync(e => e.Id == establishmentId);

            var message = $@"🎉 *Sua Fórmula está Pronta!*

Olá {quote.CustomerName}!

Sua fórmula manipulada está pronta para retirada!

💊 {quote.PharmaceuticalForm} {quote.TotalQuantity}
💰 Valor: R$ {quote.FinalPrice:N2}

📍 *Local de Retirada:*
{establishment?.NomeFantasia ?? "Farmácia"}
{establishment?.Street}, {establishment?.Number} - {establishment?.Neighborhood}
{establishment?.City}/{establishment?.State}

⏰ Horário de funcionamento: Seg-Sex 8h às 18h, Sáb 8h às 12h

Aguardamos sua visita! 😊";

            var result = await SendWhatsAppMessageAsync(phone, message);
            return (result.Success, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação de pronto");
            return (false, $"Erro: {ex.Message}");
        }
    }

    private string BuildQuoteMessage(PrescriptionQuote quote, string pharmacyName)
    {
        var publicUrl = $"{_baseUrl}/orcamento/{quote.PublicToken}";

        return $@"🏥 *{pharmacyName}*
━━━━━━━━━━━━━━━━━

📋 *ORÇAMENTO DE MANIPULAÇÃO*
Código: *{quote.Code}*

👤 *Cliente:* {quote.CustomerName}
👨‍⚕️ *Prescritor:* Dr(a). {quote.DoctorName}

💊 *Fórmula:*
• Uso: {quote.UsageType}
• Forma: {quote.PharmaceuticalForm}
• Quantidade: {quote.TotalQuantity}

💰 *Valor Total: R$ {quote.FinalPrice:N2}*

📅 Prazo de entrega: {quote.EstimatedDays} dias úteis
⏳ Orçamento válido até: {quote.ValidUntil:dd/MM/yyyy}

━━━━━━━━━━━━━━━━━

👉 *Clique no link abaixo para ver os detalhes e aprovar:*
{publicUrl}

Qualquer dúvida, estamos à disposição!";
    }

    private async Task<(bool Success, string Message, string? MessageId)> SendWhatsAppMessageAsync(
        string phone, string message)
    {
        try
        {
            var payload = new
            {
                number = phone,
                text = message
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);

            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp enviado com sucesso para {Phone}", phone?.Length > 6 ? phone[..4] + "****" + phone[^2..] : "***");
                return (true, "Mensagem enviada com sucesso", null);
            }
            else
            {
                _logger.LogError("Erro AtentBot: {Status} - {Response}", response.StatusCode, responseContent);
                return (false, $"Erro ao enviar: {response.StatusCode} - {responseContent}", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na comunicação com AtentBot");
            return (false, $"Erro de comunicação: {ex.Message}", null);
        }
    }

    private string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";

        // Remover caracteres não numéricos
        var numbers = new string(phone.Where(char.IsDigit).ToArray());

        // Adicionar código do país se não tiver
        if (numbers.Length == 11) // DDD + número
            numbers = "55" + numbers;
        else if (numbers.Length == 10) // DDD + número fixo
            numbers = "55" + numbers;

        return numbers;
    }

}