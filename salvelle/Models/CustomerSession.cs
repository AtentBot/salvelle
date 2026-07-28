using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("\"CustomerSessions\"")]
public class CustomerSession
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("CustomerAuthId")]
    public Guid CustomerAuthId { get; set; }

    [Required]
    [Column("CustomerId")]
    public Guid CustomerId { get; set; }

    [Required]
    [Column("SessionToken")]
    [StringLength(128)]
    public string SessionToken { get; set; } = string.Empty;

    [Column("IpAddress")]
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [Column("UserAgent")]
    [StringLength(500)]
    public string? UserAgent { get; set; }

    [Column("DeviceType")]
    [StringLength(50)]
    public string? DeviceType { get; set; }

    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    [Column("LastActivityAt")]
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("LogoutAt")]
    public DateTime? LogoutAt { get; set; }

    // Estabelecimento selecionado na sessão atual
    [Column("CurrentEstablishmentId")]
    public Guid? CurrentEstablishmentId { get; set; }

    // Navigation
    [ForeignKey("CustomerAuthId")]
    public virtual CustomerAuth? CustomerAuth { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("CurrentEstablishmentId")]
    public virtual Establishment? CurrentEstablishment { get; set; }
}
