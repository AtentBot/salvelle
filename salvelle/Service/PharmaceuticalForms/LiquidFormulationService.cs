using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Service.PharmaceuticalForms;

/// <summary>
/// Serviço de cálculo para formas farmacêuticas líquidas
/// (Soluções, Xaropes, Suspensões, Elixires, Gotas)
/// </summary>
public class LiquidFormulationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LiquidFormulationService> _logger;

    public LiquidFormulationService(AppDbContext context, ILogger<LiquidFormulationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula uma formulação líquida completa
    /// </summary>
    public async Task<LiquidCalculationResultDto> CalculateAsync(
        LiquidCalculationRequestDto request,
        Guid establishmentId)
    {
        var result = new LiquidCalculationResultDto
        {
            Success = false,
            TotalVolume = request.TotalVolume,
            VolumeUnit = request.VolumeUnit
        };

        try
        {
            // Validações
            if (request.TotalVolume <= 0)
            {
                result.Message = "Volume total deve ser maior que zero";
                return result;
            }

            // Converter para mL se necessário
            decimal totalVolumeMl = request.VolumeUnit.ToLower() switch
            {
                "l" => request.TotalVolume * 1000m,
                "ml" => request.TotalVolume,
                _ => request.TotalVolume
            };

            // Buscar veículo/base selecionado
            PharmaceuticalFormSubtype? vehicleSubtype = null;
            if (request.VehicleSubtypeId.HasValue)
            {
                vehicleSubtype = await _context.PharmaceuticalFormSubtypes
                    .Include(s => s.Compositions!)
                        .ThenInclude(c => c.RawMaterial)
                    .FirstOrDefaultAsync(s => s.Id == request.VehicleSubtypeId.Value && s.EstablishmentId == establishmentId);

                if (vehicleSubtype != null)
                {
                    result.VehicleName = vehicleSubtype.Name;
                    result.VehicleCode = vehicleSubtype.Code;
                }
            }

            var componentResults = new List<LiquidComponentResultDto>();
            decimal totalSolidsGrams = 0;

            // Processar cada componente
            foreach (var comp in request.Components)
            {
                var rawMaterial = await _context.RawMaterials
                    .FirstOrDefaultAsync(r => r.Id == comp.RawMaterialId && r.EstablishmentId == establishmentId);

                decimal quantity = 0;
                string displayConcentration = "";

                // Calcular quantidade baseado no tipo de concentração
                switch (comp.ConcentrationType.ToUpper())
                {
                    case "MG/ML":
                        // mg/mL → quantidade em mg = concentração × volume
                        quantity = comp.Concentration * totalVolumeMl;
                        displayConcentration = $"{comp.Concentration} mg/mL";
                        break;

                    case "G/L":
                        // g/L → converter para mg/mL primeiro
                        var mgPerMl = comp.Concentration; // g/L = mg/mL
                        quantity = mgPerMl * totalVolumeMl;
                        displayConcentration = $"{comp.Concentration} g/L";
                        break;

                    case "%":
                    case "PERCENT":
                        // % p/v → g por 100mL
                        quantity = (comp.Concentration / 100m) * totalVolumeMl * 1000; // em mg
                        displayConcentration = $"{comp.Concentration}%";
                        break;

                    case "MG":
                    case "ABSOLUTE":
                        // Quantidade absoluta em mg
                        quantity = comp.Concentration;
                        displayConcentration = $"{comp.Concentration} mg (total)";
                        break;

                    case "GOTAS/ML":
                        // Gotas por mL - precisa converter baseado no gotejador
                        var dropsPerMl = comp.DropsPerMl ?? 20m; // Padrão: 20 gotas/mL
                        var totalDrops = comp.Concentration * totalVolumeMl;
                        quantity = totalDrops / dropsPerMl; // Quantidade em mL
                        displayConcentration = $"{comp.Concentration} gotas/mL";
                        break;

                    default:
                        quantity = comp.Concentration;
                        displayConcentration = $"{comp.Concentration} {comp.ConcentrationType}";
                        break;
                }

                // Aplicar fatores de correção
                var correctionFactor = comp.CorrectionFactor ?? (decimal?)(rawMaterial?.CorrectionFactor) ?? 1.0m;
                var purityFactor = comp.PurityFactor ?? rawMaterial?.PurityFactor ?? 1.0m;
                var correctedQuantity = quantity * correctionFactor / purityFactor;

                // Converter para unidade apropriada para exibição
                string displayUnit = "mg";
                decimal displayQuantity = correctedQuantity;

                if (correctedQuantity >= 1000)
                {
                    displayQuantity = correctedQuantity / 1000m;
                    displayUnit = "g";
                }
                else if (correctedQuantity < 1)
                {
                    displayQuantity = correctedQuantity * 1000m;
                    displayUnit = "mcg";
                }

                // Acumular sólidos totais (em gramas)
                totalSolidsGrams += correctedQuantity / 1000m;

                componentResults.Add(new LiquidComponentResultDto
                {
                    RawMaterialId = comp.RawMaterialId,
                    RawMaterialName = comp.RawMaterialName ?? rawMaterial?.Name ?? "N/A",
                    DcbCode = rawMaterial?.DcbCode,
                    ConcentrationInput = displayConcentration,
                    QuantityMg = correctedQuantity,
                    QuantityDisplay = $"{displayQuantity:F4} {displayUnit}",
                    Unit = displayUnit,
                    IsControlled = rawMaterial?.ControlType != "COMUM" && !string.IsNullOrEmpty(rawMaterial?.ControlType),
                    ControlType = rawMaterial?.ControlType,
                    UnitPrice = rawMaterial?.BasePrice,
                    TotalCost = rawMaterial?.BasePrice.HasValue == true ? (correctedQuantity / 1000m) * rawMaterial.BasePrice.Value : null
                });
            }

            result.Components = componentResults;
            result.TotalSolidsGrams = totalSolidsGrams;

            // Calcular veículo QSP
            decimal vehicleVolumeMl = totalVolumeMl; // Por padrão, veículo = volume total
            
            // Em soluções, o volume do veículo deve compensar o volume dos sólidos dissolvidos
            // (simplificação: assumimos que sólidos não alteram significativamente o volume final)

            var vehicleComponents = new List<LiquidComponentResultDto>();

            if (vehicleSubtype?.Compositions != null && vehicleSubtype.Compositions.Any())
            {
                foreach (var vComp in vehicleSubtype.Compositions.OrderBy(c => c.SortOrder))
                {
                    decimal compQuantity;
                    
                    if (vComp.IsQsp)
                    {
                        // QSP = volume total
                        compQuantity = vehicleVolumeMl;
                    }
                    else if (vComp.Percentage.HasValue)
                    {
                        compQuantity = vehicleVolumeMl * (vComp.Percentage.Value / 100m);
                    }
                    else if (vComp.QuantityPerYield.HasValue && vehicleSubtype.YieldQuantity > 0)
                    {
                        compQuantity = vComp.QuantityPerYield.Value * (vehicleVolumeMl / vehicleSubtype.YieldQuantity);
                    }
                    else
                    {
                        compQuantity = 0;
                    }

                    string vUnit = vComp.Unit ?? "mL";
                    
                    vehicleComponents.Add(new LiquidComponentResultDto
                    {
                        RawMaterialId = vComp.RawMaterialId,
                        RawMaterialName = vComp.RawMaterial?.Name ?? "N/A",
                        DcbCode = vComp.RawMaterial?.DcbCode,
                        ConcentrationInput = vComp.IsQsp ? "QSP" : (vComp.Percentage.HasValue ? $"{vComp.Percentage}%" : ""),
                        QuantityMg = vUnit == "mL" ? compQuantity : compQuantity * 1000,
                        QuantityDisplay = $"{compQuantity:F2} {vUnit}",
                        Unit = vUnit,
                        IsQsp = vComp.IsQsp,
                        IsOptional = vComp.IsOptional,
                        UnitPrice = vComp.RawMaterial?.BasePrice,
                        TotalCost = vComp.RawMaterial?.BasePrice.HasValue == true ? compQuantity * vComp.RawMaterial.BasePrice.Value : null
                    });
                }
            }
            else
            {
                // Veículo genérico
                vehicleComponents.Add(new LiquidComponentResultDto
                {
                    RawMaterialId = Guid.Empty,
                    RawMaterialName = "Veículo/Solvente (QSP)",
                    ConcentrationInput = "QSP",
                    QuantityMg = vehicleVolumeMl,
                    QuantityDisplay = $"{vehicleVolumeMl:F2} mL",
                    Unit = "mL",
                    IsQsp = true
                });
            }

            result.VehicleComponents = vehicleComponents;
            result.VehicleVolumeMl = vehicleVolumeMl;

            // Calcular custos
            result.TotalActiveCost = componentResults.Where(c => c.TotalCost.HasValue).Sum(c => c.TotalCost!.Value);
            result.TotalVehicleCost = vehicleComponents.Where(c => c.TotalCost.HasValue).Sum(c => c.TotalCost!.Value);
            result.TotalMaterialsCost = result.TotalActiveCost + result.TotalVehicleCost;

            // Validações e alertas
            ValidateFormulation(result, request);

            result.Success = true;
            result.Message = $"Formulação calculada: {request.TotalVolume} {request.VolumeUnit} com {request.Components.Count} componente(s)";

            _logger.LogInformation(
                "Cálculo de formulação líquida: {Volume}{Unit} - {Components} componentes",
                request.TotalVolume, request.VolumeUnit, request.Components.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular formulação líquida");
            result.Message = $"Erro no cálculo: {ex.Message}";
            return result;
        }
    }

    private void ValidateFormulation(LiquidCalculationResultDto result, LiquidCalculationRequestDto request)
    {
        // Verificar concentração de álcool para xaropes pediátricos
        if (request.FormType?.ToUpper() == "XAROPE" && request.IsPediatric)
        {
            // Xaropes pediátricos não devem conter álcool
            result.Warnings.Add("⚠️ Verificar: xaropes pediátricos não devem conter álcool");
        }

        // Verificar volume máximo para gotas
        if (request.FormType?.ToUpper() == "GOTAS" && result.TotalVolume > 30)
        {
            result.Warnings.Add("⚠️ Volume incomum para gotas (geralmente até 30mL)");
        }

        // Verificar concentração de açúcar em xaropes
        if (request.FormType?.ToUpper() == "XAROPE")
        {
            result.Warnings.Add("💡 Xaropes devem conter 45-85% de sacarose ou equivalente");
        }
    }

    /// <summary>
    /// Lista veículos disponíveis para formas líquidas
    /// </summary>
    public async Task<List<VehicleSubtypeDto>> GetAvailableVehiclesAsync(
        Guid pharmaceuticalFormId,
        Guid establishmentId)
    {
        return await _context.PharmaceuticalFormSubtypes
            .Where(s => s.PharmaceuticalFormId == pharmaceuticalFormId
                     && s.EstablishmentId == establishmentId
                     && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new VehicleSubtypeDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IsDefault = s.IsDefault,
                YieldQuantity = s.YieldQuantity,
                YieldUnit = s.YieldUnit
            })
            .ToListAsync();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════════════

public class LiquidCalculationRequestDto
{
    public Guid? VehicleSubtypeId { get; set; }
    public string? FormType { get; set; } // SOLUCAO, XAROPE, SUSPENSAO, ELIXIR, GOTAS
    public decimal TotalVolume { get; set; }
    public string VolumeUnit { get; set; } = "mL";
    public bool IsPediatric { get; set; }
    public List<LiquidComponentRequestDto> Components { get; set; } = new();
}

public class LiquidComponentRequestDto
{
    public Guid RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public decimal Concentration { get; set; }
    public string ConcentrationType { get; set; } = "mg/mL"; // mg/mL, g/L, %, GOTAS/ML, ABSOLUTE
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
    public decimal? DropsPerMl { get; set; } // Para cálculo de gotas
}

public class LiquidCalculationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public string? VehicleName { get; set; }
    public string? VehicleCode { get; set; }

    public decimal TotalVolume { get; set; }
    public string VolumeUnit { get; set; } = "mL";
    public decimal VehicleVolumeMl { get; set; }
    public decimal TotalSolidsGrams { get; set; }

    public List<LiquidComponentResultDto> Components { get; set; } = new();
    public List<LiquidComponentResultDto> VehicleComponents { get; set; } = new();

    public decimal TotalActiveCost { get; set; }
    public decimal TotalVehicleCost { get; set; }
    public decimal TotalMaterialsCost { get; set; }

    public List<string> Warnings { get; set; } = new();
}

public class LiquidComponentResultDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public string ConcentrationInput { get; set; } = string.Empty;
    public decimal QuantityMg { get; set; }
    public string QuantityDisplay { get; set; } = string.Empty;
    public string Unit { get; set; } = "mg";
    public bool IsControlled { get; set; }
    public string? ControlType { get; set; }
    public bool IsQsp { get; set; }
    public bool IsOptional { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalCost { get; set; }
}

public class VehicleSubtypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = "mL";
}
