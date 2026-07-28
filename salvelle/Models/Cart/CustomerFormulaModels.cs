using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Pharmacy;

namespace Models.Cart;

// ══════════════════════════════════════════════════════════════════════════════
// CARRINHO DE COMPRAS (FÓRMULAS PERSONALIZADAS)
// ══════════════════════════════════════════════════════════════════════════════
// NOTA: ProductType, ProductSubType, CustomerFormula e CustomerFormulaIngredient
// estão em Models.Pharmacy - NÃO duplicar aqui!
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Carrinho do cliente para fórmulas personalizadas
/// </summary>
[Table("carts")]
public class Cart
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    /// <summary>
    /// ATIVO, CONVERTIDO, ABANDONADO
    /// </summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ATIVO";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    public virtual ICollection<FormulaCartItem> Items { get; set; } = new List<FormulaCartItem>();

    // Computed
    [NotMapped]
    public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;

    [NotMapped]
    public decimal TotalValue => Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
}

/// <summary>
/// Item do carrinho de fórmulas personalizadas
/// RENOMEADO de CartItem para FormulaCartItem para evitar conflito com Models.Cart.CartItem
/// </summary>
[Table("cart_items")]
public class FormulaCartItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("cart_id")]
    public Guid CartId { get; set; }

    /// <summary>
    /// Fórmula personalizada do cliente
    /// </summary>
    [Column("customer_formula_id")]
    public Guid? CustomerFormulaId { get; set; }

    /// <summary>
    /// Produto do catálogo (se não for fórmula personalizada)
    /// </summary>
    [Column("catalog_product_id")]
    public Guid? CatalogProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("unit_price", TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CartId")]
    public virtual Cart? Cart { get; set; }

    [ForeignKey("CustomerFormulaId")]
    public virtual CustomerFormula? CustomerFormula { get; set; }

    [ForeignKey("CatalogProductId")]
    public virtual CatalogProduct? CatalogProduct { get; set; }

    // Computed
    [NotMapped]
    public decimal TotalPrice => Quantity * UnitPrice;
}
