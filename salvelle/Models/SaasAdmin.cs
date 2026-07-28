using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("saas_admins")]
public class SaasAdmin
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("full_name")]
    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("password_algorithm")]
    [MaxLength(50)]
    public string PasswordAlgorithm { get; set; } = "argon2id-v1";

    [Column("role")]
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "ADMIN"; // SUPER_ADMIN, ADMIN, SUPPORT

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<SaasAdminSession> Sessions { get; set; } = new List<SaasAdminSession>();
}
