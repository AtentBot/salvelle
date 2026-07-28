using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(FormulaId), nameof(RawMaterialId), IsUnique = true)]
public class FormulaComponent
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FormulaId { get; set; }
    public Formula? Formula { get; set; }

    [Required]
    public Guid RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    [Required, MaxLength(10)]
    public string Unit { get; set; } = default!;

    [Required, MaxLength(20)]
    public string ComponentType { get; set; } = "ATIVO";
    // ATIVO, EXCIPIENTE, VEICULO, CONSERVANTE

    public int OrderIndex { get; set; } // Ordem de adição

    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }

    public bool IsOptional { get; set; } = false;
}