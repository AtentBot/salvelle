using System.ComponentModel.DataAnnotations;

namespace DTOs.ManipulationSteps;

public class WeighingStepRequestDto
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MinLength(1)]
    public List<ComponentWeighingDto> Components { get; set; } = new();

    [MaxLength(2000)]
    public string? Observations { get; set; }
}

public class ComponentWeighingDto
{
    [Required]
    public Guid ComponentId { get; set; }

    [Required]
    public Guid BatchId { get; set; }

    [Required]
    [Range(0.001, 999999)]
    public decimal WeighedQuantity { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public string? PhotoBase64 { get; set; }
}
