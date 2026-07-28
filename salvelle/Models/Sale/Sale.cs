using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models;

[Table("sales")]
public class Sale
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Column("sale_date")]
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    // ==================== VALORES ====================
    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("discount_percentage")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    // ==================== PAGAMENTO (LEGADO - COMPATIBILIDADE) ====================
    [Column("payment_method")]
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("payment_status")]
    [MaxLength(50)]
    public string? PaymentStatus { get; set; }

    [Column("paid_amount")]
    public decimal? PaidAmount { get; set; }

    [Column("change_amount")]
    public decimal? ChangeAmount { get; set; }

    [Column("payment_date")]
    public DateTime? PaymentDate { get; set; }

    // ==================== MÚLTIPLOS PAGAMENTOS (NOVO) ====================
    [Column("has_multiple_payments")]
    public bool HasMultiplePayments { get; set; } = false;

    // ==================== NOTA FISCAL ====================
    [Column("invoice_number")]
    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [Column("invoice_key")]
    [MaxLength(44)]
    public string? InvoiceKey { get; set; }

    [Column("invoice_status")]
    [MaxLength(50)]
    public string? InvoiceStatus { get; set; }

    // ==================== STATUS ====================
    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "FINALIZADA";
    // ORCAMENTO, PENDENTE, PAGAMENTO_PARCIAL, FINALIZADA, CANCELADA

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("cancelled_by_employee_id")]
    public Guid? CancelledByEmployeeId { get; set; }

    [ForeignKey("CancelledByEmployeeId")]
    public virtual Employee? CancelledByEmployee { get; set; }

    [Column("cancellation_reason")]
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    [Column("observations")]
    [MaxLength(1000)]
    public string? Observations { get; set; }

    // ==================== AUDITORIA ====================
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [ForeignKey("CreatedByEmployeeId")]
    public virtual Employee CreatedByEmployee { get; set; } = null!;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    [ForeignKey("UpdatedByEmployeeId")]
    public virtual Employee? UpdatedByEmployee { get; set; }

    // ==================== RELACIONAMENTOS ====================
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    public virtual ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
}