using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Pharmacy;
using Models.Employees;

namespace Models.Purchasing;

public class BatchReceiving
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseOrderItemId { get; set; }  // ? Mantém int (PurchaseOrderItem.Id é int)

    [ForeignKey(nameof(PurchaseOrderItemId))]
    public virtual PurchaseOrderItem? PurchaseOrderItem { get; set; }

    [Required]
    public Guid BatchId { get; set; }  // ? MUDOU DE int PARA Guid

    [ForeignKey(nameof(BatchId))]
    public virtual Batch? Batch { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityReceived { get; set; }

    [Required]
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public Guid ReceivedByEmployeeId { get; set; }  // ? MUDOU DE int PARA Guid

    [ForeignKey(nameof(ReceivedByEmployeeId))]
    public virtual Employee? ReceivedByEmployee { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}