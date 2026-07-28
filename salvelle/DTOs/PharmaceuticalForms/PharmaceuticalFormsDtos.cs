namespace DTOs.PharmaceuticalForms;

// ═══════════════════════════════════════════════════════════════════════════
// FORMA FARMACÊUTICA
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO para listagem de formas farmacêuticas
/// </summary>
public class PharmaceuticalFormListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemDefault { get; set; }
    public bool IsCustom { get; set; }
    public decimal MinimumPrice { get; set; }
    public decimal? MaxQuantityLimit { get; set; }
    public int DefaultValidityDays { get; set; }
    public string DefaultUnit { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int SubtypesCount { get; set; }
}

/// <summary>
/// DTO detalhado de forma farmacêutica
/// </summary>
public class PharmaceuticalFormDetailDto : PharmaceuticalFormListDto
{
    public decimal? PreparationTimeHours { get; set; }
    public string? UsageInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PharmaceuticalFormSubtypeListDto> Subtypes { get; set; } = new();
}

/// <summary>
/// DTO para criar/atualizar forma farmacêutica
/// </summary>
public class CreatePharmaceuticalFormDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal MinimumPrice { get; set; }
    public decimal? MaxQuantityLimit { get; set; }
    public int DefaultValidityDays { get; set; } = 180;
    public string DefaultUnit { get; set; } = "g";
    public decimal? PreparationTimeHours { get; set; }
    public string? UsageInstructions { get; set; }
    public string UsageType { get; set; } = "BOTH";
    public string Icon { get; set; } = "bi-capsule";
    public int SortOrder { get; set; } = 100;
}

