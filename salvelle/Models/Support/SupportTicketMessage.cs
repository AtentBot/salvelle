using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Support;

[Table("support_ticket_messages")]
public class SupportTicketMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [ForeignKey("TicketId")]
    public virtual SupportTicket Ticket { get; set; } = null!;

    /// <summary>PHARMACY | ADMIN | SYSTEM</summary>
    [Column("author_type")]
    [MaxLength(20)]
    public string AuthorType { get; set; } = "PHARMACY";

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("author_name")]
    [MaxLength(100)]
    public string AuthorName { get; set; } = "";

    [Column("body")]
    public string Body { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
