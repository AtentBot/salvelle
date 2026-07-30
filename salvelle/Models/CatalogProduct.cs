using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("CatalogProducts")]
public class CatalogProduct
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid EstablishmentId { get; set; }
    
    public Guid? CategoryId { get; set; }
    
    [StringLength(50)]
    public string? Code { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string? ShortDescription { get; set; }
    
    public string? Description { get; set; }
    
    public string? Composition { get; set; }
    
    [StringLength(200)]
    public string? Dosage { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? PromotionalPrice { get; set; }
    
    public DateTime? PromotionEndsAt { get; set; }
    
    public int StockQuantity { get; set; }
    
    [StringLength(20)]
    public string Unit { get; set; } = "UN";
    
    public bool IsHighlight { get; set; }
    
    public bool IsBestSeller { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }

    // ========== MARKETPLACE ==========
    [Column("average_rating", TypeName = "decimal(3,2)")]
    public decimal AverageRating { get; set; } = 0;

    public int TotalRatings { get; set; } = 0;

    public int TotalSold { get; set; } = 0;

    public bool IsMarketplaceVisible { get; set; } = true;

    // ========== CONTROLE SANITÁRIO (Portaria 344/98) ==========
    /// <summary>
    /// Substância controlada (Portaria 344). Medicamento controlado NÃO pode ser vendido OTC
    /// pelo marketplace: é ocultado das listagens e bloqueado no carrinho/checkout.
    /// </summary>
    [Column("is_controlled")]
    public bool IsControlled { get; set; } = false;

    /// <summary>Exige receita médica para dispensação (bloqueia a compra OTC no marketplace).</summary>
    [Column("requires_prescription")]
    public bool RequiresPrescription { get; set; } = false;

    [MaxLength(500)]
    public string? SearchKeywords { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    // Navigation
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
    
    [ForeignKey("CategoryId")]
    public virtual CatalogCategory? Category { get; set; }
    
    // Computed
    [NotMapped]
    public decimal CurrentPrice => IsOnPromotion ? PromotionalPrice!.Value : Price;
    
    [NotMapped]
    public bool IsOnPromotion => PromotionalPrice.HasValue && 
                                  PromotionalPrice.Value < Price && 
                                  (!PromotionEndsAt.HasValue || PromotionEndsAt.Value > DateTime.UtcNow);
    
    [NotMapped]
    public int DiscountPercent => IsOnPromotion 
        ? (int)Math.Round((1 - (PromotionalPrice!.Value / Price)) * 100) 
        : 0;
}
