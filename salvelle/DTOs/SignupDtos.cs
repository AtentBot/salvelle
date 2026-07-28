namespace DTOs;

public class SignupRequestDto
{
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public string BillingCycle { get; set; } = "MONTHLY";
    public string Password { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class VerifySignupCodeDto
{
    public string Code { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
}

public class SignupResponseDto
{
    public Guid EstablishmentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresVerification { get; set; }
}

public class VerifyCodeResponseDto
{
    public Guid EstablishmentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RedirectTo { get; set; } = string.Empty;
}

public class CompleteSignupDto
{
    public Guid EstablishmentId { get; set; }
    public string StripeSessionId { get; set; } = string.Empty;
}

/// <summary>
/// DTO para completar o perfil do proprietário após verificação do código
/// </summary>
public class CompleteOwnerProfileDto
{
    /// <summary>
    /// ID do estabelecimento (retornado na verificação do código)
    /// </summary>
    public Guid EstablishmentId { get; set; }
    
    /// <summary>
    /// Nome completo do proprietário
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// CPF do proprietário (apenas dígitos ou formatado)
    /// </summary>
    public string Cpf { get; set; } = string.Empty;
    
    /// <summary>
    /// Data de nascimento (opcional)
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }
    
    /// <summary>
    /// Telefone pessoal (opcional, diferente do WhatsApp da empresa)
    /// </summary>
    public string? Phone { get; set; }
}

public class CompleteOwnerProfileResponseDto
{
    public Guid EmployeeId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RedirectTo { get; set; } = string.Empty;
}
