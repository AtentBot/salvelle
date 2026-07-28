using System.ComponentModel.DataAnnotations;

namespace DTOs.Pricing;

/// <summary>
/// Resultado da consulta de preço de um ingrediente
/// </summary>
public class IngredientPriceResult
{
    /// <summary>
    /// ID da matéria-prima
    /// </summary>
    public Guid RawMaterialId { get; set; }

    /// <summary>
    /// Nome da matéria-prima
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Preço por unidade (R$/unidade)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Unidade de medida (mg, mcg, UI, bilhões UFC, etc.)
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Origem do preço: ESTOQUE, HISTORICO, BASE
    /// </summary>
    public PriceSource Source { get; set; }

    /// <summary>
    /// Nível de confiança do preço (0-100%)
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// Data da última atualização do preço
    /// </summary>
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// ID do lote (se origem = ESTOQUE)
    /// </summary>
    public Guid? BatchId { get; set; }

    /// <summary>
    /// Número do lote (se origem = ESTOQUE)
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Quantidade disponível em estoque
    /// </summary>
    public decimal AvailableStock { get; set; }

    /// <summary>
    /// Data de validade do lote (se origem = ESTOQUE)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Indica se o ingrediente é virtual (nunca teve em estoque)
    /// </summary>
    public bool IsVirtual { get; set; }

    /// <summary>
    /// Mensagem de aviso (ex: "Preço desatualizado", "Sem estoque")
    /// </summary>
    public string? Warning { get; set; }

    /// <summary>
    /// Ícone sugerido para exibição (🟢, 🟡, 🔴)
    /// </summary>
    public string StatusIcon => Source switch
    {
        PriceSource.ESTOQUE => "🟢",
        PriceSource.HISTORICO => "🟡",
        PriceSource.BASE => "🔴",
        _ => "⚪"
    };

    /// <summary>
    /// Texto do status para exibição
    /// </summary>
    public string StatusText => Source switch
    {
        PriceSource.ESTOQUE => "Em Estoque",
        PriceSource.HISTORICO => "Preço Estimado",
        PriceSource.BASE => "Tabela Base",
        _ => "Desconhecido"
    };
}

/// <summary>
/// Origem do preço
/// </summary>
public enum PriceSource
{
    /// <summary>
    /// Preço de lote aprovado em estoque (maior confiança)
    /// </summary>
    ESTOQUE = 1,

    /// <summary>
    /// Último preço pago + correção (confiança média)
    /// </summary>
    HISTORICO = 2,

    /// <summary>
    /// Preço de referência de mercado (menor confiança)
    /// </summary>
    BASE = 3
}

/// <summary>
/// Resultado do cálculo de preço de uma fórmula completa
/// </summary>
public class FormulaPriceResult
{
    /// <summary>
    /// Lista de ingredientes com seus preços
    /// </summary>
    public List<FormulaIngredientPrice> Ingredients { get; set; } = new();

    /// <summary>
    /// Custo total dos ingredientes
    /// </summary>
    public decimal TotalIngredientsCost { get; set; }

    /// <summary>
    /// Custo de manipulação (mão de obra)
    /// </summary>
    public decimal ManipulationCost { get; set; }

    /// <summary>
    /// Custo de embalagem
    /// </summary>
    public decimal PackagingCost { get; set; }

    /// <summary>
    /// Custo total antes da margem
    /// </summary>
    public decimal TotalCost => TotalIngredientsCost + ManipulationCost + PackagingCost;

    /// <summary>
    /// Margem de lucro aplicada (%)
    /// </summary>
    public decimal ProfitMargin { get; set; }

    /// <summary>
    /// Preço final sugerido
    /// </summary>
    public decimal SuggestedPrice { get; set; }

    /// <summary>
    /// Confiança média do orçamento (0-100%)
    /// </summary>
    public int AverageConfidence { get; set; }

    /// <summary>
    /// Quantidade de ingredientes em estoque
    /// </summary>
    public int InStockCount { get; set; }

    /// <summary>
    /// Quantidade de ingredientes com preço estimado
    /// </summary>
    public int EstimatedCount { get; set; }

