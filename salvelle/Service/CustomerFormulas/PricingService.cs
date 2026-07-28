using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Data;
using Models.Pharmacy;
using Models;

namespace Service.CustomerFormulas;

public class PricingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PricingService> _logger;

    // Configurações de precificação (podem vir de appsettings.json)
    private const decimal DEFAULT_RAW_MATERIAL_COST_PER_GRAM = 0.50m;
    private const decimal DEFAULT_PROFIT_MARGIN = 0.60m; // 60%
    private const decimal DEFAULT_TAX_RATE = 0.15m; // 15%

    // Cache de configurações por estabelecimento
    private readonly Dictionary<Guid, EstablishmentPricingSettings> _settingsCache = new();

    public PricingService(
        AppDbContext context,
        ILogger<PricingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MÉTODOS ORIGINAIS (Fórmulas por ProductSubType)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcular preço estimado de uma fórmula
    /// </summary>
    public async Task<decimal> CalculatePriceAsync(
        Guid productSubTypeId,
        decimal quantity,
        Guid establishmentId,
        string unit = "g")
    {
        try
        {
            // 1. Buscar subtipo do produto
            var subType = await _context.ProductSubTypes
                .FirstOrDefaultAsync(pst => pst.Id == productSubTypeId);

            if (subType == null)
                throw new Exception("Subtipo de produto não encontrado");

            // 2. Normalizar quantidade para gramas (unidade base)
            var quantityInGrams = ConvertToGrams(quantity, unit);

            // 3. Calcular custo base de matérias-primas
            var rawMaterialCost = quantityInGrams * DEFAULT_RAW_MATERIAL_COST_PER_GRAM;

            // 4. Custo de manipulação (fixo do subtipo)
            var manipulationCost = subType.ManipulationCostBase;

            // 5. Aplicar modificador de preço do subtipo
            var baseCost = (rawMaterialCost + manipulationCost) * subType.PriceModifier;

            // 6. Adicionar margem de lucro
            var priceWithMargin = baseCost * (1 + DEFAULT_PROFIT_MARGIN);

            // 7. Adicionar impostos
            var finalPrice = priceWithMargin * (1 + DEFAULT_TAX_RATE);

            // 8. Arredondar para 2 casas decimais
            var roundedPrice = Math.Round(finalPrice, 2);

            _logger.LogInformation(
                "Preço calculado para {SubType}: R$ {Price} (Qtd: {Quantity}{Unit})",
                subType.Name, roundedPrice, quantity, unit);

            return roundedPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular preço");
            throw;
        }
    }

    /// <summary>
    /// Calcular preço detalhado (com breakdown de custos)
    /// </summary>
    public async Task<PriceBreakdown> CalculatePriceDetailedAsync(
        Guid productSubTypeId,
        decimal quantity,
        Guid establishmentId,
        string unit = "g")
    {
        try
        {
            var subType = await _context.ProductSubTypes
                .FirstOrDefaultAsync(pst => pst.Id == productSubTypeId);

            if (subType == null)
                throw new Exception("Subtipo de produto não encontrado");

            var quantityInGrams = ConvertToGrams(quantity, unit);
            var rawMaterialCost = quantityInGrams * DEFAULT_RAW_MATERIAL_COST_PER_GRAM;
            var manipulationCost = subType.ManipulationCostBase;
            var baseCost = (rawMaterialCost + manipulationCost) * subType.PriceModifier;
            var profitAmount = baseCost * DEFAULT_PROFIT_MARGIN;
            var subtotal = baseCost + profitAmount;
            var taxAmount = subtotal * DEFAULT_TAX_RATE;
            var finalPrice = subtotal + taxAmount;

            return new PriceBreakdown
            {
                RawMaterialCost = Math.Round(rawMaterialCost, 2),
                ManipulationCost = Math.Round(manipulationCost, 2),
                BaseCost = Math.Round(baseCost, 2),
                PriceModifier = subType.PriceModifier,
                ProfitMargin = DEFAULT_PROFIT_MARGIN,
                ProfitAmount = Math.Round(profitAmount, 2),
                Subtotal = Math.Round(subtotal, 2),
                TaxRate = DEFAULT_TAX_RATE,
                TaxAmount = Math.Round(taxAmount, 2),
                FinalPrice = Math.Round(finalPrice, 2),
                QuantityInGrams = quantityInGrams
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular preço detalhado");
            throw;
        }
    }

    /// <summary>
    /// Aplicar desconto a um preço
    /// </summary>
    public decimal ApplyDiscount(decimal price, decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 1)
            throw new ArgumentException("Desconto deve estar entre 0 e 1 (0% a 100%)");

        var discountAmount = price * discountPercentage;
        var finalPrice = price - discountAmount;

        return Math.Round(finalPrice, 2);
    }

    /// <summary>
    /// Converter unidades para gramas (unidade base)
    /// </summary>
    private decimal ConvertToGrams(decimal quantity, string unit)
    {
        return unit.ToLower() switch
        {
            "g" => quantity,
            "mg" => quantity / 1000m,
            "kg" => quantity * 1000m,
            "ml" => quantity, // Assumindo densidade ~1g/ml
            "l" => quantity * 1000m,
            "un" => quantity * 10m, // Assumir 10g por unidade
            _ => throw new ArgumentException($"Unidade '{unit}' não suportada")
        };
    }

    /// <summary>
    /// Recalcular preço de uma fórmula existente
    /// </summary>
    public async Task<bool> RecalculateFormulaPriceAsync(Guid formulaId)
    {
        var formula = await _context.CustomerFormulas
            .Include(cf => cf.ProductSubType)
            .FirstOrDefaultAsync(cf => cf.Id == formulaId);

        if (formula == null)
            return false;

        var newPrice = await CalculatePriceAsync(
            formula.ProductSubTypeId,
            formula.Quantity,
            formula.EstablishmentId,
            formula.Unit
        );

        formula.EstimatedPrice = newPrice;
        formula.FinalPrice = newPrice;
        formula.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Preço da fórmula {Code} recalculado: R$ {Price}",
            formula.Code, newPrice);

        return true;
    }

    // ════════════════════════════════════════════════════════════════════════
    // NOVOS MÉTODOS: Precificação Inteligente de Ingredientes
    // Hierarquia: ESTOQUE > HISTÓRICO > BASE
    // ════════════════════════════════════════════════════════════════════════

    #region Configurações de Precificação

    /// <summary>
    /// Obtém configurações de precificação do estabelecimento
    /// </summary>
    public async Task<EstablishmentPricingSettings> GetPricingSettingsAsync(Guid establishmentId)
    {
        if (_settingsCache.TryGetValue(establishmentId, out var cached))
            return cached;

        var settings = await _context.EstablishmentPricingSettings
            .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

        if (settings == null)
        {
            settings = new EstablishmentPricingSettings
            {
                EstablishmentId = establishmentId,
                InflationRateMonthly = 0.5m,
                SafetyMarginPercent = 10m,
                AlertOnEstimated = true,
                BlockWithoutStock = false,
                PriceValidityDays = 180,
                DefaultProfitMargin = 100m,
                ManipulationFee = 25m,
                DefaultPackagingCost = 5m
            };
        }

        _settingsCache[establishmentId] = settings;
        return settings;
    }

    /// <summary>
    /// Atualiza configurações de precificação
    /// </summary>
    public async Task<bool> UpdatePricingSettingsAsync(Guid establishmentId, EstablishmentPricingSettings newSettings)
    {
        try
        {
            var settings = await _context.EstablishmentPricingSettings
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

            if (settings == null)
            {
                settings = new EstablishmentPricingSettings
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = establishmentId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Add(settings);
            }

            settings.InflationRateMonthly = newSettings.InflationRateMonthly;
            settings.SafetyMarginPercent = newSettings.SafetyMarginPercent;
            settings.AlertOnEstimated = newSettings.AlertOnEstimated;
            settings.BlockWithoutStock = newSettings.BlockWithoutStock;
            settings.PriceValidityDays = newSettings.PriceValidityDays;
            settings.DefaultProfitMargin = newSettings.DefaultProfitMargin;
            settings.ManipulationFee = newSettings.ManipulationFee;
            settings.DefaultPackagingCost = newSettings.DefaultPackagingCost;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _settingsCache.Remove(establishmentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar configurações de precificação");
            return false;
        }
    }

    #endregion

    #region Preço de Ingrediente Individual

    /// <summary>
    /// Obtém preço de um ingrediente com hierarquia inteligente
    /// ESTOQUE (100%) > HISTÓRICO (50-95%) > BASE (30%)
    /// </summary>
    public async Task<IngredientPriceResult?> GetIngredientPriceAsync(
        Guid rawMaterialId,
        Guid establishmentId)
    {
        var settings = await GetPricingSettingsAsync(establishmentId);

        var rawMaterial = await _context.RawMaterials
            .FirstOrDefaultAsync(rm =>
                rm.Id == rawMaterialId &&
                rm.EstablishmentId == establishmentId &&
                rm.IsActive);

        if (rawMaterial == null)
            return null;

        // 1º PRIORIDADE: Batch APROVADO em estoque (FEFO)
        var batch = await _context.Batches
            .Where(b =>
                b.RawMaterialId == rawMaterialId &&
                b.Status.ToUpper() == "APROVADO" &&
                b.CurrentQuantity > 0 &&
                b.ExpiryDate > DateTime.UtcNow)
            .OrderBy(b => b.ExpiryDate)
            .FirstOrDefaultAsync();

        if (batch != null)
        {
            return new IngredientPriceResult
            {
                RawMaterialId = rawMaterialId,
                Name = rawMaterial.Name,
                Price = batch.UnitCost,
                Unit = rawMaterial.Unit,
                Source = PriceSource.ESTOQUE,
                Confidence = 100,
                LastUpdate = batch.ApprovalDate ?? batch.ReceivedDate,
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                AvailableStock = batch.CurrentQuantity,
                ExpiryDate = batch.ExpiryDate,
                IsVirtual = false
            };
        }

        // Calcular estoque total
        var totalStock = await _context.Batches
            .Where(b =>
                b.RawMaterialId == rawMaterialId &&
                b.Status.ToUpper() == "APROVADO" &&
                b.CurrentQuantity > 0 &&
                b.ExpiryDate > DateTime.UtcNow)
            .SumAsync(b => b.CurrentQuantity);

        // 2º PRIORIDADE: Último preço conhecido (com correção)
        if (rawMaterial.LastKnownPrice.HasValue && rawMaterial.LastKnownPrice > 0)
        {
            var monthsOld = rawMaterial.LastPriceDate.HasValue
                ? (int)((DateTime.UtcNow - rawMaterial.LastPriceDate.Value).TotalDays / 30)
                : 0;

            var adjustedPrice = rawMaterial.LastKnownPrice.Value *
                (decimal)Math.Pow((double)(1 + settings.InflationRateMonthly / 100), monthsOld);

            var confidence = Math.Max(50, 100 - (monthsOld * 4));

            var result = new IngredientPriceResult
            {
                RawMaterialId = rawMaterialId,
                Name = rawMaterial.Name,
                Price = Math.Round(adjustedPrice, 4),
                Unit = rawMaterial.Unit,
                Source = PriceSource.HISTORICO,
                Confidence = confidence,
                LastUpdate = rawMaterial.LastPriceDate,
                AvailableStock = totalStock,
                IsVirtual = rawMaterial.IsVirtual
            };

            if (monthsOld > settings.PriceValidityDays / 30)
                result.Warning = $"Preço desatualizado ({monthsOld} meses)";
            else if (monthsOld > 3)
                result.Warning = $"Última compra há {monthsOld} meses";

            return result;
        }

        // 3º PRIORIDADE: Preço base (com margem de segurança)
        if (rawMaterial.BasePrice.HasValue && rawMaterial.BasePrice > 0)
        {
            var adjustedPrice = rawMaterial.BasePrice.Value *
                (1 + settings.SafetyMarginPercent / 100);

            return new IngredientPriceResult
            {
                RawMaterialId = rawMaterialId,
                Name = rawMaterial.Name,
                Price = Math.Round(adjustedPrice, 4),
                Unit = rawMaterial.Unit,
                Source = PriceSource.BASE,
                Confidence = 30,
                LastUpdate = rawMaterial.UpdatedAt,
                AvailableStock = 0,
                IsVirtual = true,
                Warning = "Nunca adquirido - preço de referência"
            };
        }

        // Sem preço disponível
        return new IngredientPriceResult
        {
            RawMaterialId = rawMaterialId,
            Name = rawMaterial.Name,
            Price = 0,
            Unit = rawMaterial.Unit,
            Source = PriceSource.BASE,
            Confidence = 0,
            IsVirtual = true,
            Warning = "Sem preço cadastrado"
        };
    }

    /// <summary>
    /// Busca ingrediente por nome e retorna preço
    /// </summary>
    public async Task<IngredientPriceResult?> GetIngredientPriceByNameAsync(
        string name,
        Guid establishmentId)
    {
        var normalizedName = NormalizeName(name);

        var rawMaterial = await _context.RawMaterials
            .Where(rm =>
                rm.EstablishmentId == establishmentId &&
                rm.IsActive &&
                (rm.Name.ToLower().Contains(normalizedName) ||
                 (rm.Synonyms != null && rm.Synonyms.ToLower().Contains(normalizedName))))
            .OrderByDescending(rm => rm.Popularity)
            .FirstOrDefaultAsync();

        if (rawMaterial == null)
            return null;

        return await GetIngredientPriceAsync(rawMaterial.Id, establishmentId);
    }

    #endregion

    #region Preço de Fórmula com Ingredientes Reais

    /// <summary>
    /// Calcula preço de fórmula usando preços reais dos ingredientes
    /// </summary>
    public async Task<FormulaIngredientPriceResult> CalculateFormulaWithIngredientsAsync(
        List<FormulaIngredientInput> ingredients,
        int productQuantity,
        Guid establishmentId,
        string productType = "Cápsula")
    {
        var settings = await GetPricingSettingsAsync(establishmentId);
        var result = new FormulaIngredientPriceResult();

        foreach (var ingredient in ingredients)
        {
            IngredientPriceResult? priceResult = null;

            if (ingredient.RawMaterialId.HasValue)
            {
                priceResult = await GetIngredientPriceAsync(ingredient.RawMaterialId.Value, establishmentId);
            }
            else if (!string.IsNullOrEmpty(ingredient.Name))
            {
                priceResult = await GetIngredientPriceByNameAsync(ingredient.Name, establishmentId);
            }

            if (priceResult == null)
            {
                result.Ingredients.Add(new FormulaIngredientPriceItem
                {
                    Name = ingredient.Name ?? "Desconhecido",
                    Quantity = ingredient.Quantity * productQuantity,
                    Unit = ingredient.Unit,
                    UnitPrice = 0,
                    TotalPrice = 0,
                    Source = PriceSource.BASE,
                    Confidence = 0,
                    Warning = "Ingrediente não encontrado"
                });
                result.Warnings.Add($"Ingrediente '{ingredient.Name}' não encontrado");
                continue;
            }

            var totalQuantity = ingredient.Quantity * productQuantity;
            var totalPrice = totalQuantity * priceResult.Price;

            result.Ingredients.Add(new FormulaIngredientPriceItem
            {
                RawMaterialId = priceResult.RawMaterialId,
                Name = priceResult.Name,
                Quantity = totalQuantity,
                Unit = priceResult.Unit,
                UnitPrice = priceResult.Price,
                TotalPrice = Math.Round(totalPrice, 2),
                Source = priceResult.Source,
                Confidence = priceResult.Confidence,
                Warning = priceResult.Warning
            });

            // Atualizar contadores
            switch (priceResult.Source)
            {
                case PriceSource.ESTOQUE:
                    result.InStockCost += totalPrice;
                    result.InStockCount++;
                    break;
                case PriceSource.HISTORICO:
                    result.HistoricalCost += totalPrice;
                    result.EstimatedCount++;
                    break;
                case PriceSource.BASE:
                    result.BaseCost += totalPrice;
                    result.BaseCount++;
                    break;
            }
        }

        // Calcular totais
        result.TotalIngredientsCost = Math.Round(result.Ingredients.Sum(i => i.TotalPrice), 2);
        result.ManipulationCost = settings.ManipulationFee;
        result.PackagingCost = GetPackagingCost(productType, productQuantity, settings);
        result.TotalCost = result.TotalIngredientsCost + result.ManipulationCost + result.PackagingCost;
        result.ProfitMargin = settings.DefaultProfitMargin;
        result.SuggestedPrice = Math.Round(result.TotalCost * (1 + settings.DefaultProfitMargin / 100), 2);

        // Confiança média ponderada
        if (result.Ingredients.Any())
        {
            var totalWeight = result.Ingredients.Sum(i => i.TotalPrice);
            if (totalWeight > 0)
            {
                result.AverageConfidence = (int)(result.Ingredients
                    .Sum(i => i.Confidence * i.TotalPrice) / totalWeight);
            }
        }

        // Avisos
        if (result.EstimatedCount > 0)
            result.Warnings.Add($"{result.EstimatedCount} ingrediente(s) com preço estimado");
        if (result.BaseCount > 0)
            result.Warnings.Add($"{result.BaseCount} ingrediente(s) nunca adquirido(s)");
        if (result.AverageConfidence < 50)
            result.Warnings.Add("Orçamento com baixa confiança");

        return result;
    }

    /// <summary>
    /// Obtém custo de embalagem por tipo de produto
    /// </summary>
    private decimal GetPackagingCost(string productType, int quantity, EstablishmentPricingSettings settings)
    {
        return productType.ToLower() switch
        {
            "cápsula" or "capsula" => 3.00m,
            "comprimido" => 3.00m,
            "creme" or "pomada" or "gel" => 5.00m,
            "loção" or "locao" or "solução" or "solucao" => 6.00m,
            "xarope" => 8.00m,
            "spray" => 10.00m,
            "gotas" => 5.00m,
            "sachê" or "sache" => quantity * 0.30m,
            "pó" or "po" => 4.00m,
            _ => settings.DefaultPackagingCost
        };
    }

    #endregion

    #region Utilitários

    /// <summary>
    /// Normaliza nome para busca
    /// </summary>
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var normalized = name.ToLower().Trim();

        var replacements = new Dictionary<string, string>
        {
            { "á", "a" }, { "à", "a" }, { "ã", "a" }, { "â", "a" }, { "ä", "a" },
            { "é", "e" }, { "è", "e" }, { "ê", "e" }, { "ë", "e" },
            { "í", "i" }, { "ì", "i" }, { "î", "i" }, { "ï", "i" },
            { "ó", "o" }, { "ò", "o" }, { "õ", "o" }, { "ô", "o" }, { "ö", "o" },
            { "ú", "u" }, { "ù", "u" }, { "û", "u" }, { "ü", "u" },
            { "ç", "c" }, { "ñ", "n" }
        };

        foreach (var kvp in replacements)
            normalized = normalized.Replace(kvp.Key, kvp.Value);

        return normalized;
    }

    /// <summary>
    /// Atualiza o último preço conhecido (chamado quando batch é aprovado)
    /// </summary>
    public async Task UpdateLastKnownPriceAsync(Guid rawMaterialId, decimal unitCost)
    {
        var rawMaterial = await _context.RawMaterials.FindAsync(rawMaterialId);
        if (rawMaterial == null) return;

        rawMaterial.LastKnownPrice = unitCost;
        rawMaterial.LastPriceDate = DateTime.UtcNow;
        rawMaterial.IsVirtual = false;
        rawMaterial.PriceSource = "HISTORICO";
        rawMaterial.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Preço atualizado para {Name}: R$ {Price}/{Unit}",
            rawMaterial.Name, unitCost, rawMaterial.Unit);
    }

    /// <summary>
    /// Obtém estatísticas de precificação
    /// </summary>
    public async Task<PricingStatistics> GetPricingStatisticsAsync(Guid establishmentId)
    {
        var rawMaterials = await _context.RawMaterials
            .Where(rm => rm.EstablishmentId == establishmentId && rm.IsActive)
            .ToListAsync();

        var batches = await _context.Batches
            .Where(b =>
                b.RawMaterial!.EstablishmentId == establishmentId &&
                b.Status.ToUpper() == "APROVADO" &&
                b.CurrentQuantity > 0 &&
                b.ExpiryDate > DateTime.UtcNow)
            .ToListAsync();

        var inStockIds = batches.Select(b => b.RawMaterialId).Distinct().ToHashSet();

        return new PricingStatistics
        {
            TotalIngredients = rawMaterials.Count,
            InStockCount = inStockIds.Count,
            WithHistoricalPrice = rawMaterials.Count(rm => rm.LastKnownPrice.HasValue && !inStockIds.Contains(rm.Id)),
            VirtualCount = rawMaterials.Count(rm => rm.IsVirtual && !rm.LastKnownPrice.HasValue),
            OutdatedPriceCount = rawMaterials.Count(rm =>
                rm.LastPriceDate.HasValue &&
                (DateTime.UtcNow - rm.LastPriceDate.Value).TotalDays > 180),
            TotalStockValue = batches.Sum(b => b.CurrentQuantity * b.UnitCost)
        };
    }

    #endregion
}

