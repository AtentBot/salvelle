using FluentValidation;
using DTOs;

namespace Validators;

public class SignupRequestValidator : AbstractValidator<SignupRequestDto>
{
    public SignupRequestValidator()
    {
        RuleFor(x => x.NomeFantasia)
            .NotEmpty().WithMessage("Nome fantasia é obrigatório")
            .MaximumLength(255).WithMessage("Nome fantasia muito longo");

        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão social é obrigatória")
            .MaximumLength(255).WithMessage("Razão social muito longa");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ é obrigatório")
            .Must(BeValidCnpj).WithMessage("CNPJ inválido");

        RuleFor(x => x.WhatsApp)
            .NotEmpty().WithMessage("WhatsApp é obrigatório")
            .Matches(@"^\d{10,11}$").WithMessage("WhatsApp deve conter 10 ou 11 dígitos");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");

        //RuleFor(x => x.PlanId)
        //    .NotEmpty().WithMessage("Plano é obrigatório");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches(@"[0-9]").WithMessage("Senha deve conter pelo menos um número")
            .Matches(@"[\!\?\*\.\@\#\$\%]").WithMessage("Senha deve conter pelo menos um caractere especial");

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Você deve aceitar os termos de uso");
    }

    private bool BeValidCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return false;

        // Remove caracteres não numéricos
        cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

        if (cnpj.Length != 14) return false;

        // Verifica se todos os dígitos são iguais
        if (cnpj.Distinct().Count() == 1) return false;

        // Validação dos dígitos verificadores
        int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCnpj = cnpj.Substring(0, 12);
        int soma = 0;

        for (int i = 0; i < 12; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        string digito = resto.ToString();
        tempCnpj += digito;
        soma = 0;

        for (int i = 0; i < 13; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cnpj.EndsWith(digito);
    }
}

public class VerifySignupCodeValidator : AbstractValidator<VerifySignupCodeDto>
{
    public VerifySignupCodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Código é obrigatório")
            .Length(6).WithMessage("Código deve ter 6 dígitos")
            .Matches(@"^\d{6}$").WithMessage("Código deve conter apenas números");

        RuleFor(x => x.WhatsApp)
            .NotEmpty().WithMessage("WhatsApp é obrigatório");
    }
}

public class CreatePlanValidator : AbstractValidator<CreatePlanDto>
{
    public CreatePlanValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do plano é obrigatório")
            .MaximumLength(100).WithMessage("Nome muito longo");

        RuleFor(x => x.PriceMonthly)
            .GreaterThan(0).WithMessage("Preço mensal deve ser maior que zero");

        RuleFor(x => x.PriceYearly)
            .GreaterThan(0).WithMessage("Preço anual deve ser maior que zero");

        RuleFor(x => x.MaxEmployees)
            .GreaterThan(0).WithMessage("Limite de funcionários deve ser maior que zero");

        RuleFor(x => x.MaxMonthlyOrders)
            .GreaterThan(0).WithMessage("Limite de ordens deve ser maior que zero");
    }
}
