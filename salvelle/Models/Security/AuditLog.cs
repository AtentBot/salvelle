using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Security;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("Action")]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Column("EntityType")]
    [StringLength(100)]
    public string? EntityType { get; set; }

    [Column("EntityId")]
    [StringLength(100)]
    public string? EntityId { get; set; }

    [Column("Details")]
    [StringLength(1000)]
    public string? Details { get; set; }

    [Column("UserId")]
    public Guid? UserId { get; set; }

    [Column("UserType")]
    [StringLength(20)]
    public string? UserType { get; set; }

    [Column("IpAddress")]
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Column("EstablishmentId")]
    public Guid? EstablishmentId { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
