using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Configuração de gateway de pagamento com credenciais criptografadas
/// Suporta Stripe, Mercado Pago, Abacatepay e outros gateways
/// </summary>
[Table("payment_gateway_configs")]
public class PaymentGatewayConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Tipo do gateway (Stripe, MercadoPago, Abacatepay)
    /// </summary>
    [Required]
    [Column("gateway_type")]
    public PaymentGatewayType GatewayType { get; set; }

    /// <summary>
    /// Ambiente (Sandbox ou Production)
    /// </summary>
    [Required]
    [Column("environment")]
    public GatewayEnvironment Environment { get; set; }

    /// <summary>
    /// Nome de exibição para identificar a configuração
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Chave pública (criptografada)
    /// Stripe: pk_test_* / pk_live_*
    /// MercadoPago: Public Key
    /// Abacatepay: API Key
    /// </summary>
    [Column("public_key_encrypted")]
    public string? PublicKeyEncrypted { get; set; }

    /// <summary>
    /// Chave secreta (criptografada)
    /// Stripe: sk_test_* / sk_live_*
    /// MercadoPago: Access Token
    /// Abacatepay: Secret Key
    /// </summary>
    [Column("secret_key_encrypted")]
    public string? SecretKeyEncrypted { get; set; }

    /// <summary>
    /// Secret do webhook (criptografado)
    /// Usado para validar assinatura dos webhooks
    /// </summary>
    [Column("webhook_secret_encrypted")]
    public string? WebhookSecretEncrypted { get; set; }

    /// <summary>
    /// Access Token adicional (criptografado)
    /// Usado principalmente pelo Mercado Pago
    /// </summary>
    [Column("access_token_encrypted")]
    public string? AccessTokenEncrypted { get; set; }

    /// <summary>
    /// Client ID (criptografado)
    /// Usado para OAuth em alguns gateways
    /// </summary>
    [Column("client_id_encrypted")]
    public string? ClientIdEncrypted { get; set; }

    /// <summary>
    /// Client Secret (criptografado)
    /// Usado para OAuth em alguns gateways
    /// </summary>
    [Column("client_secret_encrypted")]
    public string? ClientSecretEncrypted { get; set; }

    /// <summary>
    /// URL base da API (opcional, para APIs customizadas)
    /// </summary>
    [MaxLength(500)]
    [Column("api_base_url")]
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// URL do webhook configurada no gateway
    /// </summary>
    [MaxLength(500)]
    [Column("webhook_url")]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Se está ativo e disponível para uso
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se é o gateway padrão para novas cobranças
    /// Apenas um pode ser default por ambiente
    /// </summary>
    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Data/hora do último teste de conexão
    /// </summary>
    [Column("last_tested_at")]
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Status do último teste
    /// </summary>
    [Column("last_test_status")]
    public ConnectionTestStatus? LastTestStatus { get; set; }

    /// <summary>
    /// Mensagem do último teste (sucesso ou erro)
    /// </summary>
    [MaxLength(1000)]
    [Column("last_test_message")]
    public string? LastTestMessage { get; set; }

    /// <summary>
    /// Configurações adicionais em JSON (features específicas do gateway)
    /// </summary>
    [Column("additional_settings")]
    public string? AdditionalSettings { get; set; }

    /// <summary>
    /// Moedas suportadas (ex: "BRL,USD")
    /// </summary>
    [MaxLength(100)]
    [Column("supported_currencies")]
    public string SupportedCurrencies { get; set; } = "BRL";

    /// <summary>
    /// ID do admin que criou a configuração
    /// </summary>
    [Column("created_by_admin_id")]
    public Guid? CreatedByAdminId { get; set; }

    /// <summary>
    /// ID do admin que atualizou por último
    /// </summary>
    [Column("updated_by_admin_id")]
    public Guid? UpdatedByAdminId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relacionamentos
    [ForeignKey("CreatedByAdminId")]
    public virtual SaasAdmin? CreatedByAdmin { get; set; }

    [ForeignKey("UpdatedByAdminId")]
    public virtual SaasAdmin? UpdatedByAdmin { get; set; }
}
