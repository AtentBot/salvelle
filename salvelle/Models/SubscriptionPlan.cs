using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("subscription_plans")]
public class SubscriptionPlan
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("mercadopago_plan_id_monthly")]
    [MaxLength(255)]
    public string? MercadoPagoPlanIdMonthly { get; set; }

    [Column("mercadopago_plan_id_yearly")]
    [MaxLength(255)]
    public string? MercadoPagoPlanIdYearly { get; set; }

    [Column("abacatepay_plan_id_monthly")]
    [MaxLength(255)]
    public string? AbacatepayPlanIdMonthly { get; set; }

    [Column("abacatepay_plan_id_yearly")]
    [MaxLength(255)]
    public string? AbacatepayPlanIdYearly { get; set; }

    [Column("price_monthly")]
    [Required]
    public decimal PriceMonthly { get; set; }

    [Column("price_yearly")]
    [Required]
    public decimal PriceYearly { get; set; }

    [Column("max_employees")]
    public int? MaxEmployees { get; set; }

    [Column("max_monthly_orders")]
    public int? MaxMonthlyOrders { get; set; }

    [Column("features")]
    [Required]
    public string Features { get; set; } = "{}"; // JSON

    [Column("stripe_price_id_monthly")]
    [MaxLength(255)]
    public string? StripePriceIdMonthly { get; set; }

    [Column("stripe_price_id_yearly")]
    [MaxLength(255)]
    public string? StripePriceIdYearly { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
