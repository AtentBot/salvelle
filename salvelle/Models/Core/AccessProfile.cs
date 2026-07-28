using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Security;

namespace Models.Core;

[Index(nameof(Code), IsUnique = true)]
[Index(nameof(AccessLevelId))]
public class AccessProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== VÍNCULO COM ACCESS LEVEL BASE ====================
    [Required]
    public Guid AccessLevelId { get; set; }
    
    [ForeignKey(nameof(AccessLevelId))]
    public AccessLevel? AccessLevel { get; set; }

    // ==================== IDENTIFICAÇÃO ====================
    [Required, MaxLength(30)]
    public string Code { get; set; } = default!; // owner, manager, supervisor, employee, user

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    // ==================== HIERARQUIA ====================
    [Required]
    public int HierarchyLevel { get; set; } = 1; 
    // 10 = Owner (máximo)
    // 8 = Manager
    // 6 = Supervisor
    // 4 = Employee
    // 2 = User
    // 1 = Guest

    // ==================== LIMITES E RESTRIÇÕES ====================
    public bool CanManageEmployees { get; set; } = false;
    
    public bool CanManageFinances { get; set; } = false;
    
    public bool CanManageInventory { get; set; } = false;
    
    public bool CanManageReports { get; set; } = false;
    
    public bool CanManageSettings { get; set; } = false;

    public bool CanApproveOrders { get; set; } = false;
    
    public bool CanDeleteRecords { get; set; } = false;
    
    public bool CanExportData { get; set; } = false;

    // Limites operacionais
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxTransactionValue { get; set; } // Valor máximo de transação

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxDiscountPercent { get; set; } // % máximo de desconto

    public int? MaxDailyTransactions { get; set; }

    // ==================== CONFIGURAÇÕES DE SESSÃO ====================
    public int SessionDurationMinutes { get; set; } = 720; // 12 horas padrão

    public int MaxConcurrentSessions { get; set; } = 3;

    public bool RequireTwoFactor { get; set; } = false;

    // ==================== CONTROLE ====================
    public bool IsActive { get; set; } = true;

    public bool IsSystemDefault { get; set; } = false; // Perfis padrão não podem ser excluídos

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }

    // ==================== RELACIONAMENTOS ====================
    public ICollection<Permission>? Permissions { get; set; }
}
