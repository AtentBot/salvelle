using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("platform_transactions")]
public class PlatformTransaction
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [Column("gross_amount", TypeName = "decimal(18,2)")]
    public decimal GrossAmount { get; set; }

    [Column("commission_rate", TypeName = "decimal(5,4)")]
    public decimal CommissionRate { get; set; }

    [Column("commission_amount", TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; }

    [Column("net_amount_to_pharmacy", TypeName = "decimal(18,2)")]
    public decimal NetAmountToPharmacy { get; set; }

    [Column("stripe_payment_intent_id")]
    [MaxLength(200)]
    public string? StripePaymentIntentId { get; set; }

    [Column("stripe_transfer_id")]
    [MaxLength(200)]
    public string? StripeTransferId { get; set; }

    /// <summary>
    /// PENDENTE, CAPTURADO, REPASSADO, ESTORNADO
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public virtual OnlineOrder? Order { get; set; }

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}
