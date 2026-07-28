using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Table("RawMaterialsCatalog")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(DcbCode), IsUnique = false)]
[Index(nameof(Category))]
[Index(nameof(AllowedUsage))]
[Index(nameof(IsActive))]
[Index(nameof(Popularity))]
public class RawMaterialCatalog
{
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = default!;

    [MaxLength(50)]
    public string? DcbCode { get; set; }

    [MaxLength(50)]
    public string? CasNumber { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [Required, MaxLength(20)]
    public string ControlType { get; set; } = "COMUM";

    [Required, MaxLength(20)]
    public string AllowedUsage { get; set; } = "BOTH";

    [Required, MaxLength(20)]
    public string PhysicalState { get; set; } = "SOLID";

    [Required, MaxLength(10)]
    public string Unit { get; set; } = "g";

    [Column(TypeName = "decimal(10,4)")]
    public decimal DefaultPurityFactor { get; set; } = 1.0m;

    [Column(TypeName = "decimal(6,4)")]
    public decimal DefaultCorrectionFactor { get; set; } = 1.0m;

    public string? Synonyms { get; set; }

    public string? Indications { get; set; }

    public int Popularity { get; set; } = 50;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
