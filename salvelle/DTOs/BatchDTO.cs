using System.ComponentModel.DataAnnotations;

namespace Models.DTOs;

public class CreateBatchRequest
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
    public decimal ReceivedQuantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitCost { get; set; }

    public DateTime? ReceivedDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    [StringLength(500)]
    public string? QualityNotes { get; set; }
}

public class UpdateBatchRequest
{
    [Range(0, double.MaxValue)]
    public decimal? CurrentQuantity { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public Guid? ApprovedByEmployeeId { get; set; }

    [StringLength(500)]
    public string? QualityNotes { get; set; }
}

public class ApproveBatchRequest
{
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class RejectBatchRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string RejectionReason { get; set; } = string.Empty;
}

public class AdjustBatchQuantityRequest
{
    [Required]
    public decimal QuantityAdjustment { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}

public class BatchResponse
{
    public Guid Id { get; set; }
    public Guid RawMaterialId { get; set; }
    public string? RawMaterialName { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public string? ApprovedByEmployeeName { get; set; }
    public string? QualityNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByEmployeeId { get; set; }
    public string? CreatedByEmployeeName { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool IsNearExpiration { get; set; }
    public decimal UsagePercentage { get; set; }
}

public class BatchListResponse
{
    public List<BatchResponse> Batches { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class BatchStatsResponse
{
    public int TotalBatches { get; set; }
    public int QuarantineBatches { get; set; }
    public int ApprovedBatches { get; set; }
    public int RejectedBatches { get; set; }
    public int ExpiredBatches { get; set; }
    public int NearExpirationBatches { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public Dictionary<string, int> BatchesByStatus { get; set; } = new();
}