using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Fotos capturadas durante as etapas de manipulação
/// </summary>
[Table("ManipulationPhotos")]
public class ManipulationPhoto
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("ManipulationOrderId")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("ManipulationStepId")]
    public Guid? ManipulationStepId { get; set; }
    public ManipulationStep? ManipulationStep { get; set; }

    [Column("StepType")]
    [MaxLength(50)]
    public string StepType { get; set; } = default!;
    // SEPARACAO, PESAGEM, MISTURA, ENVASE, ROTULAGEM, CONFERENCIA, APROVACAO, EXPEDICAO

    [Column("PhotoUrl")]
    [MaxLength(500)]
    public string PhotoUrl { get; set; } = default!;

    [Column("ThumbnailUrl")]
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Column("FileName")]
    [MaxLength(255)]
    public string? FileName { get; set; }

    [Column("ContentType")]
    [MaxLength(100)]
    public string ContentType { get; set; } = "image/jpeg";

    [Column("FileSize")]
    public long FileSize { get; set; }

    [Column("Description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("CapturedByEmployeeId")]
    public Guid CapturedByEmployeeId { get; set; }
    public Employee? CapturedByEmployee { get; set; }

    [Column("CapturedAt")]
    public DateTime CapturedAt { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; } = false;
}