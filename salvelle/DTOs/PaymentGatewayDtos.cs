using Models;

namespace DTOs;

// ==================== REQUEST DTOs ====================

/// <summary>
/// DTO para criar uma nova configuração de gateway
/// </summary>
public class CreatePaymentGatewayDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public GatewayEnvironment Environment { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Chave pública (será criptografada)
    /// </summary>
    public string? PublicKey { get; set; }
    
    /// <summary>
    /// Chave secreta (será criptografada)
    /// </summary>
    public string? SecretKey { get; set; }
    
    /// <summary>
    /// Secret do webhook (será criptografado)
    /// </summary>
    public string? WebhookSecret { get; set; }
    
    /// <summary>
    /// Access Token - principalmente para Mercado Pago
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Client ID - para OAuth
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Client Secret - para OAuth
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// URL base customizada da API
    /// </summary>
    public string? ApiBaseUrl { get; set; }
    
    /// <summary>
    /// Moedas suportadas separadas por vírgula (ex: "BRL,USD")
    /// </summary>
    public string SupportedCurrencies { get; set; } = "BRL";
    
    /// <summary>
    /// Se deve ser ativado imediatamente
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Se deve ser definido como gateway padrão
    /// </summary>
    public bool SetAsDefault { get; set; } = false;
    
    /// <summary>
    /// Configurações adicionais em JSON
    /// </summary>
    public Dictionary<string, object>? AdditionalSettings { get; set; }
}

/// <summary>
/// DTO para atualizar uma configuração existente
/// </summary>
public class UpdatePaymentGatewayDto
{
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Nova chave pública (null = manter atual)
    /// </summary>
    public string? PublicKey { get; set; }
    
    /// <summary>
    /// Nova chave secreta (null = manter atual)
    /// </summary>
    public string? SecretKey { get; set; }
    
    /// <summary>
    /// Novo webhook secret (null = manter atual)
    /// </summary>
    public string? WebhookSecret { get; set; }
    
    /// <summary>
    /// Novo access token (null = manter atual)
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Novo client ID (null = manter atual)
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Novo client secret (null = manter atual)
    /// </summary>
    public string? ClientSecret { get; set; }
    
    public string? ApiBaseUrl { get; set; }
    public string? SupportedCurrencies { get; set; }
    public bool? IsActive { get; set; }
    public Dictionary<string, object>? AdditionalSettings { get; set; }
}

// ==================== RESPONSE DTOs ====================

/// <summary>
/// DTO de resposta com dados do gateway (credenciais mascaradas)
/// </summary>
public class PaymentGatewayConfigDto
{
    public Guid Id { get; set; }
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayTypeName => GatewayType.ToString();
    public GatewayEnvironment Environment { get; set; }
    public string EnvironmentName => Environment.ToString();
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Chave pública mascarada (ex: pk_live_***abc)
    /// </summary>
    public string? PublicKeyMasked { get; set; }
    
    /// <summary>
    /// Chave secreta mascarada (ex: sk_live_***xyz)
    /// </summary>
    public string? SecretKeyMasked { get; set; }
    
    /// <summary>
    /// Webhook secret mascarado
    /// </summary>
    public string? WebhookSecretMasked { get; set; }
    
    /// <summary>
    /// Access token mascarado
    /// </summary>
    public string? AccessTokenMasked { get; set; }
    
    /// <summary>
    /// Indica se tem chave pública configurada
    /// </summary>
    public bool HasPublicKey { get; set; }
    
    /// <summary>
    /// Indica se tem chave secreta configurada
    /// </summary>
    public bool HasSecretKey { get; set; }
    
    /// <summary>
    /// Indica se tem webhook secret configurado
    /// </summary>
    public bool HasWebhookSecret { get; set; }
    
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public ConnectionTestStatus? LastTestStatus { get; set; }
    public string? LastTestMessage { get; set; }
    public string SupportedCurrencies { get; set; } = "BRL";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para listagem resumida de gateways
/// </summary>
public class PaymentGatewayListItemDto
{
    public Guid Id { get; set; }
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayTypeName => GatewayType.ToString();
    public GatewayEnvironment Environment { get; set; }
    public string EnvironmentName => Environment.ToString();
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsConfigured { get; set; }
    public ConnectionTestStatus? LastTestStatus { get; set; }
    public DateTime? LastTestedAt { get; set; }
}

/// <summary>
/// Resultado do teste de conexão
/// </summary>
public class ConnectionTestResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    public long ResponseTimeMs { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// DTO para estatísticas dos gateways
/// </summary>
public class PaymentGatewayStatsDto
{
    public int TotalConfigurations { get; set; }
    public int ActiveConfigurations { get; set; }
    public int ProductionConfigurations { get; set; }
    public int SandboxConfigurations { get; set; }
    public PaymentGatewayType? DefaultGateway { get; set; }
    public Dictionary<string, int> ConfigurationsByGateway { get; set; } = new();
}

// ==================== WEBHOOK DTOs ====================

/// <summary>
/// DTO para listagem de logs de webhook
/// </summary>
public class WebhookLogDto
{
    public Guid Id { get; set; }
    public PaymentGatewayType GatewayType { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ExternalEventId { get; set; }
    public WebhookProcessingStatus Status { get; set; }
    public bool? SignatureValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public long? ProcessingTimeMs { get; set; }
}

/// <summary>
/// Filtros para busca de logs de webhook
/// </summary>
public class WebhookLogFilterDto
{
    public PaymentGatewayType? GatewayType { get; set; }
    public WebhookProcessingStatus? Status { get; set; }
    public string? EventType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}
