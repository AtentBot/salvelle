namespace DTOs.Pharmacy.ManipulationOrders;

// ===================================================================
// DTOs DE PERDAS
// ===================================================================

public class RegisterLossDto
{
    public Guid? RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string LossType { get; set; } = "PROCESSO"; // PROCESSO, QUEBRA, CONTAMINACAO, VENCIMENTO, OUTRO
    public string? BatchNumber { get; set; }
    public decimal? ValueLost { get; set; }
}

public class ManipulationLossDto
{
    public Guid Id { get; set; }
    public Guid ManipulationOrderId { get; set; }
    public Guid? RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string LossType { get; set; } = default!;
    public string? BatchNumber { get; set; }
    public decimal? ValueLost { get; set; }
    public Guid RegisteredByEmployeeId { get; set; }
    public string? RegisteredByEmployeeName { get; set; }
    public DateTime RegisteredAt { get; set; }
}

// ===================================================================
// DTOs DE SOBRAS
// ===================================================================

public class RegisterLeftoverDto
{
    public Guid? RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string Destination { get; set; } = default!; // REINTEGRAR_ESTOQUE, DESCARTE, OUTRA_MANIPULACAO
    public string? DestinationDetails { get; set; }
    public string? BatchNumber { get; set; }
    public bool ReintegrateToStock { get; set; } = false;
}

public class ManipulationLeftoverDto
{
    public Guid Id { get; set; }
    public Guid ManipulationOrderId { get; set; }
    public Guid? RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string Destination { get; set; } = default!;
    public string? DestinationDetails { get; set; }
    public string? BatchNumber { get; set; }
    public bool ReintegratedToStock { get; set; }
    public DateTime? ReintegrationDate { get; set; }
    public Guid RegisteredByEmployeeId { get; set; }
    public string? RegisteredByEmployeeName { get; set; }
    public DateTime RegisteredAt { get; set; }
}

// ===================================================================
// DTOs DE CONFERÊNCIA DUPLA
// ===================================================================

public class StartDualVerificationDto
{
    public string VerificationType { get; set; } = default!; // PESAGEM, ROTULAGEM, CONFERENCIA_FINAL
    public string? Notes { get; set; }
    public string? Signature { get; set; } // Base64 da assinatura digital
    public List<VerificationCheckItem>? ChecklistItems { get; set; }
}

public class CompleteDualVerificationDto
{
    public Guid VerificationId { get; set; }
    public string? Notes { get; set; }
    public string? Signature { get; set; }
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
    public List<VerificationCheckItem>? ChecklistItems { get; set; }
}

public class VerificationCheckItem
{
    public string ItemName { get; set; } = default!;
    public string? Description { get; set; }
    public bool Checked { get; set; }
    public string? Observation { get; set; }
}

public class DualVerificationDto
{
    public Guid Id { get; set; }
    public Guid ManipulationOrderId { get; set; }
    public string VerificationType { get; set; } = default!;
    
    public Guid FirstVerifierId { get; set; }
    public string? FirstVerifierName { get; set; }
    public DateTime FirstVerificationAt { get; set; }
    public string? FirstVerifierNotes { get; set; }
    public bool FirstVerifierSigned { get; set; }
    
    public Guid? SecondVerifierId { get; set; }
    public string? SecondVerifierName { get; set; }
    public DateTime? SecondVerificationAt { get; set; }
    public string? SecondVerifierNotes { get; set; }
    public bool SecondVerifierSigned { get; set; }
    
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
    public string Status { get; set; } = default!; // PENDENTE, COMPLETA, REJEITADA
    public DateTime CreatedAt { get; set; }
}

// ===================================================================
// DTOs DE REGISTRO DE PRODUÇÃO
// ===================================================================

public class RegisterProductionDto
{
    public decimal ActualQuantity { get; set; }
    public string Unit { get; set; } = default!;
    public string? YieldDeviationReason { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime ProductionStart { get; set; }
    public DateTime ProductionEnd { get; set; }
    public string? QualityNotes { get; set; }
    public List<RegisterLossDto>? Losses { get; set; }
    public List<RegisterLeftoverDto>? Leftovers { get; set; }
}

public class ProductionRecordDto
{
    public Guid Id { get; set; }
    public Guid ManipulationOrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public decimal? ExpectedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal? YieldPercentage { get; set; }
    public bool IsYieldAcceptable { get; set; }
    public string? YieldDeviationReason { get; set; }
    public string BatchNumber { get; set; } = default!;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ProductionStart { get; set; }
    public DateTime? ProductionEnd { get; set; }
    public int? TotalProductionTimeMinutes { get; set; }
    public string? ProducedByEmployeeName { get; set; }
    public string? VerifiedByEmployeeName { get; set; }
    public string? ApprovedByPharmacistName { get; set; }
    public string? QualityNotes { get; set; }
    public List<ManipulationLossDto> Losses { get; set; } = new();
    public List<ManipulationLeftoverDto> Leftovers { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ===================================================================
// DTOs DE VERIFICAÇÃO DE ESTOQUE
// ===================================================================

public class StockCheckDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public bool AllAvailable { get; set; }
    public List<StockCheckItemDto> Items { get; set; } = new();
}

public class StockCheckItemDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public string? DcbCode { get; set; }
    public decimal QuantityNeeded { get; set; }
    public decimal QuantityAvailable { get; set; }
    public string Unit { get; set; } = default!;
    public bool IsAvailable { get; set; }
    public decimal Shortage { get; set; }
    public List<AvailableBatchDto> AvailableBatches { get; set; } = new();
}

public class AvailableBatchDto
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = default!;
    public decimal AvailableQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string? SupplierName { get; set; }
}

// ===================================================================
// DTOs DE CUSTO
// ===================================================================

public class ManipulationCostDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public decimal QuantityProduced { get; set; }
    public string Unit { get; set; } = default!;
    public List<ComponentCostDto> ComponentCosts { get; set; } = new();
    public decimal TotalMaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SuggestedPrice { get; set; } // Com margem
    public decimal ProfitMargin { get; set; }
}

public class ComponentCostDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

// ===================================================================
// DTOs DE RESUMO DE PRODUÇÃO
// ===================================================================

public class ProductionSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string? FormulaName { get; set; }
    public string Status { get; set; } = default!;
    
    // Quantidades
    public decimal ExpectedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal? YieldPercentage { get; set; }
    
    // Tempos
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public int? TotalMinutes { get; set; }
    
    // Custos
    public decimal? TotalCost { get; set; }
    public decimal? UnitCost { get; set; }
    
    // Perdas e Sobras
    public int LossesCount { get; set; }
    public decimal TotalLossQuantity { get; set; }
    public decimal? TotalLossValue { get; set; }
    public int LeftoversCount { get; set; }
    public decimal TotalLeftoverQuantity { get; set; }
    
    // Verificações
    public bool HasDualVerification { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByPharmacist { get; set; }
    
    // Steps
    public List<StepSummaryDto> Steps { get; set; } = new();
}

public class StepSummaryDto
{
    public string StepType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? PerformedBy { get; set; }
    public string? CheckedBy { get; set; }
    public bool PassedCheck { get; set; }
}
