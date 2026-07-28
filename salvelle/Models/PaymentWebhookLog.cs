using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Log de webhooks recebidos dos gateways de pagamento
/// </summary>
[Table("payment_webhook_logs")]
public class PaymentWebhookLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Tipo do gateway que enviou o webhook
    /// </summary>
    [Required]
    [Column("gateway_type")]
    public PaymentGatewayType GatewayType { get; set; }

    /// <summary>
    /// ID da configuração do gateway (se identificado)
    /// </summary>
    [Column("gateway_config_id")]
    public Guid? GatewayConfigId { get; set; }

    /// <summary>
    /// Tipo do evento (ex: payment_intent.succeeded, invoice.paid)
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// ID do evento no gateway
    /// </summary>
    [MaxLength(200)]
    [Column("external_event_id")]
    public string? ExternalEventId { get; set; }

    /// <summary>
    /// Payload completo do webhook (JSON)
    /// </summary>
    [Column("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Headers relevantes do request
    /// </summary>
    [Column("headers")]
    public string? Headers { get; set; }

    /// <summary>
    /// Assinatura recebida
    /// </summary>
    [MaxLength(500)]
    [Column("signature")]
    public string? Signature { get; set; }

    /// <summary>
    /// Se a assinatura foi validada com sucesso
    /// </summary>
    [Column("signature_valid")]
    public bool? SignatureValid { get; set; }

    /// <summary>
    /// Status do processamento
    /// </summary>
    [Column("status")]
    public WebhookProcessingStatus Status { get; set; } = WebhookProcessingStatus.Received;

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace do erro (se houver)
    /// </summary>
    [Column("error_stack_trace")]
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// ID da assinatura relacionada (se identificada)
    /// </summary>
    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// ID da fatura relacionada (se identificada)
    /// </summary>
    [Column("invoice_id")]
    public Guid? InvoiceId { get; set; }

    /// <summary>
    /// ID do estabelecimento relacionado (se identificado)
    /// </summary>
    [Column("establishment_id")]
    public Guid? EstablishmentId { get; set; }

    /// <summary>
    /// IP de origem do request
    /// </summary>
    [MaxLength(50)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Número de tentativas de processamento
    /// </summary>
    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Quando foi processado com sucesso
    /// </summary>
    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Tempo de processamento em ms
    /// </summary>
    [Column("processing_time_ms")]
    public long? ProcessingTimeMs { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ════════════════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    [ForeignKey("GatewayConfigId")]
    public virtual PaymentGatewayConfig? GatewayConfig { get; set; }

    // Nota: subscription_id, invoice_id e establishment_id são apenas 
    // campos de referência para log, sem FK no banco para evitar dependências
}
