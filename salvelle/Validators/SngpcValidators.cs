using FluentValidation;
using DTOs;

namespace Validators;

public class RegisterControlledMovementValidator : AbstractValidator<RegisterControlledMovementDto>
{
    public RegisterControlledMovementValidator()
    {
        RuleFor(x => x.RawMaterialId)
            .NotEmpty().WithMessage("Matéria-prima é obrigatória");

        RuleFor(x => x.MovementDate)
            .NotEmpty().WithMessage("Data da movimentação é obrigatória")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser futura");

        RuleFor(x => x.MovementType)
            .Must(x => x == "ENTRADA" || x == "SAIDA" || x == "TRANSFERENCIA" ||
                      x == "PERDA" || x == "DEVOLUCAO" || x == "AJUSTE")
            .WithMessage("Tipo de movimentação inválido");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.PrescriptionNumber)
            .NotEmpty().When(x => x.MovementType == "SAIDA")
            .WithMessage("Número da receita é obrigatório para saídas");

        RuleFor(x => x.DoctorName)
            .NotEmpty().When(x => x.MovementType == "SAIDA")
            .WithMessage("Nome do médico é obrigatório para saídas");

        RuleFor(x => x.DoctorCrm)
            .NotEmpty().When(x => x.MovementType == "SAIDA")
            .WithMessage("CRM é obrigatório para saídas");

        RuleFor(x => x.PatientName)
            .NotEmpty().When(x => x.MovementType == "SAIDA")
            .WithMessage("Nome do paciente é obrigatório para saídas");

        RuleFor(x => x.Reason)
            .NotEmpty().When(x => x.MovementType == "PERDA" || x.MovementType == "AJUSTE")
            .WithMessage("Motivo é obrigatório para perdas e ajustes");
    }
}

public class RegisterSpecialPrescriptionValidator : AbstractValidator<RegisterSpecialPrescriptionDto>
{
    public RegisterSpecialPrescriptionValidator()
    {
        RuleFor(x => x.PrescriptionType)
            .Must(x => x == "AMARELA" || x == "AZUL" || x == "BRANCA_2_VIAS")
            .WithMessage("Tipo de receita inválido");

        RuleFor(x => x.PrescriptionNumber)
            .NotEmpty().WithMessage("Número da receita é obrigatório");

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("Data de emissão é obrigatória")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser futura");

        RuleFor(x => x.DoctorName)
            .NotEmpty().WithMessage("Nome do médico é obrigatório");

        RuleFor(x => x.DoctorCrm)
            .NotEmpty().WithMessage("CRM é obrigatório");

        RuleFor(x => x.DoctorCrmState)
            .NotEmpty().WithMessage("UF do CRM é obrigatória")
            .Length(2).WithMessage("UF deve ter 2 caracteres");

        RuleFor(x => x.PatientName)
            .NotEmpty().WithMessage("Nome do paciente é obrigatório");

        RuleFor(x => x.PatientDocument)
            .NotEmpty().WithMessage("Documento do paciente é obrigatório");

        RuleFor(x => x.Medication)
            .NotEmpty().WithMessage("Medicamento é obrigatório");

        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage("Quantidade é obrigatória");

        RuleFor(x => x.Posology)
            .NotEmpty().WithMessage("Posologia é obrigatória");
    }
}

public class GenerateBalanceValidator : AbstractValidator<GenerateBalanceDto>
{
    public GenerateBalanceValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Data inicial é obrigatória");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Data final é obrigatória")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Data final deve ser maior ou igual à data inicial");

        RuleFor(x => x.BalanceType)
            .Must(x => x == "MENSAL" || x == "TRIMESTRAL" || x == "ANUAL")
            .WithMessage("Tipo de balanço inválido");
    }
}
