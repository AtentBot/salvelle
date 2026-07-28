using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("payment_methods")]
public class PaymentMethod
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    [Required]
    public Guid EstablishmentId { get; set; }

    [Column("stripe_payment_method_id")]
    [MaxLength(255)]
    public string? StripePaymentMethodId { get; set; }

    [Column("type")]
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "CARD"; // CARD, BOLETO, PIX

    [Column("external_payment_method_id")]
    [MaxLength(255)]
    public string? ExternalPaymentMethodId { get; set; }

    [Column("gateway_type")]
    [MaxLength(20)]
    public string? GatewayType { get; set; }

    [Column("card_brand")]
    [MaxLength(50)]
    public string? CardBrand { get; set; }

    [Column("card_last4")]
    [MaxLength(4)]
    public string? CardLast4 { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("EstablishmentId")]
    public Establishment? Establishment { get; set; }
}
