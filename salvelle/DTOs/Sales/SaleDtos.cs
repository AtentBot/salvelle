using System.ComponentModel.DataAnnotations;

namespace DTOs.Sales;

// ===================================================================
// DTOs - CRIAÇÃO DE VENDAS
// ===================================================================

public class CreateSaleDto
{
    public Guid? CustomerId { get; set; }

    [Required]
    public List<SaleItemDto> Items { get; set; } = new();

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "DINHEIRO";

    [Required]
    [Range(0, 999999)]
    public decimal PaidAmount { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0, 999999)]
    public decimal DiscountAmount { get; set; } = 0;

    [MaxLength(1000)]
    public string? Observations { get; set; }
}

public class SaleItemDto
{
    public Guid? ManipulationOrderId { get; set; }
    public Guid? PrescriptionId { get; set; }
    public Guid? FormulaId { get; set; }
    public Guid? ProductId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = default!;

    [Required]
    [Range(0.01, 9999)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, 999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;
}

// ===================================================================
// DTOs - VENDA RÁPIDA (PDV)
// ===================================================================

public class QuickSaleDto
{
    public Guid? CustomerId { get; set; }

    [Required]
    public List<SaleItemDto> Items { get; set; } = new();

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "DINHEIRO";

    [Required]
    [Range(0, 999999)]
    public decimal PaidAmount { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [MaxLength(1000)]
    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - LISTAGEM
// ===================================================================

public class SaleListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string? CustomerName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public string PaymentStatus { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int ItemsCount { get; set; }
}

// ===================================================================
// DTOs - DETALHAMENTO
// ===================================================================

public class SaleResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCpf { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Campos de pagamento (legado - compatibilidade)
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? ChangeAmount { get; set; }
    public DateTime? PaymentDate { get; set; }

    // Múltiplos pagamentos
    public bool HasMultiplePayments { get; set; }

    // Nota Fiscal
    public string? InvoiceNumber { get; set; }
    public string? InvoiceKey { get; set; }
    public string? InvoiceStatus { get; set; }

    // Status
    public string Status { get; set; } = default!;
    public DateTime? CancelledAt { get; set; }
    public string? CancelledByEmployeeName { get; set; }
    public string? CancellationReason { get; set; }
    public string? Observations { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public string? CreatedByEmployeeName { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByEmployeeName { get; set; }

    // Items
    public List<SaleItemResponseDto> Items { get; set; } = new();
}

public class SaleItemResponseDto
{
    public Guid Id { get; set; }
    public Guid? ManipulationOrderId { get; set; }
    public string? ManipulationOrderCode { get; set; }
    public Guid? PrescriptionId { get; set; }
    public string? PrescriptionCode { get; set; }
    public Guid? FormulaId { get; set; }
    public string? FormulaName { get; set; }
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal ProfitMargin { get; set; }
    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - RECIBO
// ===================================================================

public class SaleReceiptDto
{
    public string Code { get; set; } = default!;
    public DateTime SaleDate { get; set; }
    public string? CustomerName { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string EstablishmentName { get; set; } = default!;
    public string EstablishmentAddress { get; set; } = default!;
    public string EstablishmentCnpj { get; set; } = default!;
}

public class ReceiptItemDto
{
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

// ===================================================================
// DTOs - CANCELAMENTO
// ===================================================================

public class CancelSaleDto
{
    [Required]
    [MinLength(10)]
    [MaxLength(500)]
    public string Reason { get; set; } = default!;
}

// ===================================================================
// DTOs - RELATÓRIOS
// ===================================================================

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal AverageTicket { get; set; }
    public Dictionary<string, int> SalesByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> AmountByPaymentMethod { get; set; } = new();
    public List<SaleListDto> Sales { get; set; } = new();
}

public class DailySalesReportDto
{
    public DateTime Date { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal AverageTicket { get; set; }
    public Dictionary<string, int> SalesByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> AmountByPaymentMethod { get; set; } = new();
}

// ===================================================================
// DTOs - ORÇAMENTO
// ===================================================================

public class CreateQuotationDto
{
    public Guid? CustomerId { get; set; }

    [Required]
    public List<QuotationItemDto> Items { get; set; } = new();

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [MaxLength(1000)]
    public string? Observations { get; set; }
}

public class QuotationItemDto
{
    public Guid? ManipulationOrderId { get; set; }
    public Guid? FormulaId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = default!;

    [Required]
    [Range(0.01, 999999)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, 999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;
}