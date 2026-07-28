using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Pharmacy;

[Index(nameof(OrderNumber), nameof(EstablishmentId), IsUnique = true)]
[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
public class ManipulationOrder
{
    [Key]
    public Guid Id { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required, MaxLength(50)]
    public string OrderNumber { get; set; } = default!;

    public Guid? FormulaId { get; set; }
    public Formula? Formula { get; set; }

    // ========== Vínculo com Orçamento ==========
    [Column("PrescriptionQuoteId")]
    public Guid? PrescriptionQuoteId { get; set; }
    // Navegação removida para evitar PrescriptionQuoteId1

    // Prescrição
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    [MaxLength(200)]
    public string? PrescriberName { get; set; }

    [MaxLength(50)]
    public string? PrescriberRegistration { get; set; }

    // Cliente
    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = default!;

    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    // Produção
    [Column(TypeName = "decimal(10,2)")]
    public decimal QuantityToProduce { get; set; }

    [Required, MaxLength(10)]
    public string Unit { get; set; } = default!;

    [MaxLength(2000)]
    public string? SpecialInstructions { get; set; }

    // ========== NOVO: Prioridade ==========
    [Column("Priority")]
    [MaxLength(20)]
    public string Priority { get; set; } = "NORMAL";
    // BAIXA, NORMAL, ALTA, URGENTE

    // Status
    [Required, MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";
    // PENDENTE, AGUARDANDO_PRODUCAO, EM_PRODUCAO, PESAGEM, MISTURA, ENVASE, ROTULAGEM, CONFERENCIA, FINALIZADO, CANCELADO

    // Datas importantes
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }

    // Validade do produto
    public DateTime? ExpiryDate { get; set; }

    // Responsáveis
    public Guid RequestedByEmployeeId { get; set; }
    public Employee? RequestedByEmployee { get; set; }

    public Guid? ManipulatedByEmployeeId { get; set; }
    public Employee? ManipulatedByEmployee { get; set; }

    public Guid? CheckedByEmployeeId { get; set; }
    public Employee? CheckedByEmployee { get; set; }

    public Guid? ApprovedByPharmacistId { get; set; }
    public Employee? ApprovedByPharmacist { get; set; }

    // Controle de qualidade
    [MaxLength(1000)]
    public string? QualityNotes { get; set; }

    public bool PassedQualityControl { get; set; } = false;

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegação
    public ICollection<StockMovement>? StockMovements { get; set; }
    public ICollection<SaleItem>? SaleItems { get; set; }

    // Navegação para Workflow de Manipulação
    public ICollection<ManipulationStep>? Steps { get; set; }
    public ICollection<ManipulationPhoto>? Photos { get; set; }
    public ICollection<ManipulationOrderComponent>? Components { get; set; }
}