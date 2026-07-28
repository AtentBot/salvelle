namespace Models;

/// <summary>
/// Tipos de gateway de pagamento suportados
/// </summary>
public enum PaymentGatewayType
{
    Stripe = 1,
    MercadoPago = 2,
    Abacatepay = 3
}

/// <summary>
/// Ambiente do gateway (Sandbox/Produção)
/// </summary>
public enum GatewayEnvironment
{
    Sandbox = 1,
    Production = 2
}

/// <summary>
/// Status do resultado de teste de conexão
/// </summary>
public enum ConnectionTestStatus
{
    Success = 1,
    Failed = 2,
    Pending = 3
}

/// <summary>
/// Status do processamento de webhook
/// </summary>
public enum WebhookProcessingStatus
{
    Received = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    Ignored = 5
}
