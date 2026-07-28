using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("\"CustomerAuths\"")]
public class CustomerAuth
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("CustomerId")]
    public Guid CustomerId { get; set; }

    [Required]
    [Column("Phone")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Column("Email")]
    [StringLength(200)]
    public string? Email { get; set; }

    [Column("Cpf")]
    [StringLength(11)]
    public string? Cpf { get; set; }

    [Column("PasswordHash")]
    [StringLength(500)]
    public string? PasswordHash { get; set; }

    [Column("PasswordAlgorithm")]
    [StringLength(50)]
    public string PasswordAlgorithm { get; set; } = "Argon2id";

    [Column("PasswordCreatedAt")]
    public DateTime? PasswordCreatedAt { get; set; }

    [Column("IsVerified")]
    public bool IsVerified { get; set; } = false;

    [Column("VerificationCode")]
    [StringLength(64)]
    public string? VerificationCode { get; set; }

    [Column("VerificationCodeExpiresAt")]
    public DateTime? VerificationCodeExpiresAt { get; set; }

    [Column("VerificationAttempts")]
    public int VerificationAttempts { get; set; } = 0;

    [Column("LastVerificationSentAt")]
    public DateTime? LastVerificationSentAt { get; set; }

    [Column("FailedLoginAttempts")]
    public int FailedLoginAttempts { get; set; } = 0;

    [Column("LockoutEnd")]
    public DateTime? LockoutEnd { get; set; }

    [Column("LastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [Column("LastLoginIp")]
    [StringLength(45)]
    public string? LastLoginIp { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public virtual ICollection<CustomerSession> Sessions { get; set; } = new List<CustomerSession>();
}