// ════════════════════════════════════════════════════════════════════════
// DTOs
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Detalhamento de custos e preço final (original)
/// </summary>
public class PriceBreakdown
{
    public decimal RawMaterialCost { get; set; }
    public decimal ManipulationCost { get; set; }
    public decimal BaseCost { get; set; }
    public decimal PriceModifier { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal QuantityInGrams { get; set; }
}

/// <summary>
/// Origem do preço do ingrediente
/// </summary>
public enum PriceSource
{
    ESTOQUE = 1,    // Lote aprovado em estoque (100% confiança)
    HISTORICO = 2,  // Último preço pago (50-95% confiança)
    BASE = 3        // Preço de referência (30% confiança)
}

/// <summary>
/// Resultado do preço de um ingrediente
/// </summary>
public class IngredientPriceResult
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Unit { get; set; } = string.Empty;
    public PriceSource Source { get; set; }
    public int Confidence { get; set; }
    public DateTime? LastUpdate { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public decimal AvailableStock { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsVirtual { get; set; }
    public string? Warning { get; set; }

    public string StatusIcon => Source switch
    {
        PriceSource.ESTOQUE => "🟢",
        PriceSource.HISTORICO => "🟡",
        PriceSource.BASE => "🔴",
        _ => "⚪"
    };

    public string StatusText => Source switch
    {
        PriceSource.ESTOQUE => "Em Estoque",
        PriceSource.HISTORICO => "Preço Estimado",
        PriceSource.BASE => "Tabela Base",
        _ => "Desconhecido"
    };
}

/// <summary>
/// Input de ingrediente para cálculo
/// </summary>
public class FormulaIngredientInput
{
    public Guid? RawMaterialId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "mg";
}

/// <summary>
/// Item de ingrediente com preço calculado
/// </summary>
public class FormulaIngredientPriceItem
{
    public Guid? RawMaterialId { get; set; }
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
/// Resultado do cálculo de fórmula com ingredientes
/// </summary>
public class FormulaIngredientPriceResult
{
    public List<FormulaIngredientPriceItem> Ingredients { get; set; } = new();
    public decimal TotalIngredientsCost { get; set; }
    public decimal ManipulationCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal SuggestedPrice { get; set; }
    public int AverageConfidence { get; set; }
    public int InStockCount { get; set; }
    public int EstimatedCount { get; set; }
    public int BaseCount { get; set; }
    public decimal InStockCost { get; set; }
    public decimal HistoricalCost { get; set; }
    public decimal BaseCost { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool AllInStock => EstimatedCount == 0 && BaseCount == 0;
}

/// <summary>
/// Estatísticas de precificação
/// </summary>
public class PricingStatistics
{
    public int TotalIngredients { get; set; }
    public int InStockCount { get; set; }
    public int WithHistoricalPrice { get; set; }
    public int VirtualCount { get; set; }
    public int OutdatedPriceCount { get; set; }
    public decimal TotalStockValue { get; set; }
}