using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Auth;

/// <summary>
/// Token de autenticação de dois fatores
/// RENOMEADO DE: TwoFactorAuth ? TwoFactorToken
/// </summary>
public class TwoFactorToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    // ? CAMPOS COMBINADOS de ambas as classes
    public bool IsUsed { get; set; }  // ? Usado pelo AuthService
    public DateTime? UsedAt { get; set; }  // ? Usado pelo AuthService

    public bool IsVerified { get; set; }  // ? Mantido da versão antiga
    public DateTime? VerifiedAt { get; set; }  // ? Mantido da versão antiga

    [Required]
    [StringLength(50)]
    public string Purpose { get; set; } = string.Empty; // LOGIN, CONTROLLED_SUBSTANCE

    [StringLength(100)]
    public string? IpAddress { get; set; }

    [StringLength(200)]
    public string? UserAgent { get; set; }

    public int Attempts { get; set; }  // ? Mantido da versão antiga

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}