    /// <summary>
    /// Quantidade de ingredientes com preço base
    /// </summary>
    public int BaseCount { get; set; }

    /// <summary>
    /// Avisos gerais sobre o orçamento
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Indica se todos os ingredientes estão em estoque
    /// </summary>
    public bool AllInStock => EstimatedCount == 0 && BaseCount == 0;

    /// <summary>
    /// Resumo do breakdown por origem
    /// </summary>
    public PriceBreakdown Breakdown { get; set; } = new();
}

/// <summary>
/// Preço de um ingrediente dentro de uma fórmula
/// </summary>
public class FormulaIngredientPrice
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public PriceSource Source { get; set; }
    public int Confidence { get; set; }
    public string? Warning { get; set; }
    public string StatusIcon => Source switch
    {
        PriceSource.ESTOQUE => "🟢",
        PriceSource.HISTORICO => "🟡",
        PriceSource.BASE => "🔴",
        _ => "⚪"
    };
}

/// <summary>
/// Breakdown do custo por origem de preço
/// </summary>
public class PriceBreakdown
{
    /// <summary>
    /// Custo de ingredientes em estoque
    /// </summary>
    public decimal InStockCost { get; set; }

    /// <summary>
    /// Custo de ingredientes com preço histórico
    /// </summary>
    public decimal HistoricalCost { get; set; }

    /// <summary>
    /// Custo de ingredientes com preço base
    /// </summary>
    public decimal BaseCost { get; set; }

    /// <summary>
    /// Quantidade de itens em cada categoria
    /// </summary>
    public int InStockItems { get; set; }
    public int HistoricalItems { get; set; }
    public int BaseItems { get; set; }
}

/// <summary>
/// DTO para configurações de precificação do estabelecimento
/// NOTA: O Model está em Models.EstablishmentPricingSettings
/// </summary>
public class EstablishmentPricingSettings
{
    public Guid EstablishmentId { get; set; }

    /// <summary>
    /// Taxa de correção mensal para preços antigos (%)
    /// </summary>
    [Range(0, 10)]
    public decimal InflationRateMonthly { get; set; } = 0.5m;

    /// <summary>
    /// Margem de segurança para preços estimados (%)
    /// </summary>
    [Range(0, 50)]
    public decimal SafetyMarginPercent { get; set; } = 10m;

    /// <summary>
    /// Alertar quando usar preço estimado
    /// </summary>
    public bool AlertOnEstimated { get; set; } = true;

    /// <summary>
    /// Bloquear manipulação sem estoque real
    /// </summary>
    public bool BlockWithoutStock { get; set; } = false;

    /// <summary>
    /// Dias até considerar preço desatualizado
    /// </summary>
    [Range(30, 365)]
    public int PriceValidityDays { get; set; } = 180;

    /// <summary>
    /// Margem de lucro padrão (%)
    /// </summary>
    [Range(0, 500)]
    public decimal DefaultProfitMargin { get; set; } = 100m;

    /// <summary>
    /// Custo fixo de manipulação (R$)
    /// </summary>
    [Range(0, 1000)]
    public decimal ManipulationFee { get; set; } = 25m;

    /// <summary>
    /// Custo de embalagem padrão (R$)
    /// </summary>
    [Range(0, 100)]
    public decimal DefaultPackagingCost { get; set; } = 5m;
}

/// <summary>
/// Lista de ingredientes que precisam ser comprados
/// </summary>
public class PurchaseListResult
{
    public List<PurchaseListItem> Items { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
    public int TotalItems { get; set; }
}

/// <summary>
/// Item da lista de compras
/// </summary>
public class PurchaseListItem
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal QuantityNeeded { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal QuantityToBuy { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public string? LastSupplier { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public string Priority { get; set; } = "NORMAL"; // URGENT, HIGH, NORMAL, LOW
}

/// <summary>
/// DTO para Autocomplete de ingredientes
/// </summary>
public class IngredientAutocompleteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public string? DcbCode { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? LastKnownPrice { get; set; }
    public string PriceSource { get; set; } = "BASE";
    public decimal UnitCost { get; set; }
}
