using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.ManipulationOrders;

/// <summary>
/// DTO para aprovação final pelo farmacêutico responsável
/// Suporta propriedades em português e inglês para compatibilidade
/// </summary>
public class StartAprovacaoDto
{
    // === TEMPO ===
    public DateTime? StartTime { get; set; }

    // === IDENTIFICAÇÃO DO FARMACÊUTICO ===

    [Required(ErrorMessage = "ID do farmacêutico é obrigatório")]
    public Guid FarmaceuticoId { get; set; }

    public Guid PharmacistEmployeeId
    {
        get => FarmaceuticoId;
        set => FarmaceuticoId = value;
    }

    [Required(ErrorMessage = "Nome do farmacêutico é obrigatório")]
    [MaxLength(200)]
    public string FarmaceuticoNome { get; set; } = default!;

    public string PharmacistName
    {
        get => FarmaceuticoNome;
        set => FarmaceuticoNome = value;
    }

    [Required(ErrorMessage = "CRF é obrigatório")]
    [MaxLength(20)]
    public string CRF { get; set; } = default!;

    public string PharmacistCRF
    {
        get => CRF;
        set => CRF = value;
    }

    // === CHECKLIST DE APROVAÇÃO ===

    public bool InspecaoVisualOk { get; set; }
    public bool VisualInspectionPassed
    {
        get => InspecaoVisualOk;
        set => InspecaoVisualOk = value;
    }

    public bool DocumentacaoCompleta { get; set; }
    public bool DocumentationComplete
    {
        get => DocumentacaoCompleta;
        set => DocumentacaoCompleta = value;
    }

    public bool RotulagemCorreta { get; set; }
    public bool LabelingCorrect
    {
        get => RotulagemCorreta;
        set => RotulagemCorreta = value;
    }

    public bool EmbalagemIntegra { get; set; }
    public bool PackagingIntact
    {
        get => EmbalagemIntegra;
        set => EmbalagemIntegra = value;
    }

    // === RESULTADO ===

    [Required(ErrorMessage = "A decisão de aprovação é obrigatória")]
    public bool Aprovado { get; set; }

    public bool Approved
    {
        get => Aprovado;
        set => Aprovado = value;
    }

    [MaxLength(1000)]
    public string? MotivoReprovacao { get; set; }

    public string? RejectionReason
    {
        get => MotivoReprovacao;
        set => MotivoReprovacao = value;
    }

    [MaxLength(1000)]
    public string? AcoesCorretivas { get; set; }

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    public string? Observations
    {
        get => Observacoes;
        set => Observacoes = value;
    }

    // === ASSINATURA ===

    public string? AssinaturaDigital { get; set; }

    public string? DigitalSignature
    {
        get => AssinaturaDigital;
        set => AssinaturaDigital = value;
    }
}

/// <summary>
/// DTO para workflow dashboard
/// </summary>
public class WorkflowDashboardDto
{
    public int TotalPendentes { get; set; }
    public int TotalEmProducao { get; set; }
    public int TotalFinalizados { get; set; }
    public int TotalRejeitados { get; set; }
    public decimal TempoMedioProducao { get; set; }
    public decimal TaxaAprovacao { get; set; }
    public List<OrdemPorEtapa> OrdensPorEtapa { get; set; } = new();
}

public class OrdemPorEtapa
{
    public string Etapa { get; set; } = default!;
    public int Quantidade { get; set; }
}

/// <summary>
/// DTO resumido para listagem de ordens no workflow
/// </summary>
public class WorkflowOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? FormulaName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public string? CurrentStepType { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public string Priority { get; set; } = "NORMAL";
    public bool IsDelayed { get; set; }
}