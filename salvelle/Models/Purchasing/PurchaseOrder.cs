using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Pharmacy;
using Models.Employees;

namespace Models.Purchasing;

public class PurchaseOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public Guid SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }

    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpectedDeliveryDate { get; set; }

    public DateTime? ActualDeliveryDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "RASCUNHO"; // RASCUNHO, APROVADO, ENVIADO, RECEBIDO_PARCIAL, RECEBIDO_TOTAL, CANCELADO

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalValue { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? SupplierInvoiceNumber { get; set; }

    public Guid? ApprovedByEmployeeId { get; set; }

    [ForeignKey(nameof(ApprovedByEmployeeId))]
    public virtual Employee? ApprovedByEmployee { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid CreatedByEmployeeId { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public virtual Employee? CreatedByEmployee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedByEmployeeId { get; set; }

    [ForeignKey(nameof(UpdatedByEmployeeId))]
    public virtual Employee? UpdatedByEmployee { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
