using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Security;
using Models.Employees;

namespace Models.Employees;

[Table("job_positions")]
[Index(nameof(EstablishmentId))]
[Index(nameof(Code), IsUnique = true)]
public class JobPosition
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== VÍNCULO COM ESTABELECIMENTO ====================
    [Required]
    public Guid EstablishmentId { get; set; }
    
    [ForeignKey(nameof(EstablishmentId))]
    public Establishment? Establishment { get; set; }

    // ==================== IDENTIFICAÇÃO DO CARGO ====================
    [Required, MaxLength(30)]
    public string Code { get; set; } = default!; // owner, manager, supervisor, employee, user

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!; // Dono, Gerente, Supervisor, Funcionário, Usuário

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Responsibilities { get; set; } // Responsabilidades detalhadas

    // ==================== HIERARQUIA ====================
    [Required]
    public int HierarchyLevel { get; set; } = 1; // 1 = mais baixo, 10 = mais alto

    public Guid? ReportsTo { get; set; } // ID do cargo superior (ex: Funcionário reporta a Supervisor)

    [ForeignKey(nameof(ReportsTo))]
    public JobPosition? SuperiorPosition { get; set; }

    // ==================== REQUISITOS ====================
    [MaxLength(200)]
    public string? RequiredEducation { get; set; } // Ensino Fundamental, Médio, Superior, Pós

    [MaxLength(200)]
    public string? RequiredCertification { get; set; } // CRF, CRC, etc

    public bool RequiresCertification { get; set; } = false;

    [MaxLength(100)]
    public string? RequiredExperience { get; set; } // "2 anos na área"

    // ==================== REMUNERAÇÃO ====================
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SuggestedSalaryMin { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SuggestedSalaryMax { get; set; }

    [MaxLength(50)]
    public string? SalaryType { get; set; } // Mensal, Horário, Comissionado

    // ==================== CONTROLE ====================
    public bool IsActive { get; set; } = true;

    public bool IsSystemDefault { get; set; } = false; // Cargos padrão do sistema não podem ser excluídos

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }

    // ==================== RELACIONAMENTOS ====================
    public ICollection<Employee>? Employees { get; set; }
    
    public ICollection<EmployeeJobHistory>? JobHistories { get; set; }
    
    public ICollection<RolePermission>? RolePermissions { get; set; }
}
