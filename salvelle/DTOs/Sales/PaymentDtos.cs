using System.ComponentModel.DataAnnotations;

namespace DTOs.Payments;

// ===================================================================
// DTOs - CRIAÇÃO DE PAGAMENTO
// ===================================================================

public class CreatePaymentDto
{
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;
    // DINHEIRO, CARTAO_DEBITO, CARTAO_CREDITO, PIX, BOLETO

    [Required]
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    // ==================== DINHEIRO ====================
    [Range(0.01, 999999)]
    public decimal? CashReceived { get; set; }

    // ==================== CARTÃO ====================
    [MaxLength(50)]
    public string? CardBrand { get; set; } // VISA, MASTER, ELO, AMEX

    [MaxLength(4)]
    [RegularExpression(@"^\d{4}$")]
    public string? CardLastDigits { get; set; }

    [Range(1, 24)]
    public int? Installments { get; set; }

    [MaxLength(100)]
    public string? Nsu { get; set; }

    [MaxLength(100)]
    public string? AuthorizationCode { get; set; }

    // ==================== PIX ====================
    [MaxLength(200)]
    public string? PixKey { get; set; }

    [MaxLength(200)]
    public string? PixTransactionId { get; set; }

    // ==================== BOLETO ====================
    [MaxLength(100)]
    public string? BoletoBarcode { get; set; }

    public DateTime? BoletoDueDate { get; set; }

    // ==================== OBSERVAÇÕES ====================
    [MaxLength(500)]
    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - RESPOSTA DE PAGAMENTO
// ===================================================================

public class PaymentDto
{
    public Guid Id { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }

    // Dinheiro
    public decimal? CashReceived { get; set; }
    public decimal? ChangeAmount { get; set; }

    // Cartão
    public string? CardBrand { get; set; }
    public string? CardLastDigits { get; set; }
    public int? Installments { get; set; }
    public string? Nsu { get; set; }
    public string? AuthorizationCode { get; set; }

    // PIX
    public string? PixKey { get; set; }
    public string? PixTransactionId { get; set; }

    // Boleto
    public string? BoletoBarcode { get; set; }
    public DateTime? BoletoDueDate { get; set; }
    public string? BoletoNumber { get; set; }

    // Gateway (futuro)
    public string? GatewayProvider { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? GatewayStatus { get; set; }

    // Auditoria
    public string ProcessedByEmployeeName { get; set; } = string.Empty;
    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - VENDA COM PAGAMENTOS
// ===================================================================

public class SaleWithPaymentsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<PaymentDto> Payments { get; set; } = new();
    public string CreatedByEmployeeName { get; set; } = string.Empty;
}

// ===================================================================
// DTOs - FLUXO DE CAIXA
// ===================================================================

public class DailyCashFlowDto
{
    public DateTime Date { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalDebitCard { get; set; }
    public decimal TotalCreditCard { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalBoleto { get; set; }
    public decimal GrandTotal { get; set; }
    public int TotalSales { get; set; }
    public List<PaymentMethodSummary> PaymentMethodBreakdown { get; set; } = new();
}

public class PaymentMethodSummary
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
}