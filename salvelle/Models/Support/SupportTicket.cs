using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Support;

[Table("support_tickets")]
public class SupportTicket
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>MANUAL | SYSTEM</summary>
    [Column("origin")]
    [MaxLength(20)]
    public string Origin { get; set; } = "MANUAL";

    /// <summary>WHATSAPP | PAYMENT | SIGNUP | JOB | GENERAL | TECHNICAL</summary>
    [Column("category")]
    [MaxLength(30)]
    public string Category { get; set; } = "GENERAL";

    [Column("establishment_id")]
    public Guid? EstablishmentId { get; set; }

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [Column("title")]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Column("description")]
    public string Description { get; set; } = "";

    /// <summary>LOW | MEDIUM | HIGH | CRITICAL</summary>
    [Column("priority")]
    [MaxLength(10)]
    public string Priority { get; set; } = "MEDIUM";

    /// <summary>OPEN | IN_PROGRESS | RESOLVED | CLOSED</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "OPEN";

    /// <summary>Chave para deduplicação de chamados sistêmicos (ex: "whatsapp_disconnected_pharm")</summary>
    [Column("deduplication_key")]
    [MaxLength(100)]
    public string? DeduplicationKey { get; set; }

    [Column("is_auto_resolvable")]
    public bool IsAutoResolvable { get; set; } = false;

    [Column("assigned_to")]
    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "OPEN" => "Aberto",
        "IN_PROGRESS" => "Em andamento",
        "RESOLVED" => "Resolvido",
        "CLOSED" => "Fechado",
        _ => Status
    };

    [NotMapped]
    public string PriorityDisplay => Priority switch
    {
        "LOW" => "Baixa",
        "MEDIUM" => "Média",
        "HIGH" => "Alta",
        "CRITICAL" => "Crítica",
        _ => Priority
    };

    [NotMapped]
    public string CategoryDisplay => Category switch
    {
        "WHATSAPP" => "WhatsApp",
        "PAYMENT" => "Pagamento",
        "SIGNUP" => "Cadastro",
        "JOB" => "Processo interno",
        "TECHNICAL" => "Técnico",
        "GENERAL" => "Geral",
        _ => Category
    };
}
