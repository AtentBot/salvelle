using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Employees;

[Index(nameof(Token), IsUnique = true)]
[Index(nameof(EmployeeId))]
[Index(nameof(ExpiresAt))]
[Index(nameof(IsActive))]
public class EmployeeSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== FUNCIONÁRIO ====================
    [Required]
    public Guid EmployeeId { get; set; }
    
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    // ==================== TOKEN ====================
    [Required, MaxLength(128)]
    public string Token { get; set; } = default!;

    // ==================== VALIDADE ====================
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(100)]
    public string? RevocationReason { get; set; }

    public bool IsActive { get; set; } = true;

    // ==================== INFORMAÇÕES DE CONTEXTO ====================
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet

    [MaxLength(100)]
    public string? Browser { get; set; }

    [MaxLength(100)]
    public string? OperatingSystem { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; } // Cidade/Estado aproximado

    // ==================== SEGURANÇA ====================
    public bool RequiresTwoFactor { get; set; } = false;

    public bool TwoFactorVerified { get; set; } = false;

    public int AccessCount { get; set; } = 0; // Quantas requisições foram feitas com este token

    [MaxLength(100)]
    public string? SessionName { get; set; } // "Chrome no Windows - SP" para identificação

    // ==================== REFRESH TOKEN (OPCIONAL) ====================
    [MaxLength(128)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }
}
