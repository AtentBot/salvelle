using Models;

namespace Service;

/// <summary>
/// Interface para serviços de gateway de pagamento
/// </summary>
public interface IPaymentGatewayService
{
    PaymentGatewayType GatewayType { get; }
    
    Task<PaymentTestResult> TestConnectionAsync(PaymentGatewayConfig config);
    Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request);
    Task<PaymentResult> CreateChargeAsync(CreateChargeRequest request);
    Task<PaymentResult> CreateSubscriptionAsync(CreateSubscriptionRequest request);
    Task<PaymentResult> CancelSubscriptionAsync(CancelSubscriptionRequest request);
    Task<PaymentResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request);
    Task<WebhookResult> ProcessWebhookAsync(ProcessWebhookRequest request);
    Task<PaymentDetails?> GetPaymentDetailsAsync(string paymentId, PaymentGatewayConfig config);
    Task<SubscriptionDetails?> GetSubscriptionDetailsAsync(string subscriptionId, PaymentGatewayConfig config);
    Task<CheckoutResult> CreateCheckoutSessionAsync(CreateCheckoutRequest request);
}

// ══════════════════════════════════════════════════════════════════════════════
// REQUEST CLASSES
// ══════════════════════════════════════════════════════════════════════════════

public class CreateCustomerRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public required Guid EstablishmentId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CreateChargeRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public required decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public required string CustomerId { get; set; }
    public string? Description { get; set; }
    public Guid? EstablishmentId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CreateSubscriptionRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public required Guid EstablishmentId { get; set; }
    public required Guid SubscriptionPlanId { get; set; }
    public required string CustomerId { get; set; }
    public string? PriceId { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string BillingCycle { get; set; } = "MONTHLY";
    public int? TrialDays { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CancelSubscriptionRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public required string ExternalSubscriptionId { get; set; }
    public bool CancelImmediately { get; set; }
    public string? Reason { get; set; }
}

public class UpdateSubscriptionRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public required string ExternalSubscriptionId { get; set; }
    public string? NewPriceId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class ProcessWebhookRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public required string Payload { get; set; }
    public string? Signature { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

public class CreateCheckoutRequest
{
    public required PaymentGatewayConfig Config { get; set; }
    public string? CustomerId { get; set; }
    public required string Mode { get; set; } // "payment" ou "subscription"
    public required string SuccessUrl { get; set; }
    public required string CancelUrl { get; set; }
    public required List<CheckoutLineItem> LineItems { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CheckoutLineItem
{
    public string? PriceId { get; set; }
    public decimal? Amount { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public long Quantity { get; set; } = 1;
}

// ══════════════════════════════════════════════════════════════════════════════
// RESULT CLASSES
// ══════════════════════════════════════════════════════════════════════════════

public class PaymentTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ResponseTimeMs { get; set; }
    
    public static PaymentTestResult Ok(string message, long responseTimeMs = 0)
        => new() { Success = true, Message = message, ResponseTimeMs = responseTimeMs };
    
    public static PaymentTestResult Fail(string message)
        => new() { Success = false, Message = message };
}

public class CustomerResult
{
    public bool Success { get; set; }
    public string? CustomerId { get; set; }
    public string? Message { get; set; }
    
    public static CustomerResult Ok(string customerId)
        => new() { Success = true, CustomerId = customerId };
    
    public static CustomerResult Fail(string message)
        => new() { Success = false, Message = message };
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public string? CheckoutUrl { get; set; }
    public decimal? AmountCharged { get; set; }
    
    public static PaymentResult Ok(string externalId, string? status = null)
        => new() { Success = true, ExternalId = externalId, Status = status };
    
    public static PaymentResult Fail(string message, string? errorCode = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode };
}

public class WebhookResult
{
    public bool Success { get; set; }
    public string? EventType { get; set; }
    public string? ExternalEventId { get; set; }
    public WebhookAction Action { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public string? NewStatus { get; set; }
    public decimal? Amount { get; set; }
    public string? Message { get; set; }
    
    // IDs internos extraídos do metadata
    public Guid? EstablishmentId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? InvoiceId { get; set; }
}

public enum WebhookAction
{
    None,
    SubscriptionCreated,
    SubscriptionUpdated,
    SubscriptionCanceled,
    InvoicePaid,
    InvoiceFailed,
    PaymentSucceeded,
    PaymentFailed,
    Refunded
}

public class CheckoutResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? Message { get; set; }
    
    public static CheckoutResult Ok(string sessionId, string? checkoutUrl)
        => new() { Success = true, SessionId = sessionId, CheckoutUrl = checkoutUrl };
    
    public static CheckoutResult Fail(string message)
        => new() { Success = false, Message = message };
}

// ══════════════════════════════════════════════════════════════════════════════
// DETAIL CLASSES
// ══════════════════════════════════════════════════════════════════════════════

public class PaymentDetails
{
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime? CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class SubscriptionDetails
{
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public string? ExternalCustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string BillingCycle { get; set; } = "MONTHLY";
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
