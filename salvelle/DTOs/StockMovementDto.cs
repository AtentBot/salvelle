using System.ComponentModel.DataAnnotations;

namespace DTOs;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public Guid RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public string? Reason { get; set; }
    public Guid? ManipulationOrderId { get; set; }
    public string? ManipulationOrderNumber { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime MovementDate { get; set; }
    public Guid PerformedByEmployeeId { get; set; }
    public string? PerformedByEmployeeName { get; set; }
    public Guid? AuthorizedByEmployeeId { get; set; }
    public string? AuthorizedByEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PrescriptionNumber { get; set; }
    public string? NotificationNumber { get; set; }
}

public class EntradaEstoqueRequest
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    public Guid SupplierId { get; set; }

    [Required]
    [StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitCost { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }
}

public class SaidaEstoqueRequest
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;

    public Guid? ManipulationOrderId { get; set; }

    public Guid? SaleId { get; set; }

    [StringLength(100)]
    public string? DocumentNumber { get; set; }

    [StringLength(50)]
    public string? PrescriptionNumber { get; set; }

    [StringLength(50)]
    public string? NotificationNumber { get; set; }

    public Guid? AuthorizedByEmployeeId { get; set; }
}

public class AjusteEstoqueRequest
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    [Required]
    public decimal QuantityAdjustment { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public Guid AuthorizedByEmployeeId { get; set; }

    [StringLength(100)]
    public string? DocumentNumber { get; set; }
}

public class PerdaEstoqueRequest
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string LossType { get; set; } = string.Empty; // VENCIMENTO, QUEBRA, CONTAMINACAO, DESCARTE

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public Guid AuthorizedByEmployeeId { get; set; }

    [StringLength(100)]
    public string? DocumentNumber { get; set; }
}

public class ConsumoManipulacaoRequest
{
    [Required]
    public Guid ManipulationOrderId { get; set; }

    [Required]
    public List<ConsumoItemRequest> Items { get; set; } = new();
}

public class ConsumoItemRequest
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }
}

public class StockMovementListResponse
{
    public List<StockMovementDto> Movements { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class StockMovementStatsResponse
{
    public int TotalMovements { get; set; }
    public int EntradasCount { get; set; }
    public int SaidasCount { get; set; }
    public int AjustesCount { get; set; }
    public int PerdasCount { get; set; }
    public decimal TotalEntradas { get; set; }
    public decimal TotalSaidas { get; set; }
    public decimal TotalPerdas { get; set; }
    public Dictionary<string, int> MovementsByType { get; set; } = new();
    public Dictionary<string, decimal> ValueByType { get; set; } = new();
}

public class RastreabilidadeResponse
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public List<StockMovementDto> Movements { get; set; } = new();
    public List<ManipulationOrderSummary> ManipulationOrders { get; set; } = new();
}

public class ManipulationOrderSummary
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal QuantityUsed { get; set; }
}