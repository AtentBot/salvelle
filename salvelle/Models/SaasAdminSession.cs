using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("saas_admin_sessions")]
public class SaasAdminSession
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("saas_admin_id")]
    [Required]
    public Guid SaasAdminId { get; set; }

    [Column("token")]
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("expires_at")]
    [Required]
    public DateTime ExpiresAt { get; set; }

    [Column("last_activity_at")]
    public DateTime LastActivityAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("SaasAdminId")]
    public SaasAdmin? SaasAdmin { get; set; }
}