/// <summary>
/// DTO para atualizar forma farmacêutica
/// </summary>
public class UpdatePharmaceuticalFormDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? MaxQuantityLimit { get; set; }
    public int? DefaultValidityDays { get; set; }
    public string? DefaultUnit { get; set; }
    public decimal? PreparationTimeHours { get; set; }
    public string? UsageInstructions { get; set; }
    public string? UsageType { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>
/// DTO para toggle ativo/inativo
/// </summary>
public class ToggleActiveDto
{
    public bool IsActive { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// SUBTIPO DE FORMA FARMACÊUTICA
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO para listagem de subtipos
/// </summary>
public class PharmaceuticalFormSubtypeListDto
{
    public Guid Id { get; set; }
    public Guid PharmaceuticalFormId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal BaseCost { get; set; }
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = string.Empty;
    public int? ValidityDays { get; set; }
    
    // Cápsulas
    public string? CapsuleSize { get; set; }
    public decimal? CapsuleVolumeMl { get; set; }
    public decimal? CapsuleCapacityMgMin { get; set; }
    public decimal? CapsuleCapacityMgMax { get; set; }
    public string? CapsuleColor { get; set; }
    
    public int SortOrder { get; set; }
    public int CompositionsCount { get; set; }
}

/// <summary>
/// DTO detalhado de subtipo
/// </summary>
public class PharmaceuticalFormSubtypeDetailDto : PharmaceuticalFormSubtypeListDto
{
    public string PharmaceuticalFormName { get; set; } = string.Empty;
    public decimal? MaxQuantityLimit { get; set; }
    public string? PreparationInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PharmaceuticalFormCompositionDto> Compositions { get; set; } = new();
}

/// <summary>
/// DTO para criar subtipo
/// </summary>
public class CreatePharmaceuticalFormSubtypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public decimal? MinimumPrice { get; set; }
    public decimal YieldQuantity { get; set; } = 100;
    public string YieldUnit { get; set; } = "g";
    public int? ValidityDays { get; set; }
    public decimal? MaxQuantityLimit { get; set; }
    
    // Cápsulas
    public string? CapsuleSize { get; set; }
    public decimal? CapsuleVolumeMl { get; set; }
    public decimal? CapsuleCapacityMgMin { get; set; }
    public decimal? CapsuleCapacityMgMax { get; set; }
    public string? CapsuleColor { get; set; }
    
    public string? PreparationInstructions { get; set; }
    public int SortOrder { get; set; } = 100;
}

/// <summary>
/// DTO para atualizar subtipo
/// </summary>
public class UpdatePharmaceuticalFormSubtypeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? YieldQuantity { get; set; }
    public string? YieldUnit { get; set; }
    public int? ValidityDays { get; set; }
    public decimal? MaxQuantityLimit { get; set; }
    
    // Cápsulas
    public string? CapsuleSize { get; set; }
    public decimal? CapsuleVolumeMl { get; set; }
    public decimal? CapsuleCapacityMgMin { get; set; }
    public decimal? CapsuleCapacityMgMax { get; set; }
    public string? CapsuleColor { get; set; }
    
    public string? PreparationInstructions { get; set; }
    public int? SortOrder { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// COMPOSIÇÃO
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO de composição
/// </summary>
public class PharmaceuticalFormCompositionDto
{
    public Guid Id { get; set; }
    public Guid SubtypeId { get; set; }
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? RawMaterialCode { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? QuantityPerYield { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsQsp { get; set; }
    public bool IsOptional { get; set; }
    public int SortOrder { get; set; }
    
    // Custo calculado
    public decimal? UnitPrice { get; set; }
    public decimal? TotalCost { get; set; }
}

/// <summary>
/// DTO para criar/atualizar composição
/// </summary>
public class CreatePharmaceuticalFormCompositionDto
{
    public Guid RawMaterialId { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? QuantityPerYield { get; set; }
    public string Unit { get; set; } = "g";
    public bool IsQsp { get; set; } = false;
    public bool IsOptional { get; set; } = false;
    public int SortOrder { get; set; } = 100;
}

// ═══════════════════════════════════════════════════════════════════════════
// CÁLCULO DE CÁPSULAS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO para requisição de cálculo de cápsulas
/// </summary>
public class CapsuleCalculationRequestDto
{
    public List<CapsuleIngredientDto> Ingredients { get; set; } = new();
    public int RequestedCapsuleCount { get; set; }
    public string? PreferredCapsuleSize { get; set; }
}

/// <summary>
/// DTO de ingrediente para cálculo
/// </summary>
public class CapsuleIngredientDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal DoseMg { get; set; }
    public decimal? Density { get; set; }
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
    public decimal? EquivalenceFactor { get; set; }
    public decimal? DilutionFactor { get; set; }
}

/// <summary>
/// DTO de resultado do cálculo de cápsulas
/// </summary>
public class CapsuleCalculationResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    
    // Resultado principal
    public string SelectedCapsuleSize { get; set; } = string.Empty;
    public decimal CapsuleVolumeMl { get; set; }
    public decimal CapsulePracticalCapacityMg { get; set; }
    
    // Quantidades
    public int RequestedCapsuleCount { get; set; }
    public int FinalCapsuleCount { get; set; }
    public int Multiplier { get; set; } = 1;
    public int CapsulesPerDose { get; set; } = 1;
    
    // Pesos
    public decimal TotalActivesWeightMg { get; set; }
    public decimal ExcipientWeightMgPerDose { get; set; }
    public decimal TotalExcipientWeightMg { get; set; }
    
    // Alertas
    public bool RequiresMultipleCapsules { get; set; }
    public bool SuggestSachet { get; set; }
    public List<string> Warnings { get; set; } = new();
    
    // Detalhamento por ingrediente
    public List<CapsuleIngredientResultDto> IngredientResults { get; set; } = new();
}

/// <summary>
/// DTO de resultado por ingrediente
/// </summary>
public class CapsuleIngredientResultDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal OriginalDoseMg { get; set; }
    public decimal CorrectedDoseMg { get; set; }
    public decimal WeightPerDoseMg { get; set; }
    public decimal TotalWeightMg { get; set; }
    public decimal TotalWeightG { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// REFERÊNCIA DE TAMANHOS DE CÁPSULAS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO de referência de tamanho de cápsula
/// </summary>
public class CapsuleSizeReferenceDto
{
    public Guid Id { get; set; }
    public string SizeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal VolumeMl { get; set; }
    public decimal CapacityMgMin { get; set; }
    public decimal CapacityMgMax { get; set; }
    public decimal PracticalCapacityMg { get; set; }
    public bool IsActive { get; set; }
    public bool IsCommon { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO para atualizar referência de cápsula
/// </summary>
public class UpdateCapsuleSizeReferenceDto
{
    public decimal? VolumeMl { get; set; }
    public decimal? CapacityMgMin { get; set; }
    public decimal? CapacityMgMax { get; set; }
    public decimal? PracticalCapacityMg { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsCommon { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// CONFIGURAÇÃO DE PRECIFICAÇÃO
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO de configuração de precificação
/// </summary>
public class EstablishmentPricingConfigDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    
    public decimal TaxPercentage { get; set; }
    
    public string Fee1Name { get; set; } = string.Empty;
    public decimal Fee1Percentage { get; set; }
    
    public string Fee2Name { get; set; } = string.Empty;
    public decimal Fee2Percentage { get; set; }
    
    public string Fee3Name { get; set; } = string.Empty;
    public decimal Fee3Percentage { get; set; }
    
    public decimal MarkupPercentage { get; set; }
    public decimal PackagingPercentage { get; set; }
    
    public bool ApplyMinimumPrice { get; set; }
    public bool RoundToCents { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para atualizar configuração de precificação
/// </summary>
public class UpdateEstablishmentPricingConfigDto
{
    public decimal? TaxPercentage { get; set; }
    
    public string? Fee1Name { get; set; }
    public decimal? Fee1Percentage { get; set; }
    
    public string? Fee2Name { get; set; }
    public decimal? Fee2Percentage { get; set; }
    
    public string? Fee3Name { get; set; }
    public decimal? Fee3Percentage { get; set; }
    
    public decimal? MarkupPercentage { get; set; }
    public decimal? PackagingPercentage { get; set; }
    
    public bool? ApplyMinimumPrice { get; set; }
    public bool? RoundToCents { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// CÁLCULO DE PREÇO
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// DTO de detalhamento de preço
/// </summary>
public class PriceBreakdownDto
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
    
    public decimal TotalAdditions { get; set; }
    public decimal CalculatedPrice { get; set; }
    public decimal? MinimumPrice { get; set; }
    public bool MinimumPriceApplied { get; set; }
    public decimal FinalPrice { get; set; }
}
