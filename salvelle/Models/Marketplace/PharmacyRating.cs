using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("pharmacy_ratings")]
public class PharmacyRating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("order_id")]
    public Guid? OrderId { get; set; }

    [Required]
    [Column("rating")]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Column("comment")]
    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Column("pharmacy_response")]
    [MaxLength(1000)]
    public string? PharmacyResponse { get; set; }

    [Column("pharmacy_responded_at")]
    public DateTime? PharmacyRespondedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("OrderId")]
    public virtual OnlineOrder? Order { get; set; }
}
