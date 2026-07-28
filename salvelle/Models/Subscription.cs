using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("subscriptions")]
public class Subscription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("subscription_plan_id")]
    public Guid SubscriptionPlanId { get; set; }

    [Column("stripe_subscription_id")]
    [StringLength(255)]
    public string? StripeSubscriptionId { get; set; }

    [Column("stripe_customer_id")]
    [StringLength(255)]
    public string? StripeCustomerId { get; set; }

    [Column("gateway_config_id")]
    public Guid? GatewayConfigId { get; set; }

    [Column("external_subscription_id")]
    [StringLength(255)]
    public string? ExternalSubscriptionId { get; set; }

    [Column("external_customer_id")]
    [StringLength(255)]
    public string? ExternalCustomerId { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "TRIALING";

    [Column("billing_cycle")]
    [StringLength(20)]
    public string BillingCycle { get; set; } = "MONTHLY";

    [Column("current_period_start")]
    public DateTime? CurrentPeriodStart { get; set; }

    [Column("current_period_end")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [Column("trial_start")]
    public DateTime? TrialStart { get; set; }

    [Column("trial_end")]
    public DateTime? TrialEnd { get; set; }

    [Column("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; set; }

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("SubscriptionPlanId")]
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }

    [ForeignKey("GatewayConfigId")]
    public virtual PaymentGatewayConfig? GatewayConfig { get; set; }
}