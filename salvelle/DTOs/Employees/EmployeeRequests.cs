using System.ComponentModel.DataAnnotations;

namespace DTOs.Employees;

// ==================== AUTENTICAÇÃO ====================

/// <summary>
/// DTO para login de funcionário
/// </summary>
public class LoginDto
{
    [Required]
    public string Cpf { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

/// <summary>
/// DTO para gerar hash de senha (ferramenta administrativa)
/// </summary>
public class GenerateHashDto
{
    [Required]
    public string Password { get; set; } = "";
}

// ==================== FUNCIONÁRIOS ====================

/// <summary>
/// DTO para criação de novo funcionário
/// </summary>
public record CreateEmployeeRequest
{
    [Required(ErrorMessage = "O ID do estabelecimento é obrigatório")]
    public Guid EstablishmentId { get; init; }

    [Required(ErrorMessage = "O ID do cargo é obrigatório")]
    public Guid JobPositionId { get; init; }

    // Dados Pessoais
    [Required(ErrorMessage = "O nome completo é obrigatório")]
    [MaxLength(200)]
    public string FullName { get; init; } = default!;

    [MaxLength(100)]
    public string? SocialName { get; init; }

    [Required(ErrorMessage = "O CPF é obrigatório")]
    [MaxLength(14)]
    public string Cpf { get; init; } = default!;

    [MaxLength(20)]
    public string? Rg { get; init; }

    [MaxLength(30)]
    public string? RgIssuer { get; init; }

    public DateOnly? RgIssueDate { get; init; }

    [Required(ErrorMessage = "A data de nascimento é obrigatória")]
    public DateOnly DateOfBirth { get; init; }

    [MaxLength(20)]
    public string? Gender { get; init; }

    [MaxLength(100)]
    public string? Nationality { get; init; }

    [MaxLength(100)]
    public string? PlaceOfBirth { get; init; }

    [MaxLength(50)]
    public string? MaritalStatus { get; init; }

    // Documentos Trabalhistas
    [MaxLength(20)]
    public string? Ctps { get; init; }

    [MaxLength(10)]
    public string? CtpsSeries { get; init; }

    [MaxLength(5)]
    public string? CtpsUf { get; init; }

    public DateOnly? CtpsIssueDate { get; init; }

    [MaxLength(20)]
    public string? PisPasep { get; init; }

    [MaxLength(20)]
    public string? VoterRegistration { get; init; }

    [MaxLength(20)]
    public string? MilitaryService { get; init; }

    [MaxLength(20)]
    public string? DriverLicense { get; init; }

    [MaxLength(10)]
    public string? DriverLicenseCategory { get; init; }

    public DateOnly? DriverLicenseExpiry { get; init; }

    // Contatos
    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(20)]
    public string? WhatsApp { get; init; }

    [Required(ErrorMessage = "O e-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [MaxLength(200)]
    public string Email { get; init; } = default!;

    // Endereço
    [Required(ErrorMessage = "O logradouro é obrigatório")]
    [MaxLength(200)]
    public string Street { get; init; } = default!;

    [Required(ErrorMessage = "O número é obrigatório")]
    [MaxLength(20)]
    public string Number { get; init; } = default!;

    [MaxLength(120)]
    public string? Complement { get; init; }

    [Required(ErrorMessage = "O bairro é obrigatório")]
    [MaxLength(120)]
    public string Neighborhood { get; init; } = default!;

    [Required(ErrorMessage = "A cidade é obrigatória")]
    [MaxLength(120)]
    public string City { get; init; } = default!;

    [Required(ErrorMessage = "O estado é obrigatório")]
    [MaxLength(2)]
    public string State { get; init; } = default!;

    [Required(ErrorMessage = "O CEP é obrigatório")]
    [MaxLength(9)]
    public string PostalCode { get; init; } = default!;

    // Dados Trabalhistas
    [Required(ErrorMessage = "A data de admissão é obrigatória")]
    public DateOnly HireDate { get; init; }

    [Required(ErrorMessage = "O tipo de contrato é obrigatório")]
    [MaxLength(50)]
    public string ContractType { get; init; } = "CLT";

    [MaxLength(50)]
    public string? WorkShift { get; init; }

    [Required(ErrorMessage = "O salário é obrigatório")]
    [Range(0.01, 999999.99, ErrorMessage = "Salário inválido")]
    public decimal Salary { get; init; }

    [MaxLength(50)]
    public string? Department { get; init; }

    // Dados Bancários
    [MaxLength(10)]
    public string? BankCode { get; init; }

    [MaxLength(100)]
    public string? BankName { get; init; }

    [MaxLength(20)]
    public string? BankBranch { get; init; }

    [MaxLength(30)]
    public string? BankAccount { get; init; }

    [MaxLength(10)]
    public string? BankAccountType { get; init; }

    [MaxLength(5)]
    public string? BankAccountDigit { get; init; }

    // Contato de Emergência
    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; init; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; init; }

    // Dependentes
    public int DependentsCount { get; init; } = 0;

    // Período de Experiência (padrão: 90 dias)
    public int ProbationDays { get; init; } = 90;

    // Senha para primeiro acesso
    [Required(ErrorMessage = "A senha é obrigatória")]
    public string Password { get; init; } = default!;
}

/// <summary>
/// DTO para atualização de funcionário
/// </summary>
public record UpdateEmployeeRequest
{
    public Guid? JobPositionId { get; init; }

    [MaxLength(200)]
    public string? FullName { get; init; }

    [MaxLength(100)]
    public string? SocialName { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [MaxLength(20)]
    public string? WhatsApp { get; init; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; init; }

    // Endereço
    [MaxLength(200)]
    public string? Street { get; init; }

    [MaxLength(20)]
    public string? Number { get; init; }

    [MaxLength(120)]
    public string? Complement { get; init; }

    [MaxLength(120)]
    public string? Neighborhood { get; init; }

    [MaxLength(120)]
    public string? City { get; init; }

    [MaxLength(2)]
    public string? State { get; init; }

    [MaxLength(9)]
    public string? PostalCode { get; init; }

    // Dados Bancários
    [MaxLength(10)]
    public string? BankCode { get; init; }

    [MaxLength(100)]
    public string? BankName { get; init; }

    [MaxLength(20)]
    public string? BankBranch { get; init; }

    [MaxLength(30)]
    public string? BankAccount { get; init; }

    [MaxLength(10)]
    public string? BankAccountType { get; init; }

    [MaxLength(5)]
    public string? BankAccountDigit { get; init; }

    public decimal? Salary { get; init; }

    [MaxLength(50)]
    public string? Department { get; init; }

    [MaxLength(30)]
    public string? Status { get; init; }

    // Contato de Emergência
    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(50)]
    public string? EmergencyContactRelationship { get; init; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; init; }

    public int? DependentsCount { get; init; }
}

/// <summary>
/// Classe para compatibilidade com controller existente
/// </summary>
public class CreateEmployeeDto
{
    public Guid EstablishmentId { get; set; }
    public Guid JobPositionId { get; set; }
    public string FullName { get; set; } = "";
    public string? SocialName { get; set; }
    public string Cpf { get; set; } = "";
    public string? Rg { get; set; }
    public string? RgIssuer { get; set; }
    public DateOnly? RgIssueDate { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Ctps { get; set; }
    public string? CtpsSeries { get; set; }
    public string? CtpsUf { get; set; }
    public DateOnly? CtpsIssueDate { get; set; }
    public string? PisPasep { get; set; }
    public string? VoterRegistration { get; set; }
    public string? MilitaryService { get; set; }
    public string? DriverLicense { get; set; }
    public string? DriverLicenseCategory { get; set; }
    public DateOnly? DriverLicenseExpiry { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string Email { get; set; } = "";
    public string Street { get; set; } = "";
    public string Number { get; set; } = "";
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public DateOnly HireDate { get; set; }
    public string? ContractType { get; set; }
    public string? WorkShift { get; set; }
    public decimal Salary { get; set; }
    public string? Department { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccount { get; set; }
    public string? BankAccountType { get; set; }
    public string? BankAccountDigit { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public int DependentsCount { get; set; } = 0;
    public int? ProbationDays { get; set; }
    public string Password { get; set; } = "";
}

/// <summary>
/// Classe para compatibilidade com controller existente
/// </summary>
public class UpdateEmployeeDto
{
    // Mudança de cargo
    public Guid? JobPositionId { get; set; }

    // Dados Pessoais
    public string? FullName { get; set; }
    public string? SocialName { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }

    // Contatos
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }

    // Endereço
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }

    // Dados Bancários
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccount { get; set; }
    public string? BankAccountType { get; set; }
    public string? BankAccountDigit { get; set; }

    // Dados Trabalhistas
    public decimal? Salary { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
    public string? StatusNotes { get; set; }
    public DateOnly? TerminationDate { get; set; }

    // Contato de Emergência
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Outros
    public int? DependentsCount { get; set; }

    // Para mudança de cargo (histórico)
    public string? ChangeReason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO para resposta com dados do funcionário
/// </summary>
public record EmployeeResponse
{
    public Guid Id { get; init; }
    public Guid EstablishmentId { get; init; }
    public string EstablishmentName { get; init; } = default!;
    public Guid JobPositionId { get; init; }
    public string JobPositionName { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string? SocialName { get; init; }
    public string Cpf { get; init; } = default!;
    public string? Phone { get; init; }
    public string? WhatsApp { get; init; }
    public string Email { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateOnly HireDate { get; init; }
    public DateOnly? TerminationDate { get; init; }
    public decimal Salary { get; init; }
    public bool IsInProbation { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

// ==================== CARGOS ====================

/// <summary>
/// DTO para criar cargo
/// </summary>
public class CreateJobPositionDto
{
    [Required]
    public Guid EstablishmentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int HierarchyLevel { get; set; }
    public bool RequiresCertification { get; set; }

    [MaxLength(100)]
    public string? RequiredCertification { get; set; }

    [MaxLength(100)]
    public string? RequiredEducation { get; set; }

    [MaxLength(1000)]
    public string? Responsibilities { get; set; }

    public decimal? SuggestedSalaryMin { get; set; }
    public decimal? SuggestedSalaryMax { get; set; }

    [MaxLength(20)]
    public string? SalaryType { get; set; }
}

/// <summary>
/// DTO para atualizar cargo
/// </summary>
public class UpdateJobPositionDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool? RequiresCertification { get; set; }

    [MaxLength(100)]
    public string? RequiredCertification { get; set; }

    [MaxLength(100)]
    public string? RequiredEducation { get; set; }

    [MaxLength(1000)]
    public string? Responsibilities { get; set; }

    public decimal? SuggestedSalaryMin { get; set; }
    public decimal? SuggestedSalaryMax { get; set; }
    public bool? IsActive { get; set; }
}

// ==================== OPERAÇÕES DE RH ====================

/// <summary>
/// DTO para mudança de cargo
/// </summary>
public record ChangeJobPositionRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    public Guid NewJobPositionId { get; init; }

    [Required]
    public DateOnly EffectiveDate { get; init; }

    [MaxLength(50)]
    public string? ChangeReason { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public decimal? NewSalary { get; init; }
}

/// <summary>
/// DTO para demissão de funcionário
/// </summary>
public record TerminateEmployeeRequest
{
    [Required]
    public Guid EmployeeId { get; init; }

    [Required]
    public DateOnly TerminationDate { get; init; }

    [Required]
    [MaxLength(50)]
    public string TerminationType { get; init; } = default!;
    // ComJustaCausa, SemJustaCausa, Pedido, Acordo, FimContrato

    [MaxLength(1000)]
    public string? TerminationReason { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public bool RevokeAccessImmediately { get; init; } = true;
}

// ==================== DTOs AUXILIARES PARA CONTROLLER ====================

/// <summary>
/// DTO para alteração de senha de funcionário
/// Use este DTO ou DTOs.Auth.ChangePasswordDto dependendo do contexto
/// </summary>
public class ChangeEmployeePasswordDto
{
    public string? CurrentPassword { get; set; }
    public string NewPassword { get; set; } = null!;
    public bool IsAdminReset { get; set; }
}

/// <summary>
/// DTO para desativar funcionário
/// </summary>
public class DeactivateEmployeeDto
{
    public string? Reason { get; set; }
    public DateOnly? TerminationDate { get; set; }
}

// =======================================================================
// NOTA IMPORTANTE: 
// Se DTOs.Auth.ChangePasswordDto não tiver IsAdminReset, use ChangeEmployeePasswordDto
// =======================================================================
public class UpdateMeDto
{
    public string? FullName { get; set; }
    public string? SocialName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
}

public class ChangeMyPasswordDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
