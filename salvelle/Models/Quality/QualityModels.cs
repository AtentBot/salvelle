using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Quality;

/// <summary>
/// Procedimento Operacional Padrão (POP)
/// Tabela: pops
/// </summary>
[Table("pops")]
public class POP
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = "";

    [Column("sequence_number")]
    public int SequenceNumber { get; set; }

    [Column("title")]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Column("category")]
    [MaxLength(50)]
    public string Category { get; set; } = "";

    [Column("objective")]
    public string? Objective { get; set; }

    [Column("scope")]
    public string? Scope { get; set; }

    [Column("definitions")]
    public string? Definitions { get; set; }

    [Column("responsibilities")]
    public string? Responsibilities { get; set; }

    [Column("procedures")]
    public string Procedures { get; set; } = "";

    [Column("references")]
    public string? References { get; set; }

    [Column("records")]
    public string? Records { get; set; }

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "RASCUNHO"; // RASCUNHO, REVISAO, VIGENTE, OBSOLETO

    [Column("effective_date")]
    public DateTime? EffectiveDate { get; set; }

    [Column("review_date")]
    public DateTime? ReviewDate { get; set; }

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("approved_by_employee_id")]
    public Guid? ApprovedByEmployeeId { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
}

/// <summary>
/// Ação Corretiva e Preventiva (CAPA)
/// Tabela: capas
/// </summary>
[Table("capas")]
public class CAPA
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = "";

    [Column("sequence_number")]
    public int SequenceNumber { get; set; }

    [Column("type")]
    [MaxLength(20)]
    public string Type { get; set; } = "CORRETIVA"; // CORRETIVA, PREVENTIVA

    [Column("title")]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Column("description")]
    public string Description { get; set; } = "";

    [Column("source")]
    [MaxLength(50)]
    public string? Source { get; set; } // AUDITORIA, RECLAMACAO, DESVIO, INDICADOR, OUTRO

    [Column("root_cause_analysis")]
    public string? RootCauseAnalysis { get; set; }

    [Column("proposed_actions")]
    public string? ProposedActions { get; set; }

    [Column("implemented_actions")]
    public string? ImplementedActions { get; set; }

    [Column("verification_results")]
    public string? VerificationResults { get; set; }

    [Column("effectiveness_evaluation")]
    public string? EffectivenessEvaluation { get; set; }

    [Column("priority")]
    [MaxLength(10)]
    public string Priority { get; set; } = "MEDIA"; // ALTA, MEDIA, BAIXA

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ABERTA"; // ABERTA, EM_ANDAMENTO, VERIFICACAO, CONCLUIDA, CANCELADA

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("responsible_employee_id")]
    public Guid? ResponsibleEmployeeId { get; set; }

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
}

/// <summary>
/// Não Conformidade (para futuras implementações)
/// Tabela: non_conformities
/// </summary>
[Table("non_conformities")]
public class NonConformity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = "";

    [Column("title")]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Column("description")]
    public string Description { get; set; } = "";

    [Column("type")]
    [MaxLength(30)]
    public string Type { get; set; } = ""; // PRODUTO, PROCESSO, SISTEMA, FORNECEDOR

    [Column("severity")]
    [MaxLength(10)]
    public string Severity { get; set; } = "MENOR"; // CRITICA, MAIOR, MENOR

    [Column("detected_at")]
    public DateTime DetectedAt { get; set; }

    [Column("detected_by_employee_id")]
    public Guid? DetectedByEmployeeId { get; set; }

    [Column("related_batch_id")]
    public Guid? RelatedBatchId { get; set; }

    [Column("related_order_id")]
    public Guid? RelatedOrderId { get; set; }

    [Column("capa_id")]
    public Guid? CAPAId { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ABERTA";

    [Column("disposition")]
    [MaxLength(30)]
    public string? Disposition { get; set; } // APROVADO, REPROCESSADO, REJEITADO, DESTRUIDO

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("CAPAId")]
    public virtual CAPA? CAPA { get; set; }
}
