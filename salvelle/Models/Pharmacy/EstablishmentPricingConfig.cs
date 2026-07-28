using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Configuração de precificação por estabelecimento
/// Define taxas, impostos e markup aplicados nos orçamentos
/// </summary>
[Table("establishment_pricing_config")]
public class EstablishmentPricingConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // IMPOSTOS E TAXAS
    // ═══════════════════════════════════════════════════════════════

    [Column("tax_percentage", TypeName = "decimal(5,2)")]
    public decimal TaxPercentage { get; set; } = 0;

    [Column("fee_1_name")]
    [MaxLength(50)]
    public string Fee1Name { get; set; } = "Taxa Operacional";

    [Column("fee_1_percentage", TypeName = "decimal(5,2)")]
    public decimal Fee1Percentage { get; set; } = 0;

    [Column("fee_2_name")]
    [MaxLength(50)]
    public string Fee2Name { get; set; } = "Mão de Obra";

    [Column("fee_2_percentage", TypeName = "decimal(5,2)")]
    public decimal Fee2Percentage { get; set; } = 0;

    [Column("fee_3_name")]
    [MaxLength(50)]
    public string Fee3Name { get; set; } = "Outros Custos";

    [Column("fee_3_percentage", TypeName = "decimal(5,2)")]
    public decimal Fee3Percentage { get; set; } = 0;

    // ═══════════════════════════════════════════════════════════════
    // MARGEM E CUSTOS
    // ═══════════════════════════════════════════════════════════════

    [Column("markup_percentage", TypeName = "decimal(5,2)")]
    public decimal MarkupPercentage { get; set; } = 100;

    [Column("packaging_percentage", TypeName = "decimal(5,2)")]
    public decimal PackagingPercentage { get; set; } = 5;

    /// <summary>
    /// Taxa fixa de manipulação (R$)
    /// </summary>
    [Column("manipulation_fee", TypeName = "decimal(10,2)")]
    public decimal? ManipulationFee { get; set; } = 10;

    // ═══════════════════════════════════════════════════════════════
    // CONFIGURAÇÕES DE ORÇAMENTO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Dias de validade do orçamento
    /// </summary>
    [Column("quote_validity_days")]
    public int? QuoteValidityDays { get; set; } = 7;

    /// <summary>
    /// Taxa de inflação mensal para correção de preços históricos (%)
    /// </summary>
    [Column("inflation_rate_monthly", TypeName = "decimal(5,2)")]
    public decimal? InflationRateMonthly { get; set; } = 0.5m;

    /// <summary>
    /// Margem de segurança para preços históricos (%)
    /// </summary>
    [Column("safety_margin_percent", TypeName = "decimal(5,2)")]
    public decimal? SafetyMarginPercent { get; set; } = 5;

    // ═══════════════════════════════════════════════════════════════
    // CONFIGURAÇÕES DE CÁLCULO
    // ═══════════════════════════════════════════════════════════════

    [Column("apply_minimum_price")]
    public bool ApplyMinimumPrice { get; set; } = true;

    [Column("round_to_cents")]
    public bool RoundToCents { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // AUDITORIA
    // ═══════════════════════════════════════════════════════════════

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey(nameof(UpdatedByEmployeeId))]
    public virtual Employee? UpdatedByEmployee { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // MÉTODOS DE CÁLCULO
    // ═══════════════════════════════════════════════════════════════

    [NotMapped]
    public decimal TotalAdditionsPercentage =>
        TaxPercentage + Fee1Percentage + Fee2Percentage + Fee3Percentage +
        MarkupPercentage + PackagingPercentage;

    public decimal CalculateFinalPrice(decimal baseCost, decimal? minimumPrice = null)
    {
        var taxValue = baseCost * (TaxPercentage / 100m);
        var fee1Value = baseCost * (Fee1Percentage / 100m);
        var fee2Value = baseCost * (Fee2Percentage / 100m);
        var fee3Value = baseCost * (Fee3Percentage / 100m);
        var markupValue = baseCost * (MarkupPercentage / 100m);
        var packagingValue = baseCost * (PackagingPercentage / 100m);

        var calculatedPrice = baseCost + taxValue + fee1Value + fee2Value + 
                              fee3Value + markupValue + packagingValue;

        if (ApplyMinimumPrice && minimumPrice.HasValue && calculatedPrice < minimumPrice.Value)
        {
            calculatedPrice = minimumPrice.Value;
        }

        if (RoundToCents)
        {
            calculatedPrice = Math.Round(calculatedPrice, 2);
        }

        return calculatedPrice;
    }

    public PriceBreakdown GetPriceBreakdown(decimal baseCost, decimal? minimumPrice = null)
    {
        var breakdown = new PriceBreakdown
        {
            BaseCost = baseCost,
            TaxValue = baseCost * (TaxPercentage / 100m),
            TaxPercentage = TaxPercentage,
            Fee1Name = Fee1Name,
            Fee1Value = baseCost * (Fee1Percentage / 100m),
            Fee1Percentage = Fee1Percentage,
            Fee2Name = Fee2Name,
            Fee2Value = baseCost * (Fee2Percentage / 100m),
            Fee2Percentage = Fee2Percentage,
            Fee3Name = Fee3Name,
            Fee3Value = baseCost * (Fee3Percentage / 100m),
            Fee3Percentage = Fee3Percentage,
            MarkupValue = baseCost * (MarkupPercentage / 100m),
            MarkupPercentage = MarkupPercentage,
            PackagingValue = baseCost * (PackagingPercentage / 100m),
            PackagingPercentage = PackagingPercentage,
            MinimumPrice = minimumPrice
        };

        breakdown.CalculatedPrice = baseCost + breakdown.TaxValue + breakdown.Fee1Value +
                                    breakdown.Fee2Value + breakdown.Fee3Value +
                                    breakdown.MarkupValue + breakdown.PackagingValue;

        breakdown.MinimumPriceApplied = ApplyMinimumPrice && 
                                         minimumPrice.HasValue && 
                                         breakdown.CalculatedPrice < minimumPrice.Value;

        breakdown.FinalPrice = breakdown.MinimumPriceApplied 
            ? minimumPrice!.Value 
            : breakdown.CalculatedPrice;

        if (RoundToCents)
        {
            breakdown.FinalPrice = Math.Round(breakdown.FinalPrice, 2);
        }

        return breakdown;
    }
}

public class PriceBreakdown
{
    public decimal BaseCost { get; set; }
    
    public decimal TaxValue { get; set; }
    public decimal TaxPercentage { get; set; }
    
    public string Fee1Name { get; set; } = string.Empty;
    public decimal Fee1Value { get; set; }
    public decimal Fee1Percentage { get; set; }
    
    public string Fee2Name { get; set; } = string.Empty;
    public decimal Fee2Value { get; set; }
    public decimal Fee2Percentage { get; set; }
    
    public string Fee3Name { get; set; } = string.Empty;
    public decimal Fee3Value { get; set; }
    public decimal Fee3Percentage { get; set; }
    
    public decimal MarkupValue { get; set; }
    public decimal MarkupPercentage { get; set; }
    
    public decimal PackagingValue { get; set; }
    public decimal PackagingPercentage { get; set; }
    
    public decimal CalculatedPrice { get; set; }
    public decimal? MinimumPrice { get; set; }
    public bool MinimumPriceApplied { get; set; }
    public decimal FinalPrice { get; set; }

    public decimal TotalAdditions => TaxValue + Fee1Value + Fee2Value + 
                                      Fee3Value + MarkupValue + PackagingValue;
}
