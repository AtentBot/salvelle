using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using DTOs.PharmaceuticalForms;

namespace Service.PharmaceuticalForms;

/// <summary>
/// Serviço de cálculo para formulações tópicas (Cremes, Géis, Pomadas, Loções)
/// Calcula quantidades absolutas a partir de percentuais e volume total
/// </summary>
public class TopicalFormulationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TopicalFormulationService> _logger;

    public TopicalFormulationService(AppDbContext context, ILogger<TopicalFormulationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula uma formulação tópica completa
    /// </summary>
    public async Task<TopicalCalculationResultDto> CalculateAsync(
        TopicalCalculationRequestDto request,
        Guid establishmentId)
    {
        var result = new TopicalCalculationResultDto
        {
            TotalQuantity = request.TotalQuantity,
            TotalUnit = request.TotalUnit,
            Success = false
        };

        try
        {
            // Validações
            if (request.TotalQuantity <= 0)
            {
                result.Message = "Quantidade total deve ser maior que zero";
                return result;
            }

            if (request.Actives == null || !request.Actives.Any())
            {
                result.Message = "Nenhum ativo informado";
                return result;
            }

            // Buscar base selecionada (se houver)
            PharmaceuticalFormSubtype? baseSubtype = null;
            List<PharmaceuticalFormComposition>? baseCompositions = null;

            if (request.BaseSubtypeId.HasValue)
            {
                baseSubtype = await _context.PharmaceuticalFormSubtypes
                    .Include(s => s.Compositions!)
                        .ThenInclude(c => c.RawMaterial)
                    .FirstOrDefaultAsync(s => s.Id == request.BaseSubtypeId.Value && s.EstablishmentId == establishmentId);

                if (baseSubtype != null)
                {
                    baseCompositions = baseSubtype.Compositions?.ToList();
                    result.BaseName = baseSubtype.Name;
                    result.BaseCode = baseSubtype.Code;
                }
            }

            // Buscar dados das matérias-primas dos ativos
            var activeIds = request.Actives.Select(a => a.RawMaterialId).ToList();
            var rawMaterials = await _context.RawMaterials
                .Where(r => activeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            // Calcular quantidade total de ativos
            decimal totalActivesPercentage = 0;
            var activeResults = new List<TopicalIngredientResultDto>();

            foreach (var active in request.Actives)
            {
                rawMaterials.TryGetValue(active.RawMaterialId, out var rawMaterial);

                var percentage = active.Percentage;
                var quantityGrams = CalculateQuantityFromPercentage(percentage, request.TotalQuantity, request.TotalUnit);

                // Aplicar fatores de correção se necessário
                var correctionFactor = active.CorrectionFactor ?? (decimal?)(rawMaterial?.CorrectionFactor) ?? 1.0m;
                var purityFactor = active.PurityFactor ?? rawMaterial?.PurityFactor ?? 1.0m;

                var correctedQuantity = quantityGrams * correctionFactor / purityFactor;

                totalActivesPercentage += percentage;

                activeResults.Add(new TopicalIngredientResultDto
                {
                    RawMaterialId = active.RawMaterialId,
                    RawMaterialName = active.RawMaterialName ?? rawMaterial?.Name ?? "N/A",
                    DcbCode = rawMaterial?.DcbCode,
                    Percentage = percentage,
                    OriginalQuantity = quantityGrams,
                    CorrectedQuantity = correctedQuantity,
                    Unit = "g",
                    IsActive = true,
                    IsQsp = false,
                    UnitPrice = rawMaterial?.BasePrice,
                    TotalCost = rawMaterial?.BasePrice.HasValue == true ? correctedQuantity * rawMaterial.BasePrice.Value : null
                });
            }

            result.ActiveResults = activeResults;
            result.TotalActivesPercentage = totalActivesPercentage;

            // Calcular componentes da base
            var baseResults = new List<TopicalIngredientResultDto>();
            decimal totalBasePercentage = 0;

            if (baseCompositions != null && baseCompositions.Any())
            {
                foreach (var comp in baseCompositions.OrderBy(c => c.SortOrder))
                {
                    if (comp.IsQsp)
                    {
                        // QSP = 100% - (ativos + outros componentes da base)
                        var qspPercentage = 100m - totalActivesPercentage - totalBasePercentage;
                        if (qspPercentage < 0) qspPercentage = 0;

                        var qspQuantity = CalculateQuantityFromPercentage(qspPercentage, request.TotalQuantity, request.TotalUnit);

                        baseResults.Add(new TopicalIngredientResultDto
                        {
                            RawMaterialId = comp.RawMaterialId,
                            RawMaterialName = comp.RawMaterial?.Name ?? "QSP",
                            DcbCode = comp.RawMaterial?.DcbCode,
                            Percentage = qspPercentage,
                            OriginalQuantity = qspQuantity,
                            CorrectedQuantity = qspQuantity,
                            Unit = comp.Unit,
                            IsActive = false,
                            IsQsp = true,
                            IsOptional = comp.IsOptional,
                            UnitPrice = comp.RawMaterial?.BasePrice,
                            TotalCost = comp.RawMaterial?.BasePrice.HasValue == true ? qspQuantity * comp.RawMaterial.BasePrice.Value : null
                        });
                    }
                    else
                    {
                        var percentage = comp.Percentage ?? 0;
                        var quantityGrams = comp.QuantityPerYield.HasValue
                            ? comp.QuantityPerYield.Value * (request.TotalQuantity / (baseSubtype?.YieldQuantity ?? 100m))
                            : CalculateQuantityFromPercentage(percentage, request.TotalQuantity, request.TotalUnit);

                        totalBasePercentage += percentage;

                        baseResults.Add(new TopicalIngredientResultDto
                        {
                            RawMaterialId = comp.RawMaterialId,
                            RawMaterialName = comp.RawMaterial?.Name ?? "N/A",
                            DcbCode = comp.RawMaterial?.DcbCode,
                            Percentage = percentage,
                            OriginalQuantity = quantityGrams,
                            CorrectedQuantity = quantityGrams,
                            Unit = comp.Unit,
                            IsActive = false,
                            IsQsp = false,
                            IsOptional = comp.IsOptional,
                            UnitPrice = comp.RawMaterial?.BasePrice,
                            TotalCost = comp.RawMaterial?.BasePrice.HasValue == true ? quantityGrams * comp.RawMaterial.BasePrice.Value : null
                        });
                    }
                }
            }
            else
            {
                // Sem base definida - calcular QSP genérico
                var qspPercentage = 100m - totalActivesPercentage;
                var qspQuantity = CalculateQuantityFromPercentage(qspPercentage, request.TotalQuantity, request.TotalUnit);

                baseResults.Add(new TopicalIngredientResultDto
                {
                    RawMaterialId = Guid.Empty,
                    RawMaterialName = "Base/Veículo (QSP)",
                    Percentage = qspPercentage,
                    OriginalQuantity = qspQuantity,
                    CorrectedQuantity = qspQuantity,
                    Unit = "g",
                    IsActive = false,
                    IsQsp = true
                });
            }

            result.BaseResults = baseResults;
            result.TotalBasePercentage = totalBasePercentage + (100m - totalActivesPercentage - totalBasePercentage);

            // Validações e alertas
            if (totalActivesPercentage > 100)
            {
                result.Warnings.Add($"⚠️ Total de ativos ({totalActivesPercentage:F1}%) excede 100%");
            }

            if (totalActivesPercentage > 30)
            {
                result.Warnings.Add($"⚠️ Concentração de ativos alta ({totalActivesPercentage:F1}%) - verificar estabilidade");
            }

            // Calcular custos totais
            result.TotalActivesCost = activeResults.Where(a => a.TotalCost.HasValue).Sum(a => a.TotalCost!.Value);
            result.TotalBaseCost = baseResults.Where(b => b.TotalCost.HasValue).Sum(b => b.TotalCost!.Value);
            result.TotalMaterialsCost = result.TotalActivesCost + result.TotalBaseCost;

            // Calcular peso total
            result.TotalWeightGrams = activeResults.Sum(a => a.CorrectedQuantity) + baseResults.Sum(b => b.CorrectedQuantity);

            result.Success = true;
            result.Message = GenerateResultMessage(result);

            _logger.LogInformation(
                "Cálculo de formulação tópica: {Quantity}{Unit} com {ActiveCount} ativos - Base: {Base}",
                request.TotalQuantity, request.TotalUnit, request.Actives.Count, result.BaseName ?? "Genérica");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular formulação tópica");
            result.Message = $"Erro no cálculo: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Calcula quantidade em gramas a partir de percentual
    /// </summary>
    private decimal CalculateQuantityFromPercentage(decimal percentage, decimal totalQuantity, string unit)
    {
        // Converter para gramas se necessário
        var totalGrams = unit.ToLower() switch
        {
            "kg" => totalQuantity * 1000m,
            "ml" => totalQuantity, // Assumindo densidade ~1 para cremes
            "l" => totalQuantity * 1000m,
            _ => totalQuantity // g é padrão
        };

        return totalGrams * (percentage / 100m);
    }

    /// <summary>
    /// Gera mensagem descritiva do resultado
    /// </summary>
    private string GenerateResultMessage(TopicalCalculationResultDto result)
    {
        var msg = $"Formulação calculada: {result.TotalQuantity}{result.TotalUnit}";
        
        if (!string.IsNullOrEmpty(result.BaseName))
            msg += $" - Base: {result.BaseName}";
        
        msg += $" - {result.ActiveResults?.Count ?? 0} ativo(s)";
        
        if (result.TotalMaterialsCost > 0)
            msg += $" - Custo materiais: R$ {result.TotalMaterialsCost:F2}";

        return msg;
    }

    /// <summary>
    /// Lista bases disponíveis para uma forma farmacêutica
    /// </summary>
    public async Task<List<BaseSubtypeDto>> GetAvailableBasesAsync(
        Guid pharmaceuticalFormId,
        Guid establishmentId)
    {
        return await _context.PharmaceuticalFormSubtypes
            .Where(s => s.PharmaceuticalFormId == pharmaceuticalFormId 
                     && s.EstablishmentId == establishmentId 
                     && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new BaseSubtypeDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IsDefault = s.IsDefault,
                YieldQuantity = s.YieldQuantity,
                YieldUnit = s.YieldUnit,
                MinimumPrice = s.MinimumPrice,
                CompositionsCount = s.Compositions != null ? s.Compositions.Count : 0
            })
            .ToListAsync();
    }

    /// <summary>
    /// Busca composição completa de uma base
    /// </summary>
    public async Task<BaseCompositionDetailDto?> GetBaseCompositionAsync(
        Guid subtypeId,
        Guid establishmentId)
    {
        var subtype = await _context.PharmaceuticalFormSubtypes
            .Include(s => s.PharmaceuticalForm)
            .Include(s => s.Compositions!)
                .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(s => s.Id == subtypeId && s.EstablishmentId == establishmentId);

        if (subtype == null) return null;

        return new BaseCompositionDetailDto
        {
            SubtypeId = subtype.Id,
            SubtypeCode = subtype.Code,
            SubtypeName = subtype.Name,
            SubtypeDescription = subtype.Description,
            PharmaceuticalFormName = subtype.PharmaceuticalForm?.Name ?? "",
            YieldQuantity = subtype.YieldQuantity,
            YieldUnit = subtype.YieldUnit,
            PreparationInstructions = subtype.PreparationInstructions,
            Compositions = subtype.Compositions?.OrderBy(c => c.SortOrder).Select(c => new BaseCompositionItemDto
            {
                Id = c.Id,
                RawMaterialId = c.RawMaterialId,
                RawMaterialName = c.RawMaterial?.Name ?? "",
                DcbCode = c.RawMaterial?.DcbCode,
                Percentage = c.Percentage,
                QuantityPerYield = c.QuantityPerYield,
                Unit = c.Unit,
                IsQsp = c.IsQsp,
                IsOptional = c.IsOptional,
                SortOrder = c.SortOrder
            }).ToList() ?? new()
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs ESPECÍFICOS PARA FORMULAÇÕES TÓPICAS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO para requisição de cálculo de formulação tópica
/// </summary>
public class TopicalCalculationRequestDto
{
    public Guid? BaseSubtypeId { get; set; }
    public decimal TotalQuantity { get; set; }
    public string TotalUnit { get; set; } = "g";
    public List<TopicalActiveDto> Actives { get; set; } = new();
}

/// <summary>
/// DTO de ativo para formulação tópica
/// </summary>
public class TopicalActiveDto
{
    public Guid RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public decimal Percentage { get; set; }
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
}

/// <summary>
/// DTO de resultado do cálculo de formulação tópica
/// </summary>
public class TopicalCalculationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    
    // Base utilizada
    public string? BaseName { get; set; }
    public string? BaseCode { get; set; }
    
    // Quantidades
    public decimal TotalQuantity { get; set; }
    public string TotalUnit { get; set; } = "g";
    public decimal TotalWeightGrams { get; set; }
    
    // Percentuais
    public decimal TotalActivesPercentage { get; set; }
    public decimal TotalBasePercentage { get; set; }
    
    // Custos
    public decimal TotalActivesCost { get; set; }
    public decimal TotalBaseCost { get; set; }
    public decimal TotalMaterialsCost { get; set; }
    
    // Resultados detalhados
    public List<TopicalIngredientResultDto> ActiveResults { get; set; } = new();
    public List<TopicalIngredientResultDto> BaseResults { get; set; } = new();
    
    // Alertas
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// DTO de resultado por ingrediente
/// </summary>
public class TopicalIngredientResultDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal Percentage { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal CorrectedQuantity { get; set; }
    public string Unit { get; set; } = "g";
    public bool IsActive { get; set; }
    public bool IsQsp { get; set; }
    public bool IsOptional { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalCost { get; set; }
}

/// <summary>
/// DTO de base/subtipo disponível
/// </summary>
public class BaseSubtypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = "g";
    public decimal? MinimumPrice { get; set; }
    public int CompositionsCount { get; set; }
}

/// <summary>
/// DTO de composição detalhada da base
/// </summary>
public class BaseCompositionDetailDto
{
    public Guid SubtypeId { get; set; }
    public string SubtypeCode { get; set; } = string.Empty;
    public string SubtypeName { get; set; } = string.Empty;
    public string? SubtypeDescription { get; set; }
    public string PharmaceuticalFormName { get; set; } = string.Empty;
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = "g";
    public string? PreparationInstructions { get; set; }
    public List<BaseCompositionItemDto> Compositions { get; set; } = new();
}

/// <summary>
/// DTO de item de composição da base
/// </summary>
public class BaseCompositionItemDto
{
    public Guid Id { get; set; }
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? QuantityPerYield { get; set; }
    public string Unit { get; set; } = "g";
    public bool IsQsp { get; set; }
    public bool IsOptional { get; set; }
    public int SortOrder { get; set; }
}
