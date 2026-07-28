using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using DTOs.PharmaceuticalForms;

namespace Service.PharmaceuticalForms;

/// <summary>
/// Serviço de cálculo automático de cápsulas
/// Implementa a lógica de seleção de tamanho e multiplicador
/// </summary>
public class CapsuleCalculationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CapsuleCalculationService> _logger;

    public CapsuleCalculationService(AppDbContext context, ILogger<CapsuleCalculationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula automaticamente o tamanho de cápsula ideal e quantidades
    /// </summary>
    public async Task<CapsuleCalculationResultDto> CalculateAsync(
        CapsuleCalculationRequestDto request,
        Guid establishmentId)
    {
        var result = new CapsuleCalculationResultDto
        {
            RequestedCapsuleCount = request.RequestedCapsuleCount,
            Success = false
        };

        try
        {
            // Validações iniciais
            if (request.Ingredients == null || !request.Ingredients.Any())
            {
                result.Message = "Nenhum ingrediente informado";
                return result;
            }

            if (request.RequestedCapsuleCount <= 0)
            {
                result.Message = "Quantidade de cápsulas deve ser maior que zero";
                return result;
            }

            // Buscar referências de tamanhos de cápsulas
            var capsuleSizes = await _context.CapsuleSizeReferences
                .Where(c => c.EstablishmentId == establishmentId && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            if (!capsuleSizes.Any())
            {
                result.Message = "Nenhum tamanho de cápsula configurado para este estabelecimento";
                return result;
            }

            // Buscar dados das matérias-primas (densidade, fatores)
            var rawMaterialIds = request.Ingredients.Select(i => i.RawMaterialId).ToList();
            var rawMaterials = await _context.RawMaterials
                .Where(r => rawMaterialIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

            // Calcular peso total dos ativos por dose
            decimal totalActiveWeightMgPerDose = 0;
            var ingredientResults = new List<CapsuleIngredientResultDto>();

            foreach (var ingredient in request.Ingredients)
            {
                rawMaterials.TryGetValue(ingredient.RawMaterialId, out var rawMaterial);

                // Usar valores do request ou da matéria-prima cadastrada
                var density = ingredient.Density ?? (decimal?)(rawMaterial?.BulkDensity) ?? 1.0m;
                var correctionFactor = ingredient.CorrectionFactor ?? (decimal?)(rawMaterial?.CorrectionFactor) ?? 1.0m;
                var purityFactor = ingredient.PurityFactor ?? rawMaterial?.PurityFactor ?? 1.0m;
                var equivalenceFactor = ingredient.EquivalenceFactor ?? rawMaterial?.EquivalenceFactor ?? 1.0m;
                var dilutionFactor = ingredient.DilutionFactor ?? (decimal?)(rawMaterial?.DilutionFactor) ?? 1.0m;

                // Calcular dose corrigida
                // Fórmula: dose × diluição × equivalência × correção / pureza
                var correctedDoseMg = CalculateCorrectedDose(
                    ingredient.DoseMg,
                    correctionFactor,
                    purityFactor,
                    equivalenceFactor,
                    dilutionFactor);

                // Calcular peso real considerando densidade
                // Se densidade < 1, o pó ocupa mais volume (é mais leve)
                // Peso efetivo para volume = dose / densidade
                var weightForVolumeMg = density > 0 ? correctedDoseMg / density : correctedDoseMg;

                totalActiveWeightMgPerDose += weightForVolumeMg;

                ingredientResults.Add(new CapsuleIngredientResultDto
                {
                    RawMaterialId = ingredient.RawMaterialId,
                    RawMaterialName = ingredient.RawMaterialName ?? rawMaterial?.Name ?? "N/A",
                    OriginalDoseMg = ingredient.DoseMg,
                    CorrectedDoseMg = correctedDoseMg,
                    WeightPerDoseMg = weightForVolumeMg,
                    TotalWeightMg = weightForVolumeMg * request.RequestedCapsuleCount,
                    TotalWeightG = (weightForVolumeMg * request.RequestedCapsuleCount) / 1000m
                });
            }

            result.TotalActivesWeightMg = totalActiveWeightMgPerDose;
            result.IngredientResults = ingredientResults;

            // Selecionar tamanho de cápsula
            CapsuleSizeReference? selectedSize = null;
            int multiplier = 1;

            // Se tem preferência, tentar usar
            if (!string.IsNullOrEmpty(request.PreferredCapsuleSize))
            {
                selectedSize = capsuleSizes.FirstOrDefault(c => 
                    c.SizeCode.Equals(request.PreferredCapsuleSize, StringComparison.OrdinalIgnoreCase));
            }

            // Se não tem preferência ou a preferida não comporta, calcular automaticamente
            if (selectedSize == null || !selectedSize.CanFit(totalActiveWeightMgPerDose))
            {
                // Encontrar a menor cápsula que comporta o peso
                selectedSize = capsuleSizes
                    .OrderBy(c => c.PracticalCapacityMg)
                    .FirstOrDefault(c => c.CanFit(totalActiveWeightMgPerDose));

                // Se nenhuma cápsula comporta, calcular multiplicador
                if (selectedSize == null)
                {
                    // Usar a maior cápsula disponível e calcular quantas serão necessárias
                    var largestCapsule = capsuleSizes
                        .OrderByDescending(c => c.PracticalCapacityMg)
                        .First();

                    multiplier = largestCapsule.CalculateCapsuleCount(totalActiveWeightMgPerDose);
                    selectedSize = largestCapsule;

                    result.RequiresMultipleCapsules = true;
                    result.Warnings.Add($"Dose requer {multiplier} cápsula(s) por vez");

                    // Se multiplicador > 4, sugerir sachê
                    if (multiplier > 4)
                    {
                        result.SuggestSachet = true;
                        result.Warnings.Add("Considere usar sachê como alternativa - dose muito alta para cápsulas");
                    }
                }
            }

            // Calcular excipiente necessário
            var capsuleCapacity = selectedSize!.PracticalCapacityMg * multiplier;
            var excipientPerDose = capsuleCapacity - totalActiveWeightMgPerDose;
            if (excipientPerDose < 0) excipientPerDose = 0;

            // Preencher resultado
            result.Success = true;
            result.SelectedCapsuleSize = selectedSize.SizeCode;
            result.CapsuleVolumeMl = selectedSize.VolumeMl;
            result.CapsulePracticalCapacityMg = selectedSize.PracticalCapacityMg;
            result.Multiplier = multiplier;
            result.CapsulesPerDose = multiplier;
            result.FinalCapsuleCount = request.RequestedCapsuleCount * multiplier;
            result.ExcipientWeightMgPerDose = excipientPerDose;
            result.TotalExcipientWeightMg = excipientPerDose * request.RequestedCapsuleCount;
            result.Message = GenerateResultMessage(result);

            _logger.LogInformation(
                "Cálculo de cápsulas: {Count}x {Size} (multiplicador: {Mult}) - Total ativos: {Weight}mg",
                result.FinalCapsuleCount, selectedSize.SizeCode, multiplier, totalActiveWeightMgPerDose);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular cápsulas");
            result.Message = $"Erro no cálculo: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Calcula a dose corrigida aplicando todos os fatores
    /// </summary>
    private decimal CalculateCorrectedDose(
        decimal originalDoseMg,
        decimal correctionFactor,
        decimal purityFactor,
        decimal equivalenceFactor,
        decimal dilutionFactor)
    {
        // Fórmula: dose × diluição × equivalência × correção / pureza
        // 
        // correctionFactor: Ajuste por variação entre lotes (geralmente > 1)
        // purityFactor: Teor real do ativo (ex: 0.98 para 98%)
        // equivalenceFactor: Conversão de unidades (ex: UI para mg)
        // dilutionFactor: Se o ativo é diluído (ex: 10 para 1:10)

        if (purityFactor <= 0) purityFactor = 1;

        return originalDoseMg * dilutionFactor * equivalenceFactor * correctionFactor / purityFactor;
    }

    /// <summary>
    /// Gera mensagem descritiva do resultado
    /// </summary>
    private string GenerateResultMessage(CapsuleCalculationResultDto result)
    {
        if (result.SuggestSachet)
        {
            return $"Dose muito alta ({result.TotalActivesWeightMg:F0}mg). " +
                   $"Requer {result.CapsulesPerDose} cápsulas {result.SelectedCapsuleSize} por vez. " +
                   "Considere usar sachê.";
        }

        if (result.RequiresMultipleCapsules)
        {
            return $"Usar {result.CapsulesPerDose} cápsula(s) {result.SelectedCapsuleSize} por dose. " +
                   $"Total: {result.FinalCapsuleCount} cápsulas. " +
                   $"Excipiente: {result.ExcipientWeightMgPerDose:F0}mg por dose.";
        }

        return $"Cápsula {result.SelectedCapsuleSize} selecionada. " +
               $"Total: {result.FinalCapsuleCount} cápsulas. " +
               $"Excipiente: {result.ExcipientWeightMgPerDose:F0}mg por cápsula.";
    }

    /// <summary>
    /// Sugere o melhor tamanho de cápsula para uma dose
    /// </summary>
    public async Task<CapsuleSizeReferenceDto?> SuggestCapsuleSizeAsync(
        decimal doseMg,
        Guid establishmentId)
    {
        var sizes = await _context.CapsuleSizeReferences
            .Where(c => c.EstablishmentId == establishmentId && c.IsActive)
            .OrderBy(c => c.PracticalCapacityMg)
            .ToListAsync();

        var suggested = sizes.FirstOrDefault(c => c.PracticalCapacityMg >= doseMg);

        if (suggested == null && sizes.Any())
        {
            suggested = sizes.OrderByDescending(c => c.PracticalCapacityMg).First();
        }

        if (suggested == null) return null;

        return new CapsuleSizeReferenceDto
        {
            Id = suggested.Id,
            SizeCode = suggested.SizeCode,
            Name = suggested.Name,
            VolumeMl = suggested.VolumeMl,
            CapacityMgMin = suggested.CapacityMgMin,
            CapacityMgMax = suggested.CapacityMgMax,
            PracticalCapacityMg = suggested.PracticalCapacityMg,
            IsActive = suggested.IsActive,
            IsCommon = suggested.IsCommon,
            SortOrder = suggested.SortOrder
        };
    }

    /// <summary>
    /// Lista todos os tamanhos de cápsulas do estabelecimento
    /// </summary>
    public async Task<List<CapsuleSizeReferenceDto>> GetCapsuleSizesAsync(
        Guid establishmentId,
        bool onlyActive = true)
    {
        var query = _context.CapsuleSizeReferences
            .Where(c => c.EstablishmentId == establishmentId);

        if (onlyActive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.SortOrder)
            .Select(c => new CapsuleSizeReferenceDto
            {
                Id = c.Id,
                SizeCode = c.SizeCode,
                Name = c.Name,
                VolumeMl = c.VolumeMl,
                CapacityMgMin = c.CapacityMgMin,
                CapacityMgMax = c.CapacityMgMax,
                PracticalCapacityMg = c.PracticalCapacityMg,
                IsActive = c.IsActive,
                IsCommon = c.IsCommon,
                SortOrder = c.SortOrder
            })
            .ToListAsync();
    }

    /// <summary>
    /// Atualiza configuração de um tamanho de cápsula
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateCapsuleSizeAsync(
        Guid sizeId,
        UpdateCapsuleSizeReferenceDto dto,
        Guid establishmentId)
    {
        var size = await _context.CapsuleSizeReferences
            .FirstOrDefaultAsync(c => c.Id == sizeId && c.EstablishmentId == establishmentId);

        if (size == null)
            return (false, "Tamanho de cápsula não encontrado");

        if (dto.VolumeMl.HasValue) size.VolumeMl = dto.VolumeMl.Value;
        if (dto.CapacityMgMin.HasValue) size.CapacityMgMin = dto.CapacityMgMin.Value;
        if (dto.CapacityMgMax.HasValue) size.CapacityMgMax = dto.CapacityMgMax.Value;
        if (dto.PracticalCapacityMg.HasValue) size.PracticalCapacityMg = dto.PracticalCapacityMg.Value;
        if (dto.IsActive.HasValue) size.IsActive = dto.IsActive.Value;
        if (dto.IsCommon.HasValue) size.IsCommon = dto.IsCommon.Value;

        size.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Tamanho de cápsula atualizado com sucesso");
    }
}

/// <summary>
/// Extensão para adicionar propriedade BulkDensity e outras ao RawMaterial
/// (para compatibilidade até a migration ser aplicada)
/// </summary>
public static class RawMaterialExtensions
{
    public static decimal? GetBulkDensity(this RawMaterial material)
    {
        // Tentar obter via reflexão (campo pode não existir ainda)
        var prop = material.GetType().GetProperty("BulkDensity");
        if (prop != null)
        {
            return (decimal?)prop.GetValue(material);
        }
        return null;
    }

    public static decimal? GetCorrectionFactor(this RawMaterial material)
    {
        var prop = material.GetType().GetProperty("CorrectionFactor");
        if (prop != null)
        {
            return (decimal?)prop.GetValue(material);
        }
        return null;
    }

    public static decimal? GetDilutionFactor(this RawMaterial material)
    {
        var prop = material.GetType().GetProperty("DilutionFactor");
        if (prop != null)
        {
            return (decimal?)prop.GetValue(material);
        }
        return null;
    }
}
