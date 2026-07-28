using FluentValidation;
using DTOs;

namespace Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Cpf)
            .Must(BeValidCpf).When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF inválido");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email inválido");

        RuleFor(x => x.Phone)
            .Matches(@"^\d{10,11}$").When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Telefone deve conter 10 ou 11 dígitos");

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\d{10,11}$").When(x => !string.IsNullOrWhiteSpace(x.WhatsApp))
            .WithMessage("WhatsApp deve conter 10 ou 11 dígitos");

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Today).When(x => x.BirthDate.HasValue)
            .WithMessage("Data de nascimento deve ser anterior a hoje");

        RuleFor(x => x.Gender)
            .Must(x => x == null || x == "M" || x == "F" || x == "O")
            .WithMessage("Gênero deve ser M, F ou O");

        RuleFor(x => x.ZipCode)
            .Matches(@"^\d{8}$").When(x => !string.IsNullOrWhiteSpace(x.ZipCode))
            .WithMessage("CEP deve conter 8 dígitos");

        RuleFor(x => x.State)
            .Length(2).When(x => !string.IsNullOrWhiteSpace(x.State))
            .WithMessage("Estado deve ter 2 caracteres (UF)");

        RuleFor(x => x.ConsentDataProcessing)
            .Equal(true).WithMessage("Consentimento LGPD é obrigatório");
    }

    private bool BeValidCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return true;

        cpf = cpf.Replace(".", "").Replace("-", "").Trim();

        if (cpf.Length != 11) return false;
        if (cpf.Distinct().Count() == 1) return false;

        int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = cpf.Substring(0, 9);
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        string digito = resto.ToString();
        tempCpf += digito;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;
        digito += resto.ToString();

        return cpf.EndsWith(digito);
    }
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email inválido");

        RuleFor(x => x.Status)
            .Must(x => x == "ATIVO" || x == "INATIVO" || x == "BLOQUEADO")
            .WithMessage("Status deve ser ATIVO, INATIVO ou BLOQUEADO");
    }
}

public class BlockCustomerValidator : AbstractValidator<BlockCustomerDto>
{
    public BlockCustomerValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Motivo do bloqueio é obrigatório")
            .MinimumLength(10).WithMessage("Motivo deve ter no mínimo 10 caracteres");
    }
}