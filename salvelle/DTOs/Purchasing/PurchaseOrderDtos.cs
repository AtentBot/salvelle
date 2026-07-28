namespace DTOs.Purchasing;

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }  
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal ShippingValue { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
}

public class CreatePurchaseOrderItemDto
{
    public Guid RawMaterialId { get; set; }  
    public decimal QuantityOrdered { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }  // ? MUDOU DE int PARA Guid
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal ShippingValue { get; set; }
    public string? Notes { get; set; }
}

public class ApprovePurchaseOrderDto
{
    public string? ApprovalNotes { get; set; }
}

public class ReceivePurchaseOrderDto
{
    public DateTime ActualDeliveryDate { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public List<ReceivePurchaseOrderItemDto> Items { get; set; } = new();
}

public class ReceivePurchaseOrderItemDto
{
    public int PurchaseOrderItemId { get; set; }  // ? Mantém int (PurchaseOrderItem.Id é int)
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public string? CertificateOfAnalysis { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderResponseDto
{
    public int Id { get; set; }  // ? Mantém int (PurchaseOrder.Id é int)
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }  // ? MUDOU DE int PARA Guid
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal ShippingValue { get; set; }
    public decimal FinalValue { get; set; }
    public string? Notes { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public string? ApprovedByEmployeeName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderItemResponseDto> Items { get; set; } = new();
}

public class PurchaseOrderItemResponseDto
{
    public int Id { get; set; }  // ? Mantém int (PurchaseOrderItem.Id é int)
    public Guid RawMaterialId { get; set; }  // ? MUDOU DE int PARA Guid
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}