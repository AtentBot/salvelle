namespace DTOs.ManipulationSteps;

public class WeighingStepResponseDto
{
    public Guid StepId { get; set; }
    public string Status { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ComponentWeighingResultDto> ComponentsWeighed { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? Observations { get; set; }
}

public class ComponentWeighingResultDto
{
    public Guid ComponentId { get; set; }
    public string ComponentName { get; set; } = default!;
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = default!;
    public decimal PlannedQuantity { get; set; }
    public decimal WeighedQuantity { get; set; }
    public decimal DeviationPercentage { get; set; }
    public bool IsWithinTolerance { get; set; }
    public string? PhotoUrl { get; set; }
}
