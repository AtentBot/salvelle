using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.ManipulationOrders;

// ===================================================================
// DTOs - PESAGEM
// ===================================================================

public class StartPesagemDto
{
    public List<ComponentPesadoDto> Components { get; set; } = new();
    public string? ScaleId { get; set; }
    public string? ScaleName { get; set; }
    public decimal? EnvironmentTemperature { get; set; }
    public decimal? EnvironmentHumidity { get; set; }
    public DateTime? StartTime { get; set; }
    public string? Observations { get; set; }
}

public class ComponentPesadoDto
{
    [Required]
    public Guid RawMaterialId { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    [Required]
    public decimal ExpectedWeight { get; set; }

    [Required]
    public decimal ActualWeight { get; set; }

    [Required]
    public string Unit { get; set; } = default!;

    public string? BatchNumber { get; set; }
}

public class CheckPesagemDto
{
    [Required]
    public bool Passed { get; set; }

    [Required]
    public Guid CheckedByEmployeeId { get; set; }

    public string? CheckNotes { get; set; }
}

// ===================================================================
// DTOs - MISTURA
// ===================================================================

public class StartMisturaDto
{
    [Required]
    [MaxLength(100)]
    public string MixingMethod { get; set; } = default!;

    [MaxLength(100)]
    public string? Equipment { get; set; }

    public int? MixingDuration { get; set; }

    public string? MixingSpeed { get; set; }

    public DateTime? StartTime { get; set; }

    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - ENVASE
// ===================================================================

public class StartEnvaseDto
{
    [Required]
    [MaxLength(100)]
    public string PackagingType { get; set; } = default!;

    [Required]
    public decimal PackagedQuantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string BatchNumber { get; set; } = default!;

    [Required]
    public DateTime ManufacturingDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    public DateTime? StartTime { get; set; }

    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - ROTULAGEM
// ===================================================================

public class StartRotulagemDto
{
    [Required]
    [MaxLength(50)]
    public string BatchNumber { get; set; } = default!;

    [Required]
    public DateTime ManufacturingDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [MaxLength(500)]
    public string? AdditionalInfo { get; set; }

    [MaxLength(100)]
    public string? Barcode { get; set; }

    public DateTime? StartTime { get; set; }

    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - CONFERÊNCIA
// ===================================================================

public class StartConferenciaDto
{
    [Required]
    [MaxLength(500)]
    public string VisualAspects { get; set; } = default!;

    [Required]
    public bool PackagingIntact { get; set; }

    [Required]
    public bool LabelCorrect { get; set; }

    [Required]
    public bool QuantityCorrect { get; set; }

    [Required]
    public bool DocumentationComplete { get; set; }

    [Required]
    public bool ApprovedByPharmacist { get; set; }

    public Guid? PharmacistEmployeeId { get; set; }

    [MaxLength(200)]
    public string? PharmacistName { get; set; }

    [MaxLength(20)]
    public string? PharmacistCRF { get; set; }

    public DateTime? StartTime { get; set; }

    public string? Observations { get; set; }
}

// ===================================================================
// DTOs - FOTOS
// ===================================================================

public class AddPhotoDto
{
    [Required]
    [MaxLength(500)]
    public string PhotoUrl { get; set; } = default!;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }
}

public class PhotoDto
{
    public Guid Id { get; set; }
    public string StepType { get; set; } = default!;
    public string PhotoUrl { get; set; } = default!;
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
    public string CapturedByEmployeeName { get; set; } = default!;
    public DateTime CapturedAt { get; set; }
}

// ===================================================================
// DTOs - RESPONSE
// ===================================================================

public class ManipulationStepDto
{
    public Guid Id { get; set; }
    public Guid ManipulationOrderId { get; set; }
    public string StepType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public Guid? PerformedByEmployeeId { get; set; }
    public string? PerformedByEmployeeName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StepData { get; set; }
    public string? Observations { get; set; }
    public bool? PassedIntermediateCheck { get; set; }
    public Guid? CheckedByEmployeeId { get; set; }
    public string? CheckedByEmployeeName { get; set; }
    public string? CheckNotes { get; set; }
    public DateTime? CheckedAt { get; set; }
    public List<PhotoDto> Photos { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; } 
}