using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("PharmaceuticalAnalysisLog")]
public class PharmaceuticalAnalysisLog
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("CustomerFormulaId")]
    public Guid CustomerFormulaId { get; set; }
    
    [ForeignKey("CustomerFormulaId")]
    public CustomerFormula? CustomerFormula { get; set; }

    [Required]
    [Column("PharmacistId")]
    public Guid PharmacistId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("PharmacistName")]
    public string PharmacistName { get; set; } = default!;

    [Required]
    [MaxLength(20)]
    [Column("PharmacistCrf")]
    public string PharmacistCrf { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    [Column("ActionType")]
    public string ActionType { get; set; } = default!;

    [Column("Analysis")]
    public string? Analysis { get; set; }

    [Column("SafetyCheck")]
    public bool SafetyCheck { get; set; } = false;

    [Column("DosageCheck")]
    public bool DosageCheck { get; set; } = false;

    [Column("InteractionCheck")]
    public bool InteractionCheck { get; set; } = false;

    [Column("StabilityCheck")]
    public bool StabilityCheck { get; set; } = false;

    [Column("Observations")]
    public string? Observations { get; set; }

    [Column("InternalNotes")]
    public string? InternalNotes { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
