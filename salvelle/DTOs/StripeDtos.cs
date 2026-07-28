namespace DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxMonthlyOrders { get; set; }
    public Dictionary<string, bool> Features { get; set; } = new();
    public bool IsActive { get; set; }
    
    // Gateway IDs
    public string? StripePriceIdMonthly { get; set; }
    public string? StripePriceIdYearly { get; set; }
    public string? MercadoPagoPlanIdMonthly { get; set; }
    public string? MercadoPagoPlanIdYearly { get; set; }
    public string? AbacatepayPlanIdMonthly { get; set; }
    public string? AbacatepayPlanIdYearly { get; set; }
}

public class CreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int? MaxEmployees { get; set; }
    public int? MaxMonthlyOrders { get; set; }
    public Dictionary<string, bool>? Features { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Gateway IDs - Stripe
    public string? StripePriceIdMonthly { get; set; }
    public string? StripePriceIdYearly { get; set; }
    
    // Gateway IDs - MercadoPago
    public string? MercadoPagoPlanIdMonthly { get; set; }
    public string? MercadoPagoPlanIdYearly { get; set; }
    
    // Gateway IDs - Abacatepay
    public string? AbacatepayPlanIdMonthly { get; set; }
    public string? AbacatepayPlanIdYearly { get; set; }
}

public class CreateCheckoutSessionDto
{
    public Guid EstablishmentId { get; set; }
    public Guid PlanId { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class CheckoutSessionResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
}

public class CreatePortalSessionDto
{
    public Guid EstablishmentId { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
}

public class PortalSessionResponseDto
{
    public string Url { get; set; } = string.Empty;
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InvoiceUrl { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para atualizar apenas os Gateway IDs de um plano
/// </summary>
public class UpdateGatewayIdsDto
{
    public string? StripePriceIdMonthly { get; set; }
    public string? StripePriceIdYearly { get; set; }
    public string? MercadoPagoPlanIdMonthly { get; set; }
    public string? MercadoPagoPlanIdYearly { get; set; }
    public string? AbacatepayPlanIdMonthly { get; set; }
    public string? AbacatepayPlanIdYearly { get; set; }
}
