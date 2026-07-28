using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(EstablishmentId), nameof(Code), IsUnique = true)]
[Index(nameof(Name))]
[Index(nameof(Category))]
public class Formula
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = default!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, MaxLength(50)]
    public string Category { get; set; } = default!;
    // CAPSULA, POMADA, CREME, GEL, SOLUCAO, SUSPENSAO, XAROPE

    [Required, MaxLength(50)]
    public string PharmaceuticalForm { get; set; } = default!;

    // Rendimento e validade
    [Column(TypeName = "decimal(10,2)")]
    public decimal StandardYield { get; set; } // Quantidade padrão produzida

    public int ShelfLifeDays { get; set; } // Prazo de validade em dias

    // Instruções
    [MaxLength(2000)]
    public string? PreparationInstructions { get; set; }

    [MaxLength(1000)]
    public string? StorageInstructions { get; set; }

    [MaxLength(1000)]
    public string? UsageInstructions { get; set; }

    // Controle
    public bool RequiresSpecialControl { get; set; } = false;
    public bool RequiresPrescription { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Versionamento
    public int Version { get; set; } = 1;
    public Guid? PreviousVersionId { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByEmployeeId { get; set; }
    public Guid? UpdatedByEmployeeId { get; set; }
    public Guid? ApprovedByPharmacistId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navegação
    public ICollection<FormulaComponent>? Components { get; set; }
    public ICollection<ManipulationOrder>? ManipulationOrders { get; set; }
}