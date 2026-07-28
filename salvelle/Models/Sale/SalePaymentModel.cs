using Models.Employees;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("sale_payments")]
public class SalePayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    public Guid SaleId { get; set; }

    [ForeignKey("SaleId")]
    public virtual Sale Sale { get; set; } = null!;

    [Required]
    [Column("payment_method")]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;
    // DINHEIRO, CARTAO_DEBITO, CARTAO_CREDITO, PIX, BOLETO

    [Required]
    [Column("amount")]
    public decimal Amount { get; set; }

    // ==================== CAMPOS PARA DINHEIRO ====================
    [Column("cash_received")]
    public decimal? CashReceived { get; set; }

    [Column("change_amount")]
    public decimal? ChangeAmount { get; set; }

    // ==================== CAMPOS PARA CARTÃO ====================
    [Column("card_brand")]
    [MaxLength(50)]
    public string? CardBrand { get; set; } // VISA, MASTER, ELO, etc

    [Column("card_last_digits")]
    [MaxLength(4)]
    public string? CardLastDigits { get; set; }

    [Column("installments")]
    public int? Installments { get; set; } = 1;

    [Column("nsu")]
    [MaxLength(100)]
    public string? Nsu { get; set; }

    [Column("authorization_code")]
    [MaxLength(100)]
    public string? AuthorizationCode { get; set; }

    // ==================== CAMPOS PARA PIX ====================
    [Column("pix_key")]
    [MaxLength(200)]
    public string? PixKey { get; set; }

    [Column("pix_transaction_id")]
    [MaxLength(200)]
    public string? PixTransactionId { get; set; }

    [Column("pix_qr_code")]
    [MaxLength(1000)]
    public string? PixQrCode { get; set; }

    // ==================== CAMPOS PARA BOLETO ====================
    [Column("boleto_barcode")]
    [MaxLength(100)]
    public string? BoletoBarcode { get; set; }

    [Column("boleto_due_date")]
    public DateTime? BoletoDueDate { get; set; }

    [Column("boleto_number")]
    [MaxLength(100)]
    public string? BoletoNumber { get; set; }

    // ==================== CAMPOS PARA GATEWAY (FUTURO) ====================
    [Column("gateway_provider")]
    [MaxLength(50)]
    public string? GatewayProvider { get; set; } // MERCADOPAGO, PAGSEGURO, STRIPE

    [Column("gateway_transaction_id")]
    [MaxLength(200)]
    public string? GatewayTransactionId { get; set; }

    [Column("gateway_status")]
    [MaxLength(50)]
    public string? GatewayStatus { get; set; } // PENDING, APPROVED, CANCELLED, REFUNDED

    [Column("gateway_response")]
    [MaxLength(2000)]
    public string? GatewayResponse { get; set; }

    // ==================== CONTROLE ====================
    [Required]
    [Column("payment_status")]
    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "APPROVED";
    // PENDING, APPROVED, CANCELLED, REFUNDED

    [Column("payment_date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("processed_by_employee_id")]
    public Guid ProcessedByEmployeeId { get; set; }

    [ForeignKey("ProcessedByEmployeeId")]
    public virtual Employee ProcessedByEmployee { get; set; } = null!;

    [Column("observations")]
    [MaxLength(500)]
    public string? Observations { get; set; }

    // ==================== AUDITORIA ====================
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}