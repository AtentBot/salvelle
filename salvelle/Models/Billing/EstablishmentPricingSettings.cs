using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Configurações de precificação por estabelecimento
/// Tabela: establishment_pricing_settings
/// </summary>
[Table("establishment_pricing_settings")]
public class EstablishmentPricingSettings
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    /// <summary>
    /// Taxa de inflação mensal (%) - Correção aplicada a preços históricos
    /// </summary>
    [Column("inflation_rate_monthly")]
    public decimal InflationRateMonthly { get; set; } = 0.5m;

    /// <summary>
    /// Margem de segurança (%) - Adicionada a preços de tabela base
    /// </summary>
    [Column("safety_margin_percent")]
    public decimal SafetyMarginPercent { get; set; } = 10m;

    /// <summary>
    /// Margem de lucro padrão (%) - Aplicada sobre o custo total
    /// </summary>
    [Column("default_profit_margin")]
    public decimal DefaultProfitMargin { get; set; } = 100m;

    /// <summary>
    /// Validade do preço em dias - Após esse período, alerta de desatualização
    /// </summary>
    [Column("price_validity_days")]
    public int PriceValidityDays { get; set; } = 180;

    /// <summary>
    /// Taxa de manipulação (R$) - Valor fixo por fórmula
    /// </summary>
    [Column("manipulation_fee")]
    public decimal ManipulationFee { get; set; } = 25m;

    /// <summary>
    /// Custo de embalagem padrão (R$) - Usado quando não há custo específico
    /// </summary>
    [Column("default_packaging_cost")]
    public decimal DefaultPackagingCost { get; set; } = 5m;

    /// <summary>
    /// Alertar quando usar preço estimado (não em estoque)
    /// </summary>
    [Column("alert_on_estimated")]
    public bool AlertOnEstimated { get; set; } = true;

    /// <summary>
    /// Bloquear venda sem estoque
    /// </summary>
    [Column("block_without_stock")]
    public bool BlockWithoutStock { get; set; } = false;

    /// <summary>
    /// Data de criação
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data de última atualização
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ═══════════════════════════════════════════════════════════════════════════

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
}