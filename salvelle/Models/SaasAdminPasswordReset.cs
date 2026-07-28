using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Token de recuperação de senha para administradores SaaS
/// </summary>
[Table("saas_admin_password_resets")]
public class SaasAdminPasswordReset
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("saas_admin_id")]
    public Guid SaasAdminId { get; set; }

    /// <summary>
    /// Token único para reset de senha (URL-safe)
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// IP de origem da solicitação
    /// </summary>
    [MaxLength(50)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User Agent de origem da solicitação
    /// </summary>
    [MaxLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Data/hora de expiração do token
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Se o token já foi utilizado
    /// </summary>
    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Quando o token foi utilizado
    /// </summary>
    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegação
    [ForeignKey("SaasAdminId")]
    public virtual SaasAdmin? SaasAdmin { get; set; }
}
