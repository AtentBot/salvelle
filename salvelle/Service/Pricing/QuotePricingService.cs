using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Service.Pricing;

/// <summary>
/// Serviço de Precificação para Orçamentos
/// Integra as configurações de precificação do estabelecimento com o cálculo de orçamentos
/// </summary>
public class QuotePricingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<QuotePricingService> _logger;

    public QuotePricingService(AppDbContext context, ILogger<QuotePricingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula o preço final de um orçamento aplicando todas as configurações do estabelecimento
    /// </summary>
    public async Task<QuotePricingResultDto> CalculatePricingAsync(
        QuotePricingRequestDto request,
        Guid establishmentId)
    {
        var result = new QuotePricingResultDto
        {
            Success = false
        };

        try
        {
            // Buscar configurações de precificação do estabelecimento
            var config = await _context.EstablishmentPricingConfigs
                .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId);

            // Usar valores padrão se não houver configuração
            config ??= GetDefaultPricingConfig(establishmentId);

            // Calcular custo dos materiais
            decimal materialsCost = 0;
            var componentResults = new List<QuoteComponentResultDto>();

            foreach (var comp in request.Components)
            {
                var componentCost = await CalculateComponentCostAsync(comp, establishmentId);
                materialsCost += componentCost.TotalCost;
                componentResults.Add(componentCost);
            }

            result.Components = componentResults;
            result.MaterialsCost = materialsCost;

            // Aplicar taxas e markup conforme configuração
            var pricingBreakdown = ApplyPricingConfig(materialsCost, config, request);

            result.TaxValue = pricingBreakdown.TaxValue;
            result.TaxPercentage = config.TaxPercentage;

            result.Fee1Name = config.Fee1Name;
            result.Fee1Value = pricingBreakdown.Fee1Value;
            result.Fee1Percentage = config.Fee1Percentage;

            result.Fee2Name = config.Fee2Name;
            result.Fee2Value = pricingBreakdown.Fee2Value;
            result.Fee2Percentage = config.Fee2Percentage;

            result.Fee3Name = config.Fee3Name;
            result.Fee3Value = pricingBreakdown.Fee3Value;
            result.Fee3Percentage = config.Fee3Percentage;

            result.MarkupPercentage = request.CustomMarkup ?? config.MarkupPercentage;
            result.MarkupValue = pricingBreakdown.MarkupValue;

            result.PackagingPercentage = config.PackagingPercentage;
            result.PackagingCost = pricingBreakdown.PackagingCost;

            result.LaborCost = request.LaborCost ?? pricingBreakdown.DefaultLaborCost;

            // Calcular subtotal
            result.Subtotal = materialsCost
                + result.TaxValue
                + result.Fee1Value
                + result.Fee2Value
                + result.Fee3Value
                + result.MarkupValue
                + result.PackagingCost
                + result.LaborCost;

            // Aplicar desconto
            result.DiscountPercentage = request.DiscountPercentage ?? 0;
            result.DiscountValue = result.Subtotal * (result.DiscountPercentage / 100);

            // Calcular preço final
            result.FinalPrice = result.Subtotal - result.DiscountValue;

            // Verificar preço mínimo da forma farmacêutica
            if (request.PharmaceuticalFormId.HasValue && config.ApplyMinimumPrice)
            {
                var minPrice = await GetMinimumPriceAsync(request.PharmaceuticalFormId.Value, request.SubtypeId);

                if (result.FinalPrice < minPrice)
                {
                    result.MinimumPriceApplied = true;
                    result.OriginalFinalPrice = result.FinalPrice;
                    result.FinalPrice = minPrice;
                    result.Warnings.Add($"Preço mínimo aplicado: R$ {minPrice:F2}");
                }
            }

            // Arredondar se configurado
            if (config.RoundToCents)
            {
                result.FinalPrice = Math.Round(result.FinalPrice, 2);
            }

            // Calcular prazo estimado
            result.EstimatedDays = CalculateEstimatedDays(request.PharmaceuticalFormId, request.Components.Count);
            result.ValidUntil = DateTime.UtcNow.AddDays(config.QuoteValidityDays ?? 7);

            result.Success = true;
            result.Message = "Orçamento calculado com sucesso";

            _logger.LogInformation(
                "Orçamento calculado: MaterialsCost={MaterialsCost}, FinalPrice={FinalPrice}, Components={Count}",
                materialsCost, result.FinalPrice, request.Components.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular precificação do orçamento");
            result.Message = $"Erro no cálculo: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Calcula o custo de um componente individual
    /// </summary>
    private async Task<QuoteComponentResultDto> CalculateComponentCostAsync(
        QuoteComponentRequestDto comp,
        Guid establishmentId)
    {
        var result = new QuoteComponentResultDto
        {
            RawMaterialId = comp.RawMaterialId,
            Name = comp.Name,
            Quantity = comp.Quantity,
            Unit = comp.Unit,
            IsQsp = comp.IsQsp
        };

        // Buscar matéria-prima
        var rawMaterial = await _context.RawMaterials
            .FirstOrDefaultAsync(r => r.Id == comp.RawMaterialId && r.EstablishmentId == establishmentId);

        if (rawMaterial == null)
        {
            // Se não encontrou mas tem preço informado, usar
            if (comp.UnitCost.HasValue)
            {
                result.UnitCost = comp.UnitCost.Value;
                result.TotalCost = comp.Quantity * comp.UnitCost.Value;
                result.PriceSource = "INFORMADO";
            }
            else
            {
                result.UnitCost = 0;
                result.TotalCost = 0;
                result.PriceSource = "NAO_ENCONTRADO";
                result.Warning = "Matéria-prima não encontrada";
            }
            return result;
        }

        result.Name = rawMaterial.Name;
        result.DcbCode = rawMaterial.DcbCode;
        result.IsControlled = rawMaterial.ControlType != "COMUM" && !string.IsNullOrEmpty(rawMaterial.ControlType);
        result.ControlType = rawMaterial.ControlType;

        // Hierarquia de preços: 1. Estoque → 2. Histórico → 3. Base
        var (unitCost, source) = await GetBestPriceAsync(rawMaterial, establishmentId);

        result.UnitCost = unitCost;
        result.PriceSource = source;

        // Aplicar fator de correção se houver
        var correctionFactor = comp.CorrectionFactor ?? rawMaterial.CorrectionFactor;
        var correctedQuantity = comp.Quantity * correctionFactor;

        // Calcular custo total
        result.TotalCost = correctedQuantity * unitCost;

        // Verificar estoque
        result.AvailableStock = rawMaterial.CurrentStock;
        if (correctedQuantity > rawMaterial.CurrentStock)
        {
            result.Warning = $"Estoque insuficiente: {rawMaterial.CurrentStock:F2} {rawMaterial.Unit}";
        }

        return result;
    }

    /// <summary>
    /// Busca o melhor preço disponível para uma matéria-prima
    /// Hierarquia: Estoque → Histórico → Base
    /// </summary>
    private async Task<(decimal Price, string Source)> GetBestPriceAsync(
        RawMaterial rawMaterial,
        Guid establishmentId)
    {
        // 1. Preço do estoque (lote aprovado mais recente)
        var stockPrice = await _context.Batches
            .Where(b => b.RawMaterialId == rawMaterial.Id
                     && b.Status.ToUpper() == "APROVADO"
                     && b.CurrentQuantity > 0)
            .OrderByDescending(b => b.ReceivedDate)
            .Select(b => b.UnitCost)
            .FirstOrDefaultAsync();

        if (stockPrice > 0)
        {
            return (stockPrice, "ESTOQUE");
        }

        // 2. Preço histórico (última compra)
        if (rawMaterial.LastPurchasePrice.HasValue && rawMaterial.LastPurchasePrice.Value > 0)
        {
            // Aplicar correção inflacionária se configurado
            var config = await _context.EstablishmentPricingConfigs
                .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId);

            var inflationRate = config?.InflationRateMonthly ?? 0.5m;
            var safetyMargin = config?.SafetyMarginPercent ?? 5m;

            var monthsSinceLastPurchase = rawMaterial.LastPurchasePriceDate.HasValue
                ? (decimal)(DateTime.UtcNow - rawMaterial.LastPurchasePriceDate.Value).TotalDays / 30
                : 0;

            var inflationAdjustment = 1 + (inflationRate * monthsSinceLastPurchase / 100);
            var safetyAdjustment = 1 + (safetyMargin / 100);

            var adjustedPrice = rawMaterial.LastPurchasePrice.Value * inflationAdjustment * safetyAdjustment;
            return (adjustedPrice, "HISTORICO");
        }

        // 3. Preço base cadastrado
        if (rawMaterial.BasePrice.HasValue && rawMaterial.BasePrice.Value > 0)
        {
            return (rawMaterial.BasePrice.Value, "BASE");
        }

        // Nenhum preço disponível
        return (0, "SEM_PRECO");
    }

    /// <summary>
    /// Aplica as configurações de precificação sobre o custo dos materiais
    /// </summary>
    private PricingBreakdown ApplyPricingConfig(
        decimal materialsCost,
        EstablishmentPricingConfig config,
        QuotePricingRequestDto request)
    {
        var breakdown = new PricingBreakdown();

        // Taxas percentuais sobre o custo dos materiais
        breakdown.TaxValue = materialsCost * (config.TaxPercentage / 100);
        breakdown.Fee1Value = materialsCost * (config.Fee1Percentage / 100);
        breakdown.Fee2Value = materialsCost * (config.Fee2Percentage / 100);
        breakdown.Fee3Value = materialsCost * (config.Fee3Percentage / 100);

        // Markup
        var markupPct = request.CustomMarkup ?? config.MarkupPercentage;
        breakdown.MarkupValue = materialsCost * (markupPct / 100);

        // Embalagem
        breakdown.PackagingCost = materialsCost * (config.PackagingPercentage / 100);

        // Mão de obra padrão (taxa de manipulação)
        breakdown.DefaultLaborCost = config.ManipulationFee ?? 10m;

        return breakdown;
    }

    /// <summary>
    /// Busca o preço mínimo da forma farmacêutica ou subtipo
    /// </summary>
    private async Task<decimal> GetMinimumPriceAsync(Guid formId, Guid? subtypeId)
    {
        // Primeiro verificar subtipo
        if (subtypeId.HasValue)
        {
            var subtypePrice = await _context.PharmaceuticalFormSubtypes
                .Where(s => s.Id == subtypeId.Value)
                .Select(s => s.MinimumPrice)
                .FirstOrDefaultAsync();

            if (subtypePrice.HasValue && subtypePrice.Value > 0)
                return subtypePrice.Value;
        }

        // Depois verificar forma farmacêutica
        var formPrice = await _context.PharmaceuticalForms
            .Where(f => f.Id == formId)
            .Select(f => f.MinimumPrice)
            .FirstOrDefaultAsync();

        return formPrice;
    }

    /// <summary>
    /// Calcula prazo estimado de entrega baseado na complexidade
    /// </summary>
    private int CalculateEstimatedDays(Guid? formId, int componentCount)
    {
        // Base: 2 dias
        int days = 2;

        // Adicionar por complexidade
        if (componentCount > 5) days++;
        if (componentCount > 10) days++;

        // TODO: Verificar tipo de forma para ajustar prazo
        // Formas controladas podem ter prazo maior

        return days;
    }

    /// <summary>
    /// Retorna configuração padrão se não houver configuração cadastrada
    /// </summary>
    private EstablishmentPricingConfig GetDefaultPricingConfig(Guid establishmentId)
    {
        return new EstablishmentPricingConfig
        {
            EstablishmentId = establishmentId,
            TaxPercentage = 0,
            Fee1Name = "Taxa Operacional",
            Fee1Percentage = 0,
            Fee2Name = "Mão de Obra",
            Fee2Percentage = 0,
            Fee3Name = "Outros",
            Fee3Percentage = 0,
            MarkupPercentage = 100,
            PackagingPercentage = 5,
            ApplyMinimumPrice = true,
            RoundToCents = true,
            ManipulationFee = 10,
            QuoteValidityDays = 7,
            InflationRateMonthly = 0.5m,
            SafetyMarginPercent = 5
        };
    }

    /// <summary>
    /// Cria um orçamento a partir do resultado do cálculo
    /// </summary>
    public async Task<Guid> CreateQuoteAsync(
        QuotePricingResultDto pricing,
        QuoteMetadataDto metadata,
        Guid establishmentId,
        Guid? createdByEmployeeId)
    {
        var code = await GenerateQuoteCodeAsync(establishmentId);

        var quote = new PrescriptionQuote
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = code,
            PublicToken = Guid.NewGuid().ToString("N"),

            // Prescrição (se houver)
            PrescriptionId = metadata.PrescriptionId,

            // Médico
            DoctorName = metadata.DoctorName,
            DoctorCrm = metadata.DoctorCrm,

            // Cliente
            CustomerId = metadata.CustomerId,
            CustomerName = metadata.CustomerName,
            CustomerPhone = metadata.CustomerPhone,
            CustomerEmail = metadata.CustomerEmail,

            // Fórmula
            PharmaceuticalForm = metadata.PharmaceuticalForm,
            TotalQuantity = metadata.TotalQuantity,
            TotalQuantityUnit = metadata.TotalQuantityUnit,
            UsageType = metadata.UsageType,
            Instructions = metadata.Instructions,

            // Componentes (JSON)
            ComponentsJson = System.Text.Json.JsonSerializer.Serialize(
                pricing.Components.Select(c => new
                {
                    c.RawMaterialId,
                    c.Name,
                    c.DcbCode,
                    c.Quantity,
                    c.Unit,
                    c.UnitCost,
                    c.TotalCost,
                    c.IsControlled,
                    c.ControlType,
                    c.IsQsp
                })),

            // Valores
            MaterialsCost = pricing.MaterialsCost,
            MarkupPercentage = pricing.MarkupPercentage,
            MarkupValue = pricing.MarkupValue,
            LaborCost = pricing.LaborCost,
            PackagingCost = pricing.PackagingCost,
            Subtotal = pricing.Subtotal,
            DiscountPercentage = pricing.DiscountPercentage,
            DiscountValue = pricing.DiscountValue,
            FinalPrice = pricing.FinalPrice,

            // Prazo
            EstimatedDays = pricing.EstimatedDays,
            ValidUntil = pricing.ValidUntil,

            // Status
            Status = "PENDENTE",

            // Auditoria
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = createdByEmployeeId ?? Guid.Empty
        };

        _context.PrescriptionQuotes.Add(quote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Orçamento criado: {Code} - R$ {FinalPrice}", code, pricing.FinalPrice);

        return quote.Id;
    }

    /// <summary>
    /// Gera código único para orçamento
    /// </summary>
    private async Task<string> GenerateQuoteCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var count = await _context.PrescriptionQuotes
            .Where(q => q.EstablishmentId == establishmentId && q.CreatedAt.Date == today.Date)
            .CountAsync();

        return $"ORC{today:yyyyMMdd}-{(count + 1):D4}";
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public class QuotePricingRequestDto
{
    public Guid? PharmaceuticalFormId { get; set; }
    public Guid? SubtypeId { get; set; }
    public List<QuoteComponentRequestDto> Components { get; set; } = new();
    public decimal? CustomMarkup { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? DiscountPercentage { get; set; }
}

public class QuoteComponentRequestDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "g";
    public decimal? UnitCost { get; set; }
    public decimal? CorrectionFactor { get; set; }
    public bool IsQsp { get; set; }
}

public class QuotePricingResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    // Componentes
    public List<QuoteComponentResultDto> Components { get; set; } = new();

    // Valores calculados
    public decimal MaterialsCost { get; set; }

    public decimal TaxPercentage { get; set; }
    public decimal TaxValue { get; set; }

    public string Fee1Name { get; set; } = string.Empty;
    public decimal Fee1Percentage { get; set; }
    public decimal Fee1Value { get; set; }

    public string Fee2Name { get; set; } = string.Empty;
    public decimal Fee2Percentage { get; set; }
    public decimal Fee2Value { get; set; }

    public string Fee3Name { get; set; } = string.Empty;
    public decimal Fee3Percentage { get; set; }
    public decimal Fee3Value { get; set; }

    public decimal MarkupPercentage { get; set; }
    public decimal MarkupValue { get; set; }

    public decimal PackagingPercentage { get; set; }
    public decimal PackagingCost { get; set; }

    public decimal LaborCost { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountPercentage { get; set; }
    public decimal DiscountValue { get; set; }

    public decimal FinalPrice { get; set; }
    public decimal? OriginalFinalPrice { get; set; }
    public bool MinimumPriceApplied { get; set; }

    // Prazo
    public int EstimatedDays { get; set; }
    public DateTime ValidUntil { get; set; }

    // Alertas
    public List<string> Warnings { get; set; } = new();
}

public class QuoteComponentResultDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "g";
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string PriceSource { get; set; } = string.Empty;
    public bool IsControlled { get; set; }
    public string? ControlType { get; set; }
    public bool IsQsp { get; set; }
    public decimal AvailableStock { get; set; }
    public string? Warning { get; set; }
}

public class QuoteMetadataDto
{
    public Guid? PrescriptionId { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorCrm { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? PharmaceuticalForm { get; set; }
    public string? TotalQuantity { get; set; }
    public string? TotalQuantityUnit { get; set; }
    public string? UsageType { get; set; }
    public string? Instructions { get; set; }
}

internal class PricingBreakdown
{
    public decimal TaxValue { get; set; }
    public decimal Fee1Value { get; set; }
    public decimal Fee2Value { get; set; }
    public decimal Fee3Value { get; set; }
    public decimal MarkupValue { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal DefaultLaborCost { get; set; }
}