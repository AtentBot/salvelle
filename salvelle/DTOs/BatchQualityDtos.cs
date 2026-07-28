namespace DTOs.BatchQuality;

public class ApproveBatchDto
{
    public string? CertificateNumber { get; set; }
    public string? QualityNotes { get; set; }
}

public class RejectBatchDto
{
    public string Reason { get; set; } = string.Empty;
    public string? QualityNotes { get; set; }
}

public class BatchQualityResponseDto
{
    public Guid Id { get; set; }
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
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
    public string? ApprovedByEmployeeName { get; set; }
    public string? QualityNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public int DaysUntilExpiry { get; set; }
}

public class QuarantineSummaryDto
{
    public int TotalBatches { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int ExpiringIn60Days { get; set; }
    public decimal TotalValue { get; set; }
    public List<BatchQualityResponseDto> Batches { get; set; } = new();
}