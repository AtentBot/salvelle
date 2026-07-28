using System;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para listagem de ordens de manipulação
/// </summary>
public class ManipulationOrderListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string? FormulaCode { get; set; }
    public string? FormulaName { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string RequestedByEmployeeName { get; set; } = default!;
    public string? ManipulatedByEmployeeName { get; set; }
    public bool PassedQualityControl { get; set; }
    public bool IsOverdue { get; set; } // Calculado: ExpectedDate < Now e Status != FINALIZADO
    public int DaysUntilExpected { get; set; } // Calculado
}
