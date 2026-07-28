using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Employees;

[Index(nameof(EmployeeId))]
[Index(nameof(JobPositionId))]
[Index(nameof(StartDate))]
public class EmployeeJobHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== FUNCIONÁRIO ====================
    [Required]
    public Guid EmployeeId { get; set; }
    
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    // ==================== CARGO ====================
    [Required]
    public Guid JobPositionId { get; set; }
    
    [ForeignKey(nameof(JobPositionId))]
    public JobPosition? JobPosition { get; set; }

    // ==================== PERÍODO ====================
    [Required]
    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; } // Null = cargo atual

    public bool IsCurrent { get; set; } = true;

    // ==================== MOTIVO DA MUDANÇA ====================
    [MaxLength(50)]
    public string? ChangeReason { get; set; } // Promoção, Transferência, Rebaixamento, Reestruturação

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // ==================== SALÁRIO NA ÉPOCA ====================
    [Column(TypeName = "decimal(10,2)")]
    public decimal SalaryAtTime { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PreviousSalary { get; set; }

    // ==================== APROVAÇÃO ====================
    public Guid? ApprovedBy { get; set; } // Employee que aprovou a mudança

    public DateTime? ApprovedAt { get; set; }

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? CreatedBy { get; set; }
}
