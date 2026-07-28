using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models;

[Table("pending_two_factor_sessions")]
public class PendingTwoFactorSession
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Token opaco retornado ao cliente após login que requer 2FA.
    /// Substitui o X-Temp-Identifier header (que era controlado pelo cliente).
    /// </summary>
    [Column("temp_token")]
    [Required]
    [MaxLength(128)]
    public string TempToken { get; set; } = string.Empty;

    [Column("employee_id")]
    [Required]
    public Guid EmployeeId { get; set; }

    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }
}
