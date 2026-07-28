using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("product_ratings")]
public class ProductRating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("catalog_product_id")]
    public Guid CatalogProductId { get; set; }

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

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CatalogProductId")]
    public virtual CatalogProduct? Product { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("OrderId")]
    public virtual OnlineOrder? Order { get; set; }
}
