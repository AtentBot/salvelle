using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Pharmacy;

[Index(nameof(MovementDate))]
[Index(nameof(MovementType))]
[Index(nameof(RawMaterialId), nameof(MovementDate))]
public class StockMovement
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required]
    public Guid RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public Guid? BatchId { get; set; }
    public Batch? Batch { get; set; }

    [Required, MaxLength(20)]
    public string MovementType { get; set; } = default!;
    // ENTRADA, SAIDA, AJUSTE, PERDA, VENCIMENTO, MANIPULACAO, VENDA

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal StockBefore { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal StockAfter { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    // Referências
    public Guid? ManipulationOrderId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? SupplierId { get; set; }

    [MaxLength(100)]
    public string? DocumentNumber { get; set; } // NF, Receita, etc

    // Auditoria
    public DateTime MovementDate { get; set; }
    public Guid PerformedByEmployeeId { get; set; }
    public Employee? PerformedByEmployee { get; set; }

    public Guid? AuthorizedByEmployeeId { get; set; }
    public Employee? AuthorizedByEmployee { get; set; }

    public DateTime CreatedAt { get; set; }

    // Para substâncias controladas
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    [MaxLength(50)]
    public string? NotificationNumber { get; set; } // Receita azul/amarela
}