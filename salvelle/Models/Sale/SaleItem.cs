using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Pharmacy;

namespace Models;

[Table("sale_items")]
public class SaleItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    public Guid SaleId { get; set; }

    // ==================== REFERÊNCIAS DE ORIGEM ====================
    
    /// <summary>
    /// Ordem de manipulação (quando é venda de manipulado)
    /// </summary>
    [Column("manipulation_order_id")]
    public Guid? ManipulationOrderId { get; set; }

    /// <summary>
    /// Prescrição vinculada
    /// </summary>
    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    /// <summary>
    /// Fórmula usada na manipulação
    /// </summary>
    [Column("formula_id")]
    public Guid? FormulaId { get; set; }

    /// <summary>
    /// Produto do catálogo (quando é venda de produto de prateleira)
    /// </summary>
    [Column("catalog_product_id")]
    public Guid? CatalogProductId { get; set; }

    /// <summary>
    /// Matéria-prima (quando é venda direta de produto pronto - Paracetamol, Diazepam, etc.)
    /// </summary>
    [Column("raw_material_id")]
    public Guid? RawMaterialId { get; set; }

    // ==================== DADOS DO ITEM ====================

    [Required]
    [Column("description")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("discount_percentage")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("cost_price")]
    public decimal CostPrice { get; set; } = 0;

    [Column("profit_margin")]
    public decimal ProfitMargin { get; set; } = 0;

    // ==================== CONTROLE DE SUBSTÂNCIAS ====================

    /// <summary>
    /// Tipo de controle para substâncias controladas (LISTA_A, LISTA_B, etc.)
    /// </summary>
    [Column("control_type")]
    [MaxLength(20)]
    public string? ControlType { get; set; }

    /// <summary>
    /// Número da receita (obrigatório para controlados)
    /// </summary>
    [Column("prescription_number")]
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    // ==================== OBSERVAÇÕES ====================

    [Column("observations")]
    [MaxLength(1000)]
    public string? Observations { get; set; }

    // ==================== NAVEGAÇÃO ====================

    [ForeignKey("SaleId")]
    public virtual Sale Sale { get; set; } = null!;

    [ForeignKey("CatalogProductId")]
    public virtual CatalogProduct? CatalogProduct { get; set; }

    [ForeignKey("RawMaterialId")]
    public virtual RawMaterial? RawMaterial { get; set; }

    [ForeignKey("ManipulationOrderId")]
    public virtual ManipulationOrder? ManipulationOrder { get; set; }
}
