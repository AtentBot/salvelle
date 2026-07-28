using System;
using System.Collections.Generic;
using DTOs.Formulas;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para detalhes completos de uma ordem de manipulação
/// </summary>
public class ManipulationOrderDetailsDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public string OrderNumber { get; set; } = default!;

    // Fórmula
    public Guid? FormulaId { get; set; }
    public string? FormulaCode { get; set; }
    public string? FormulaName { get; set; }
    public string? FormulaCategory { get; set; }
    public string? PharmaceuticalForm { get; set; }

    // Prescrição
    public string? PrescriptionNumber { get; set; }
    public string? PrescriberName { get; set; }
    public string? PrescriberRegistration { get; set; }

    // Cliente
    public string CustomerName { get; set; } = default!;
    public string? CustomerPhone { get; set; }

    // Produção
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = default!;
    public string? SpecialInstructions { get; set; }

    // Status e Datas
    public string Status { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // Responsáveis
    public Guid RequestedByEmployeeId { get; set; }
    public string RequestedByEmployeeName { get; set; } = default!;
    
    public Guid? ManipulatedByEmployeeId { get; set; }
    public string? ManipulatedByEmployeeName { get; set; }
    
    public Guid? CheckedByEmployeeId { get; set; }
    public string? CheckedByEmployeeName { get; set; }
    
    public Guid? ApprovedByPharmacistId { get; set; }
    public string? ApprovedByPharmacistName { get; set; }

    // Controle de Qualidade
    public string? QualityNotes { get; set; }
    public bool PassedQualityControl { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Componentes (se houver fórmula)
    public List<FormulaComponentDto>? Components { get; set; }

    // Movimentações de estoque relacionadas
    public List<StockMovementSummaryDto>? StockMovements { get; set; }

    // Timeline de status
    public List<StatusHistoryDto>? StatusHistory { get; set; }

    // Informações calculadas
    public bool IsOverdue { get; set; }
    public int DaysUntilExpected { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
}

/// <summary>
/// DTO para resumo de movimentação de estoque
/// </summary>
public class StockMovementSummaryDto
{
    public Guid Id { get; set; }
    public string RawMaterialName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public string MovementType { get; set; } = default!;
    public DateTime MovementDate { get; set; }
}

/// <summary>
/// DTO para histórico de mudanças de status
/// </summary>
public class StatusHistoryDto
{
    public string FromStatus { get; set; } = default!;
    public string ToStatus { get; set; } = default!;
    public DateTime ChangedAt { get; set; }
    public string ChangedByEmployeeName { get; set; } = default!;
    public string? Notes { get; set; }
}
