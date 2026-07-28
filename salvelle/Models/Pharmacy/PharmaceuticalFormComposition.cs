using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

/// <summary>
/// Composição de matérias-primas de um subtipo de forma farmacêutica
/// Ex: Creme Lanette = Lanette N 40% + Sorbitol 15% + ...
/// </summary>
[Table("pharmaceutical_form_compositions")]
public class PharmaceuticalFormComposition
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("subtype_id")]
    public Guid SubtypeId { get; set; }

    [Column("raw_material_id")]
    public Guid RawMaterialId { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // QUANTIDADE
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Percentual na composição (ex: 40 para 40%)
    /// </summary>
    [Column("percentage", TypeName = "decimal(8,4)")]
    public decimal? Percentage { get; set; }

    /// <summary>
    /// Quantidade por rendimento padrão (ex: 40g para 100g de creme)
    /// </summary>
    [Column("quantity_per_yield", TypeName = "decimal(18,4)")]
    public decimal? QuantityPerYield { get; set; }

    /// <summary>
    /// Unidade de medida
    /// </summary>
    [Column("unit")]
    [MaxLength(20)]
    public string Unit { get; set; } = "g";

    // ═══════════════════════════════════════════════════════════════
    // FLAGS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Se é "quantidade suficiente para" (qsp)
    /// </summary>
    [Column("is_qsp")]
    public bool IsQsp { get; set; } = false;

    /// <summary>
    /// Se é um componente opcional
    /// </summary>
    [Column("is_optional")]
    public bool IsOptional { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════
    // ORDENAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [Column("sort_order")]
    public int SortOrder { get; set; } = 100;

    // ═══════════════════════════════════════════════════════════════
    // AUDITORIA
    // ═══════════════════════════════════════════════════════════════

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [ForeignKey(nameof(SubtypeId))]
    public virtual PharmaceuticalFormSubtype? Subtype { get; set; }

    [ForeignKey(nameof(RawMaterialId))]
    public virtual RawMaterial? RawMaterial { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula a quantidade necessária para uma produção específica
    /// </summary>
    /// <param name="productionQuantity">Quantidade a produzir</param>
    /// <returns>Quantidade do componente necessária</returns>
    public decimal CalculateQuantityFor(decimal productionQuantity)
    {
        if (IsQsp)
            return 0; // QSP é calculado à parte

        var yieldQuantity = Subtype?.YieldQuantity ?? 100;

        if (Percentage.HasValue)
        {
            return (Percentage.Value / 100m) * productionQuantity;
        }

        if (QuantityPerYield.HasValue)
        {
            return (QuantityPerYield.Value / yieldQuantity) * productionQuantity;
        }

        return 0;
    }

    /// <summary>
    /// Calcula o custo deste componente para uma produção específica
    /// </summary>
    /// <param name="productionQuantity">Quantidade a produzir</param>
    /// <param name="unitPrice">Preço unitário da matéria-prima</param>
    /// <returns>Custo total do componente</returns>
    public decimal CalculateCostFor(decimal productionQuantity, decimal unitPrice)
    {
        var quantity = CalculateQuantityFor(productionQuantity);
        return quantity * unitPrice;
    }
}
