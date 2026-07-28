using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Pharmacy;

namespace Models.Purchasing;

public class PurchaseOrderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [Required]
    public Guid RawMaterialId { get; set; }  // ? MUDOU DE int PARA Guid

    [ForeignKey(nameof(RawMaterialId))]
    public virtual RawMaterial? RawMaterial { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityOrdered { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityReceived { get; set; }

    [Required]
    [StringLength(10)]
    public string Unit { get; set; } = string.Empty; // g, kg, mL, L, UN

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "PENDENTE"; // PENDENTE, RECEBIDO_PARCIAL, RECEBIDO_TOTAL, CANCELADO

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BatchReceiving> BatchesReceived { get; set; } = new List<BatchReceiving>();
}