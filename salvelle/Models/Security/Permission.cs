using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Security;

[Index(nameof(ResourceAction), IsUnique = true)]
[Index(nameof(Resource))]
[Index(nameof(Category))]
public class Permission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== IDENTIFICAÇÃO ====================
    [Required, MaxLength(100)]
    public string ResourceAction { get; set; } = default!; 
    // Formato: "resource.action" 
    // Ex: "employees.create", "sales.read", "inventory.update", "reports.delete"

    [Required, MaxLength(50)]
    public string Resource { get; set; } = default!; 
    // employees, sales, inventory, reports, settings, etc

    [Required, MaxLength(20)]
    public string Action { get; set; } = default!; 
    // create, read, update, delete, approve, export, manage

    // ==================== CATEGORIZAÇÃO ====================
    [Required, MaxLength(50)]
    public string Category { get; set; } = default!; 
    // HR, Sales, Inventory, Finance, Reports, Settings, Security

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = default!; 
    // "Criar Funcionários", "Visualizar Vendas", etc

    [MaxLength(300)]
    public string? Description { get; set; }

    // ==================== ESCOPO ====================
    [Required, MaxLength(20)]
    public string Scope { get; set; } = "Own"; 
    // Own (apenas seus registros)
    // Team (sua equipe)
    // Establishment (todo estabelecimento)
    // Global (todos estabelecimentos - para superadmin)

    // ==================== CLASSIFICAÇÃO ====================
    [Required, MaxLength(20)]
    public string RiskLevel { get; set; } = "Low"; 
    // Low, Medium, High, Critical
    // Critical = deletar dados, alterar configurações críticas
    // High = aprovar transações, gerenciar funcionários
    // Medium = criar/editar registros
    // Low = visualizar dados

    public bool RequiresApproval { get; set; } = false; // Requer aprovação de superior

    public bool RequiresTwoFactor { get; set; } = false; // Requer 2FA para executar

    public bool AuditLog { get; set; } = true; // Registrar em log de auditoria

    // ==================== DEPENDÊNCIAS ====================
    [MaxLength(500)]
    public string? DependsOn { get; set; } 
    // Lista de permissões necessárias (separadas por vírgula)
    // Ex: "employees.read" é necessário para "employees.update"

    // ==================== CONTROLE ====================
    public bool IsActive { get; set; } = true;

    public bool IsSystemPermission { get; set; } = true; // Permissões do sistema não podem ser excluídas

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ==================== RELACIONAMENTOS ====================
    public ICollection<RolePermission>? RolePermissions { get; set; }
}
