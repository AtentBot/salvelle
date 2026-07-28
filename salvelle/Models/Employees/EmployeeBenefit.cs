using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Employees;

[Index(nameof(EmployeeId))]
[Index(nameof(BenefitType))]
[Index(nameof(IsActive))]
public class EmployeeBenefit
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== FUNCIONÁRIO ====================
    [Required]
    public Guid EmployeeId { get; set; }
    
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    // ==================== TIPO DE BENEFÍCIO ====================
    [Required, MaxLength(50)]
    public string BenefitType { get; set; } = default!; 
    // ValeTransporte, ValeRefeicao, ValeAlimentacao, PlanoSaude, PlanoOdontologico, 
    // SeguroVida, AuxilioCreche, AuxilioEducacao, Outro

    [Required, MaxLength(150)]
    public string BenefitName { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    // ==================== VALOR ====================
    [Column(TypeName = "decimal(10,2)")]
    public decimal MonthlyValue { get; set; } // Valor mensal do benefício

    [Column(TypeName = "decimal(10,2)")]
    public decimal? EmployeeContribution { get; set; } // Quanto o funcionário contribui

    [Column(TypeName = "decimal(10,2)")]
    public decimal? EmployerContribution { get; set; } // Quanto a empresa contribui

    // ==================== VIGÊNCIA ====================
    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    // ==================== FORNECEDOR ====================
    [MaxLength(200)]
    public string? ProviderName { get; set; } // Nome da operadora/fornecedor

    [MaxLength(100)]
    public string? ContractNumber { get; set; }

    [MaxLength(100)]
    public string? CardNumber { get; set; } // Número do cartão (se aplicável)

    // ==================== DESCONTOS ====================
    public bool DeductFromSalary { get; set; } = false;

    [MaxLength(50)]
    public string? DeductionType { get; set; } // Percentual, ValorFixo

    // ==================== DEPENDENTES ====================
    public int DependentsIncluded { get; set; } = 0;

    [MaxLength(500)]
    public string? DependentsNames { get; set; }

    // ==================== NOTAS ====================
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }
}
