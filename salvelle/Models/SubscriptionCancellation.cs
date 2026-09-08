using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Histórico de cancelamento de assinatura. Registra o questionário (razões + comentário)
/// preenchido pelo cliente ao cancelar. Cobrança é por período fechado — sem reembolso;
/// o acesso segue até <see cref="PeriodEnd"/> e o Stripe fica com CancelAtPeriodEnd=true.
/// </summary>
[Table("subscription_cancellations")]
public class SubscriptionCancellation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    /// <summary>Razões selecionadas (códigos separados por vírgula). Opcional.</summary>
    [Column("reasons")]
    [MaxLength(500)]
    public string? Reasons { get; set; }

    /// <summary>Comentário livre do cliente (textbox). Opcional.</summary>
    [Column("comment")]
    [MaxLength(4000)]
    public string? Comment { get; set; }

    /// <summary>Employee que solicitou o cancelamento (se identificável na sessão).</summary>
    [Column("canceled_by_employee_id")]
    public Guid? CanceledByEmployeeId { get; set; }

    /// <summary>Fim do período pago — acesso mantido até aqui (período fechado, sem reembolso).</summary>
    [Column("period_end")]
    public DateTime? PeriodEnd { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
