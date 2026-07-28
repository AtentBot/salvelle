using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Pharmacy;

[Index(nameof(BatchNumber), nameof(RawMaterialId), IsUnique = true)]
[Index(nameof(ExpiryDate))]
[Index(nameof(Status))]
public class Batch
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Required]
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    [Required, MaxLength(50)]
    public string BatchNumber { get; set; } = default!;

    [Required, MaxLength(50)]
    public string InvoiceNumber { get; set; } = default!;

    [Column(TypeName = "decimal(18,4)")]
    public decimal ReceivedQuantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentQuantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitCost { get; set; }

    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }

    // Controle de Qualidade
    [Required, MaxLength(20)]
    public string Status { get; set; } = "QUARENTENA";
    // QUARENTENA, APROVADO, REPROVADO, VENCIDO

    [MaxLength(100)]
    public string? CertificateNumber { get; set; }

    public DateTime? ApprovalDate { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }

    [ForeignKey(nameof(ApprovedByEmployeeId))]
    public Employee? ApprovedByEmployee { get; set; }  

    [MaxLength(500)]
    public string? QualityNotes { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByEmployeeId { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public Employee? CreatedByEmployee { get; set; }  

    // Navegação
    public ICollection<StockMovement>? StockMovements { get; set; }
}