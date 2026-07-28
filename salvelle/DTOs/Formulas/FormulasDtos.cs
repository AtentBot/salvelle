namespace DTOs.Formulas;

public class CreateFormulaDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public decimal StandardYield { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? PreparationInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    public bool RequiresSpecialControl { get; set; } = false;
    public bool RequiresPrescription { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<CreateFormulaComponentDto> Components { get; set; } = new();
}

public class CreateFormulaComponentDto
{
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "ATIVO";
    public int OrderIndex { get; set; } = 0;  // ? ADICIONADO
    public string? SpecialInstructions { get; set; }
    public bool IsOptional { get; set; } = false;
}

public class UpdateFormulaDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public decimal StandardYield { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? PreparationInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    public bool RequiresSpecialControl { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }
    public List<UpdateFormulaComponentDto> Components { get; set; } = new();
}

public class UpdateFormulaComponentDto
{
    public Guid? Id { get; set; }
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ComponentType { get; set; } = "ATIVO";
    public int OrderIndex { get; set; } = 0;  // ? ADICIONADO
    public string? SpecialInstructions { get; set; }
    public bool IsOptional { get; set; }
}

public class FormulaResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public decimal StandardYield { get; set; }
    public int ShelfLifeDays { get; set; }
    public string? PreparationInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public string? UsageInstructions { get; set; }
    public bool RequiresSpecialControl { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalCost { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByEmployeeName { get; set; }
    public List<FormulaComponentResponseDto> Components { get; set; } = new();
}

public class FormulaComponentResponseDto
{
    public Guid Id { get; set; }
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? RawMaterialDcbCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string? SpecialInstructions { get; set; }
    public bool IsOptional { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsAvailable { get; set; }
    public decimal AvailableStock { get; set; }
}

public class FormulaCostCalculationDto
{
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<ComponentCostDto> ComponentsCost { get; set; } = new();
}

public class ComponentCostDto
{
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsAvailable { get; set; }
}


public class FormulaComponentDto
{
    public Guid Id { get; set; }
    public Guid FormulaId { get; set; }
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string? RawMaterialCode { get; set; }  // ? ADICIONADO
    public string Unit { get; set; } = string.Empty;
    public string? RawMaterialUnit { get; set; }  // ? ADICIONADO
    public decimal Quantity { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string? SpecialInstructions { get; set; }
    public bool IsOptional { get; set; }
    public bool RawMaterialIsControlled { get; set; } = false;  // ? ADICIONADO
}

// ? DTO SIMPLES PARA LISTAGEM DE FÓRMULAS (uso em outros módulos)
public class FormulaListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal TotalCost { get; set; }
}