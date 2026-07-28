using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Etapa do workflow de manipulação
/// </summary>
[Table("ManipulationSteps")]
public class ManipulationStep
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("ManipulationOrderId")]
    public Guid ManipulationOrderId { get; set; }
    public ManipulationOrder? ManipulationOrder { get; set; }

    [Column("StepType")]
    [MaxLength(50)]
    public string StepType { get; set; } = default!;
    // SEPARACAO, PESAGEM, MISTURA, ENVASE, ROTULAGEM, CONFERENCIA, APROVACAO, EXPEDICAO

    [Column("StepNumber")]
    public int StepNumber { get; set; }

    [Column("Status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";
    // PENDENTE, EM_ANDAMENTO, CONCLUIDA, CANCELADA

    [Column("StartedAt")]
    public DateTime? StartedAt { get; set; }

    [Column("CompletedAt")]
    public DateTime? CompletedAt { get; set; }

    [Column("PerformedByEmployeeId")]
    public Guid? PerformedByEmployeeId { get; set; }
    public Employee? PerformedByEmployee { get; set; }

    [Column("CheckedByEmployeeId")]
    public Guid? CheckedByEmployeeId { get; set; }
    public Employee? CheckedByEmployee { get; set; }

    [Column("CheckedAt")]
    public DateTime? CheckedAt { get; set; }

    [Column("CheckNotes")]
    [MaxLength(1000)]
    public string? CheckNotes { get; set; }

    [Column("PassedIntermediateCheck")]
    public bool? PassedIntermediateCheck { get; set; }

    [Column("Observations")]
    [MaxLength(1000)]
    public string? Observations { get; set; }

    /// <summary>
    /// JSON com dados específicos da etapa (ex: PesagemStepData, MisturaStepData)
    /// </summary>
    [Column("StepData")]
    public string? StepData { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Navegação para fotos desta etapa
    public ICollection<ManipulationPhoto>? Photos { get; set; }
}