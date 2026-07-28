namespace ViewModels.Manipulation;

/// <summary>
/// ViewModel para a tela de pesagem de componentes
/// </summary>
public class WeighingViewModel
{
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? FormulaName { get; set; }
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = "un";
    
    public List<ComponentWeighingItem> Components { get; set; } = new();
}

/// <summary>
/// Item de componente para pesagem
/// </summary>
public class ComponentWeighingItem
{
    public Guid ComponentId { get; set; }
    public string RawMaterialName { get; set; } = "";
    public string? DcbCode { get; set; }
    public decimal UnitQuantity { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = "";
    public bool IsControlled { get; set; }
    
    public List<AvailableBatchItem> AvailableBatches { get; set; } = new();
    
    // Dados preenchidos pelo operador
    public Guid? SelectedBatchId { get; set; }
    public decimal WeighedQuantity { get; set; }
    public string? WeighingNotes { get; set; }
}

/// <summary>
/// Lote disponível para seleção na pesagem
/// </summary>
public class AvailableBatchItem
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = "";
    public decimal AvailableQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExpiringSoon { get; set; }
    
    public string ExpiryDateFormatted => ExpiryDate.ToString("dd/MM/yyyy");
    public string Status => IsExpiringSoon ? "warning" : "ok";
}

/// <summary>
/// ViewModel para a ficha de pesagem
/// </summary>
public class WeighingSheetViewModel
{
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? FormulaName { get; set; }
    public string? FormulaCode { get; set; }
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = "";
    public string? PharmaceuticalForm { get; set; }
    
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    
    public string? PrescriberName { get; set; }
    public string? PrescriptionNumber { get; set; }
    
    public List<WeighingSheetComponent> Components { get; set; } = new();
    
    public string? SpecialInstructions { get; set; }
}

/// <summary>
/// Componente na ficha de pesagem
/// </summary>
public class WeighingSheetComponent
{
    public int OrderIndex { get; set; }
    public string RawMaterialName { get; set; } = "";
    public string? DcbCode { get; set; }
    public string? CasNumber { get; set; }
    public decimal UnitQuantity { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = "";
    public bool IsControlled { get; set; }
    public string? ControlType { get; set; }
    
    // Campos para preenchimento manual
    public string? BatchNumber { get; set; }
    public string? WeighedBy { get; set; }
    public string? CheckedBy { get; set; }
}

/// <summary>
/// ViewModel para histórico de etapas
/// </summary>
public class StepHistoryViewModel
{
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string Status { get; set; } = "";
    
    public List<StepHistoryItem> Steps { get; set; } = new();
}

/// <summary>
/// Item do histórico de etapas
/// </summary>
public class StepHistoryItem
{
    public Guid StepId { get; set; }
    public string StepType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? PerformedByName { get; set; }
    public string? CheckedByName { get; set; }
    public string? Observations { get; set; }
    public bool PassedCheck { get; set; }
    
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue 
        ? CompletedAt.Value - StartedAt.Value 
        : null;
        
    public string DurationFormatted => Duration.HasValue 
        ? $"{(int)Duration.Value.TotalMinutes}min" 
        : "N/A";
}

/// <summary>
/// ViewModel para dashboard de produção
/// </summary>
public class ProductionDashboardViewModel
{
    public int TotalPendentes { get; set; }
    public int TotalEmProducao { get; set; }
    public int TotalFinalizadosHoje { get; set; }
    public int TotalAtrasados { get; set; }
    
    public decimal TaxaAprovacao { get; set; }
    public TimeSpan TempoMedioProducao { get; set; }
    
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public List<DailyProductionItem> ProducaoSemanal { get; set; } = new();
}

/// <summary>
/// Produção diária para gráfico
/// </summary>
public class DailyProductionItem
{
    public DateTime Date { get; set; }
    public int Quantity { get; set; }
    
    public string DateFormatted => Date.ToString("dd/MM");
}

/// <summary>
/// ViewModel para criar/editar ordem de manipulação
/// </summary>
public class ManipulationOrderFormViewModel
{
    public Guid? Id { get; set; }
    public Guid? FormulaId { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string? PrescriberName { get; set; }
    public string? PrescriberRegistration { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerPhone { get; set; }
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = "un";
    public string? SpecialInstructions { get; set; }
    public DateTime ExpectedDate { get; set; } = DateTime.UtcNow.AddDays(3);
    
    // Listas para dropdowns
    public List<FormulaSelectItem> AvailableFormulas { get; set; } = new();
}

/// <summary>
/// Item de fórmula para seleção
/// </summary>
public class FormulaSelectItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? PharmaceuticalForm { get; set; }
    
    public string DisplayName => $"{Code} - {Name}";
}
