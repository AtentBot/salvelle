using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Support;

[Table("whatsapp_instance_status")]
public class WhatsAppInstanceStatus
{
    [Key]
    [Column("instance_name")]
    [MaxLength(100)]
    public string InstanceName { get; set; } = "";

    /// <summary>OPEN | CONNECTING | DISCONNECTED | UNKNOWN</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "UNKNOWN";

    [Column("last_checked_at")]
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;

    [Column("disconnected_since")]
    public DateTime? DisconnectedSince { get; set; }

    [Column("last_connected_at")]
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>Chamado sistêmico aberto para este problema, se houver</summary>
    [Column("active_ticket_id")]
    public Guid? ActiveTicketId { get; set; }

    [NotMapped]
    public bool IsHealthy => Status == "OPEN";

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        "OPEN" => "Conectado",
        "CONNECTING" => "Reconectando",
        "DISCONNECTED" => "Desconectado",
        _ => "Desconhecido"
    };
}
