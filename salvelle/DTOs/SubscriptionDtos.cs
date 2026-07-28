namespace DTOs;

public class CreateSubscriptionDto
{
    public Guid EstablishmentId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
}

public class UpdateSubscriptionDto
{
    public Guid SubscriptionPlanId { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
}

public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public string EstablishmentName { get; set; } = string.Empty;
    public Guid SubscriptionPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime? TrialEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SubscriptionListDto
{
    public Guid Id { get; set; }
    public string EstablishmentName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}

public class CancelSubscriptionDto
{
    public bool CancelImmediately { get; set; }
    public string? Reason { get; set; }
}
