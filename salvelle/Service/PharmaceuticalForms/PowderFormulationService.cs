using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Service.PharmaceuticalForms;

/// <summary>
/// Serviço de cálculo para Sachês, Pós e Papéis
/// </summary>
public class PowderFormulationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PowderFormulationService> _logger;

    public PowderFormulationService(AppDbContext context, ILogger<PowderFormulationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula uma formulação de sachê/pó
    /// </summary>
    public async Task<PowderCalculationResultDto> CalculateAsync(
        PowderCalculationRequestDto request,
        Guid establishmentId)
    {
        var result = new PowderCalculationResultDto
        {
            Success = false,
            UnitCount = request.UnitCount,
            WeightPerUnit = request.WeightPerUnit,
            WeightUnit = request.WeightUnit
        };

        try
        {
            // Validações
            if (request.UnitCount <= 0)
            {
                result.Message = "Número de unidades deve ser maior que zero";
                return result;
            }

            if (request.WeightPerUnit <= 0 && !request.CalculateWeightFromComponents)
            {
                result.Message = "Peso por unidade deve ser maior que zero";
                return result;
            }

            // Converter para mg se necessário
            decimal weightPerUnitMg = request.WeightUnit.ToLower() switch
            {
                "g" => request.WeightPerUnit * 1000m,
                "mg" => request.WeightPerUnit,
                "mcg" => request.WeightPerUnit / 1000m,
                _ => request.WeightPerUnit * 1000m // Assume gramas
            };

            var componentResults = new List<PowderComponentResultDto>();
            decimal totalActivesWeightMg = 0;

            // Processar cada componente ativo
            foreach (var comp in request.Components)
            {
                var rawMaterial = await _context.RawMaterials
                    .FirstOrDefaultAsync(r => r.Id == comp.RawMaterialId && r.EstablishmentId == establishmentId);

                // Dose por unidade em mg
                decimal dosePerUnitMg = comp.DoseUnit.ToLower() switch
                {
                    "g" => comp.DosePerUnit * 1000m,
                    "mg" => comp.DosePerUnit,
                    "mcg" => comp.DosePerUnit / 1000m,
                    _ => comp.DosePerUnit
                };

                // Aplicar fatores
                var correctionFactor = comp.CorrectionFactor ?? (decimal?)(rawMaterial?.CorrectionFactor) ?? 1.0m;
                var purityFactor = comp.PurityFactor ?? rawMaterial?.PurityFactor ?? 1.0m;
                var dilutionFactor = comp.DilutionFactor ?? (decimal?)(rawMaterial?.DilutionFactor) ?? 1.0m;

                // Quantidade corrigida por unidade
                var correctedDosePerUnit = dosePerUnitMg * correctionFactor * dilutionFactor / purityFactor;

                // Quantidade total para todas as unidades
                var totalQuantityMg = correctedDosePerUnit * request.UnitCount;

                totalActivesWeightMg += correctedDosePerUnit;

                componentResults.Add(new PowderComponentResultDto
                {
                    RawMaterialId = comp.RawMaterialId,
                    RawMaterialName = comp.RawMaterialName ?? rawMaterial?.Name ?? "N/A",
                    DcbCode = rawMaterial?.DcbCode,
                    DosePerUnit = comp.DosePerUnit,
                    DoseUnit = comp.DoseUnit,
                    CorrectedDosePerUnitMg = correctedDosePerUnit,
                    TotalQuantityMg = totalQuantityMg,
                    TotalQuantityDisplay = FormatWeight(totalQuantityMg),
                    IsControlled = rawMaterial?.ControlType != "COMUM" && !string.IsNullOrEmpty(rawMaterial?.ControlType),
                    ControlType = rawMaterial?.ControlType,
                    UnitPrice = rawMaterial?.BasePrice,
                    TotalCost = rawMaterial?.BasePrice.HasValue == true ? (totalQuantityMg / 1000m) * rawMaterial.BasePrice.Value : null
                });
            }

            result.Components = componentResults;
            result.TotalActivesPerUnitMg = totalActivesWeightMg;

            // Calcular excipiente QSP (se aplicável)
            decimal excipientPerUnitMg = 0;

            if (request.CalculateWeightFromComponents)
            {
                // Peso definido pelos componentes (sem excipiente de enchimento)
                result.WeightPerUnit = totalActivesWeightMg;
                result.WeightPerUnitMg = totalActivesWeightMg;
            }
            else
            {
                result.WeightPerUnitMg = weightPerUnitMg;
                excipientPerUnitMg = weightPerUnitMg - totalActivesWeightMg;

                if (excipientPerUnitMg < 0)
                {
                    result.Warnings.Add($"⚠️ Peso dos ativos ({FormatWeight(totalActivesWeightMg)}) excede o peso por unidade ({FormatWeight(weightPerUnitMg)})");
                    excipientPerUnitMg = 0;
                }
            }

            // Adicionar excipiente
            if (excipientPerUnitMg > 0)
            {
                var excipientTotal = excipientPerUnitMg * request.UnitCount;

                // Buscar excipiente selecionado ou usar genérico
                RawMaterial? excipient = null;
                if (request.ExcipientId.HasValue)
                {
                    excipient = await _context.RawMaterials
                        .FirstOrDefaultAsync(r => r.Id == request.ExcipientId.Value);
                }

                result.Excipient = new PowderComponentResultDto
                {
                    RawMaterialId = request.ExcipientId ?? Guid.Empty,
                    RawMaterialName = excipient?.Name ?? request.ExcipientName ?? "Excipiente QSP",
                    DcbCode = excipient?.DcbCode,
                    DosePerUnit = excipientPerUnitMg,
                    DoseUnit = "mg",
                    CorrectedDosePerUnitMg = excipientPerUnitMg,
                    TotalQuantityMg = excipientTotal,
                    TotalQuantityDisplay = FormatWeight(excipientTotal),
                    IsQsp = true,
                    UnitPrice = excipient?.BasePrice,
                    TotalCost = excipient?.BasePrice.HasValue == true ? (excipientTotal / 1000m) * excipient.BasePrice.Value : null
                };

                result.ExcipientPerUnitMg = excipientPerUnitMg;
            }

            // Calcular totais
            result.TotalWeightMg = result.WeightPerUnitMg * request.UnitCount;
            result.TotalWeightDisplay = FormatWeight(result.TotalWeightMg);

            // Custos
            result.TotalActiveCost = componentResults.Where(c => c.TotalCost.HasValue).Sum(c => c.TotalCost!.Value);
            result.TotalExcipientCost = result.Excipient?.TotalCost ?? 0;
            result.TotalMaterialsCost = result.TotalActiveCost + result.TotalExcipientCost;

            // Validações adicionais
            if (totalActivesWeightMg > weightPerUnitMg * 0.9m && !request.CalculateWeightFromComponents)
            {
                result.Warnings.Add("⚠️ Pouco espaço para excipiente - verificar fluidez do pó");
            }

            result.Success = true;
            result.Message = $"Formulação calculada: {request.UnitCount} sachês de {FormatWeight(result.WeightPerUnitMg)} cada";

            _logger.LogInformation(
                "Cálculo de sachê/pó: {Count} unidades de {Weight} - {Components} ativos",
                request.UnitCount, FormatWeight(result.WeightPerUnitMg), request.Components.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular formulação de sachê/pó");
            result.Message = $"Erro no cálculo: {ex.Message}";
            return result;
        }
    }

    private string FormatWeight(decimal mg)
    {
        if (mg >= 1000)
            return $"{mg / 1000:F2} g";
        if (mg >= 1)
            return $"{mg:F2} mg";
        return $"{mg * 1000:F2} mcg";
    }

    /// <summary>
    /// Lista excipientes comuns para sachês
    /// </summary>
    public async Task<List<ExcipientOptionDto>> GetCommonExcipientsAsync(Guid establishmentId)
    {
        var commonExcipients = new[] { "LACTOSE", "AMIDO", "CELULOSE", "MALTODEXTRINA", "SACAROSE", "MANITOL", "TALCO" };

        return await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId
                     && r.IsActive
                     && (r.AllowedUsage == "ORAL" || r.AllowedUsage == "BOTH")
                     && (commonExcipients.Any(e => r.Name.ToUpper().Contains(e)) || r.Category == "Excipiente"))
            .OrderBy(r => r.Name)
            .Select(r => new ExcipientOptionDto
            {
                Id = r.Id,
                Name = r.Name,
                BulkDensity = (double?)r.BulkDensity,
                BasePrice = r.BasePrice
            })
            .Take(20)
            .ToListAsync();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public class PowderCalculationRequestDto
{
    public int UnitCount { get; set; } // Número de sachês/papéis
    public decimal WeightPerUnit { get; set; } // Peso por unidade
    public string WeightUnit { get; set; } = "g"; // g, mg
    public bool CalculateWeightFromComponents { get; set; } // Se true, não adiciona excipiente QSP
    public Guid? ExcipientId { get; set; }
    public string? ExcipientName { get; set; }
    public List<PowderComponentRequestDto> Components { get; set; } = new();
}

public class PowderComponentRequestDto
{
    public Guid RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public decimal DosePerUnit { get; set; } // Dose por sachê
    public string DoseUnit { get; set; } = "mg";
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
    public decimal? DilutionFactor { get; set; }
}

public class PowderCalculationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public int UnitCount { get; set; }
    public decimal WeightPerUnit { get; set; }
    public string WeightUnit { get; set; } = "g";
    public decimal WeightPerUnitMg { get; set; }

    public decimal TotalActivesPerUnitMg { get; set; }
    public decimal ExcipientPerUnitMg { get; set; }
    public decimal TotalWeightMg { get; set; }
    public string TotalWeightDisplay { get; set; } = string.Empty;

    public List<PowderComponentResultDto> Components { get; set; } = new();
    public PowderComponentResultDto? Excipient { get; set; }

    public decimal TotalActiveCost { get; set; }
    public decimal TotalExcipientCost { get; set; }
    public decimal TotalMaterialsCost { get; set; }

    public List<string> Warnings { get; set; } = new();
}

public class PowderComponentResultDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal DosePerUnit { get; set; }
    public string DoseUnit { get; set; } = "mg";
    public decimal CorrectedDosePerUnitMg { get; set; }
    public decimal TotalQuantityMg { get; set; }
    public string TotalQuantityDisplay { get; set; } = string.Empty;
    public bool IsControlled { get; set; }
    public string? ControlType { get; set; }
    public bool IsQsp { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalCost { get; set; }
}

public class ExcipientOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? BulkDensity { get; set; }
    public decimal? BasePrice { get; set; }
}
