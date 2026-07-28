using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;


namespace Models.Employees;

[Index(nameof(Cpf), IsUnique = true)]
[Index(nameof(EstablishmentId))]
[Index(nameof(JobPositionId))]
[Index(nameof(Status))]
public class Employee
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== VÍNCULO COM ESTABELECIMENTO ====================
    [Required]
    public Guid EstablishmentId { get; set; }
    
    [ForeignKey(nameof(EstablishmentId))]
    public Establishment? Establishment { get; set; }

    // ==================== CARGO ATUAL ====================
    [Required]
    public Guid JobPositionId { get; set; }
    
    [ForeignKey(nameof(JobPositionId))]
    public JobPosition? JobPosition { get; set; }

    // ==================== DADOS PESSOAIS ====================
    [Required, MaxLength(200)]
    public string FullName { get; set; } = default!;

    [MaxLength(100)]
    public string? SocialName { get; set; } // Nome social (LGPD+)

    // CPF apenas dígitos (11 caracteres) - Opcional para signup inicial
    [MaxLength(11)]
    public string? Cpf { get; set; }

    [MaxLength(20)]
    public string? Rg { get; set; }

    [MaxLength(30)]
    public string? RgIssuer { get; set; } // Órgão emissor

    public DateOnly? RgIssueDate { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; } // Masculino, Feminino, Outro, Prefiro não informar

    [MaxLength(50)]
    public string? Nationality { get; set; } = "Brasileira";

    [MaxLength(100)]
    public string? PlaceOfBirth { get; set; } // Naturalidade

    [MaxLength(50)]
    public string? MaritalStatus { get; set; } // Solteiro, Casado, Divorciado, Viúvo, União Estável

    // ==================== DOCUMENTOS TRABALHISTAS ====================
    [MaxLength(20)]
    public string? Ctps { get; set; } // Carteira de Trabalho

    [MaxLength(10)]
    public string? CtpsSeries { get; set; }

    [MaxLength(5)]
    public string? CtpsUf { get; set; }

    public DateOnly? CtpsIssueDate { get; set; }

    [MaxLength(20)]
    public string? PisPasep { get; set; }

    [MaxLength(50)]
    public string? VoterRegistration { get; set; } // Título de eleitor

    [MaxLength(50)]
    public string? MilitaryService { get; set; } // Certificado de reservista

    [MaxLength(50)]
    public string? DriverLicense { get; set; } // CNH

    [MaxLength(20)]
    public string? DriverLicenseCategory { get; set; }

    public DateOnly? DriverLicenseExpiry { get; set; }

    // ==================== CONTATOS ====================
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = default!;

    // ==================== ENDEREÇO (Opcional para signup inicial) ====================
    [MaxLength(200)]
    public string? Street { get; set; }

    [MaxLength(20)]
    public string? Number { get; set; }

    [MaxLength(120)]
    public string? Complement { get; set; }

    [MaxLength(120)]
    public string? Neighborhood { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; }

    [MaxLength(8)]
    public string? PostalCode { get; set; } // CEP apenas dígitos

    // ==================== DADOS TRABALHISTAS ====================
    public DateOnly? HireDate { get; set; } // Data de admissão - Opcional para proprietário

    public DateOnly? TerminationDate { get; set; } // Data de demissão

    [MaxLength(50)]
    public string ContractType { get; set; } = "Proprietário"; // CLT, PJ, Estagiário, Temporário, Proprietário

    [MaxLength(50)]
    public string? WorkShift { get; set; } // Diurno, Noturno, Misto

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Salary { get; set; } // Salário atual - Opcional para proprietário

    [MaxLength(50)]
    public string? Department { get; set; } // Departamento

    // ==================== DADOS BANCÁRIOS ====================
    [MaxLength(10)]
    public string? BankCode { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(20)]
    public string? BankBranch { get; set; }

    [MaxLength(30)]
    public string? BankAccount { get; set; }

    [MaxLength(10)]
    public string? BankAccountType { get; set; } // Corrente, Poupança

    [MaxLength(20)]
    public string? BankAccountDigit { get; set; }

    // ==================== CONTATO DE EMERGÊNCIA ====================
    [MaxLength(200)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; set; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    // ==================== DEPENDENTES ====================
    public int DependentsCount { get; set; } = 0;

    // ==================== STATUS E CONTROLE ====================
    [Required, MaxLength(30)]
    public string Status { get; set; } = "Ativo"; // Ativo, Demitido, Afastado, Férias, Experiência

    [MaxLength(500)]
    public string? StatusNotes { get; set; }

    public DateOnly? ProbationEndDate { get; set; } // Fim do período de experiência (45 ou 90 dias)

    // ==================== SEGURANÇA E ACESSO ====================
    [Column("crm")]
    [MaxLength(20)]
    public string? Crm { get; set; }

    [Column("crm_state")]
    [MaxLength(2)]
    public string? CrmState { get; set; }

    [Required]
    public string PasswordHash { get; set; } = default!;

    public DateTime PasswordCreatedAt { get; set; }
    
    public DateTime? PasswordLastChanged { get; set; }

    [Required, MaxLength(40)]
    public string PasswordAlgorithm { get; set; } = "argon2id-v1";

    public bool RequirePasswordChange { get; set; } = false; // Proprietário não precisa trocar senha

    public bool TwoFactorEnabled { get; set; } = false;

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; } // Employee ou Establishment que criou

    public Guid? UpdatedBy { get; set; }

    // ==================== RELACIONAMENTOS ====================
    public ICollection<EmployeeJobHistory>? JobHistory { get; set; }
    
    public ICollection<EmployeeSession>? Sessions { get; set; }
    
    public ICollection<EmployeeBenefit>? Benefits { get; set; }
    
    public ICollection<EmployeeDocument>? Documents { get; set; }

    public ICollection<StockMovement>? StockMovements { get; set; }
}
