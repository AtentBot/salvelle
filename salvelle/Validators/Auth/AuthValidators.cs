using FluentValidation;
using DTOs.Auth;

namespace Validators.Auth;

public class EmployeeLoginValidator : AbstractValidator<EmployeeLoginDto>
{
    public EmployeeLoginValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("CPF ou WhatsApp é obrigatório");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Senha é obrigatória")
            .MinimumLength(6)
            .WithMessage("Senha deve ter no mínimo 6 caracteres");
    }
}

public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetDto>
{
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("CPF ou WhatsApp é obrigatório");

        RuleFor(x => x.Method)
            .NotEmpty()
            .WithMessage("Método é obrigatório")
            .Must(m => new[] { "WHATSAPP", "EMAIL" }.Contains(m.ToUpper()))
            .WithMessage("Método deve ser WHATSAPP ou EMAIL");
    }
}

public class VerifyResetCodeValidator : AbstractValidator<VerifyResetCodeDto>
{
    public VerifyResetCodeValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("CPF ou WhatsApp é obrigatório");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Código é obrigatório")
            .Length(6)
            .WithMessage("Código deve ter 6 dígitos")
            .Matches("^[0-9]{6}$")
            .WithMessage("Código deve conter apenas números");
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("CPF ou WhatsApp é obrigatório");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Código é obrigatório")
            .Length(6)
            .WithMessage("Código deve ter 6 dígitos");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Nova senha é obrigatória")
            .MinimumLength(8)
            .WithMessage("Senha deve ter no mínimo 8 caracteres")
            .Matches("[A-Z]")
            .WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches("[a-z]")
            .WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches("[0-9]")
            .WithMessage("Senha deve conter pelo menos um número");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Senhas não conferem");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Senha atual é obrigatória");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Nova senha é obrigatória")
            .MinimumLength(8)
            .WithMessage("Senha deve ter no mínimo 8 caracteres")
            .Matches("[A-Z]")
            .WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches("[a-z]")
            .WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches("[0-9]")
            .WithMessage("Senha deve conter pelo menos um número")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Nova senha deve ser diferente da atual");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Senhas não conferem");
    }
}

public class Verify2FAValidator : AbstractValidator<Verify2FADto>
{
    public Verify2FAValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Código é obrigatório")
            .Length(6)
            .WithMessage("Código deve ter 6 dígitos")
            .Matches("^[0-9]{6}$")
            .WithMessage("Código deve conter apenas números");

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .WithMessage("Propósito é obrigatório");
    }
}
