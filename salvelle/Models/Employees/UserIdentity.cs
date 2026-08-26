using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Employees;

/// <summary>
/// Identidade de login de uma pessoa (multi-tenant). Guarda a credencial UMA vez
/// (senha, 2FA, lockout) e é vinculada a N estabelecimentos via <see cref="Employee"/>
/// (que passa a funcionar como membership). Chave de login global = CPF.
/// </summary>
[Table("identities")]
[Index(nameof(Cpf), IsUnique = true)]
public class UserIdentity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>CPF (somente dígitos) — identificador de login global da pessoa.</summary>
    [Required, MaxLength(14)]
    public string Cpf { get; set; } = default!;

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(200), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? FullName { get; set; }

    // ==================== CREDENCIAL (única por pessoa) ====================
    [Required]
    public string PasswordHash { get; set; } = default!;

    public DateTime PasswordCreatedAt { get; set; }

    [MaxLength(50)]
    public string PasswordAlgorithm { get; set; } = "argon2id-v1";

    public bool RequirePasswordChange { get; set; } = false;

    // ==================== 2FA / LOCKOUT (na identidade) ====================
    public bool TwoFactorEnabled { get; set; } = false;

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ==================== NAVEGAÇÃO ====================
    /// <summary>Vínculos (memberships) desta identidade com estabelecimentos.</summary>
    public ICollection<Employee> Memberships { get; set; } = new List<Employee>();
}
