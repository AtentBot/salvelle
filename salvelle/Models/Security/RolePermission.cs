using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Employees;

namespace Models.Security;

[Index(nameof(JobPositionId), nameof(PermissionId), IsUnique = true)]
[Index(nameof(EstablishmentId))]
public class RolePermission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== CARGO ====================
    [Required]
    public Guid JobPositionId { get; set; }
    
    [ForeignKey(nameof(JobPositionId))]
    public JobPosition? JobPosition { get; set; }

    // ==================== PERMISSÃO ====================
    [Required]
    public Guid PermissionId { get; set; }
    
    [ForeignKey(nameof(PermissionId))]
    public Permission? Permission { get; set; }

    // ==================== ESTABELECIMENTO ====================
    [Required]
    public Guid EstablishmentId { get; set; }
    
    [ForeignKey(nameof(EstablishmentId))]
    public Establishment? Establishment { get; set; }

    // ==================== CUSTOMIZAÇÃO ====================
    public bool IsGranted { get; set; } = true; // Permitir ou negar explicitamente

    [MaxLength(500)]
    public string? CustomConditions { get; set; } // Condições específicas (JSON)
    // Ex: {"max_value": 1000, "only_own_records": true}

    // ==================== TEMPORÁRIO ====================
    public DateTime? GrantedFrom { get; set; } // Permissão válida a partir de

    public DateTime? GrantedUntil { get; set; } // Permissão válida até (permissão temporária)

    public bool IsPermanent { get; set; } = true;

    // ==================== APROVAÇÃO ====================
    public Guid? GrantedBy { get; set; } // Quem concedeu a permissão

    public DateTime? GrantedAt { get; set; }

    [MaxLength(500)]
    public string? GrantReason { get; set; }

    // ==================== CONTROLE ====================
    public bool IsActive { get; set; } = true;

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }
}
