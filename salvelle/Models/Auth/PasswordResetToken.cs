using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Auth;

/// <summary>
/// Token de recupera��o de senha para funcion�rios (via WhatsApp/SMS)
/// </summary>
[Table("password_reset_tokens")]
public class PasswordResetToken
{
    [Key]
    [Column("Id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("EmployeeId")]
    public Guid EmployeeId { get; set; }

    [Required]
    [Column("Token")]
    [StringLength(100)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [Column("Code")]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("ExpiresAt")]
    public DateTime ExpiresAt { get; set; }

    [Column("IsUsed")]
    public bool IsUsed { get; set; }

    [Column("UsedAt")]
    public DateTime? UsedAt { get; set; }

    [Required]
    [Column("Type")]
    [StringLength(20)]
    public string Type { get; set; } = "WHATSAPP";

    [Column("Attempts")]
    public int Attempts { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegacao
    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }
}