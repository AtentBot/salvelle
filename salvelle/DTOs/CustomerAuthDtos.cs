namespace DTOs.Cliente;

// ==================== LOGIN ====================

/// <summary>
/// Login simples: telefone + senha (sem verificação de código)
/// </summary>
public class CustomerLoginDto
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CustomerLoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SessionToken { get; set; }
    public CustomerInfoDto? Customer { get; set; }
}

public class CustomerInfoDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsVerified { get; set; }
}

// ==================== REGISTRO ====================

public class CustomerRegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BirthDate { get; set; }
    public string? Gender { get; set; }
    
    // Endereço
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    
    // LGPD
    public bool ConsentDataProcessing { get; set; }
}

public class CustomerRegisterResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public bool RequiresVerification { get; set; }
}

// ==================== VERIFICAÇÃO ====================

public class CustomerVerifyCodeDto
{
    public string Phone { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class CustomerVerifyResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SessionToken { get; set; }
    public CustomerInfoDto? Customer { get; set; }
}

public class CustomerResendCodeDto
{
    public string Phone { get; set; } = string.Empty;
}

// ==================== SENHA ====================

public class CustomerSetPasswordDto
{
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CustomerResetPasswordDto
{
    public string Phone { get; set; } = string.Empty;
}

public class CustomerResetPasswordConfirmDto
{
    public string Phone { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ==================== ESTABELECIMENTO ====================

public class SelectEstablishmentDto
{
    public Guid EstablishmentId { get; set; }
}

public class EstablishmentListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Neighborhood { get; set; }
    public bool AcceptsOnlineOrders { get; set; }
    public decimal? Rating { get; set; }
}

// ==================== SESSÃO ====================

public class CustomerSessionInfoDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid? CurrentEstablishmentId { get; set; }
    public string? CurrentEstablishmentName { get; set; }
    public bool IsVerified { get; set; }
}
