using FluentValidation;
using DTOs;

namespace Validators;

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescriptionDto>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Cliente é obrigatório");

        RuleFor(x => x.PrescriptionDate)
            .NotEmpty().WithMessage("Data da prescrição é obrigatória")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser futura");

        RuleFor(x => x.DoctorName)
            .NotEmpty().WithMessage("Nome do médico é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.DoctorCrm)
            .NotEmpty().WithMessage("CRM é obrigatório")
            .Matches(@"^\d{4,7}$").WithMessage("CRM deve conter entre 4 e 7 dígitos");

        RuleFor(x => x.DoctorCrmState)
            .NotEmpty().WithMessage("UF do CRM é obrigatória")
            .Length(2).WithMessage("UF deve ter 2 caracteres");

        RuleFor(x => x.PrescriptionType)
            .Must(x => x == "COMUM" || x == "CONTROLE_ESPECIAL" || x == "ANTIBIOTICO")
            .WithMessage("Tipo deve ser COMUM, CONTROLE_ESPECIAL ou ANTIBIOTICO");

        RuleFor(x => x.ControlledType)
            .Must(x => x == null ||
                      x == "A1" || x == "A2" || x == "A3" ||
                      x == "B1" || x == "B2" ||
                      x == "C1" || x == "C2" || x == "C3" || x == "C4" || x == "C5")
            .When(x => x.PrescriptionType == "CONTROLE_ESPECIAL")
            .WithMessage("Lista de controle inválida");

        RuleFor(x => x.PrescriptionColor)
            .Must(x => x == null || x == "BRANCA" || x == "AMARELA" || x == "AZUL")
            .WithMessage("Cor deve ser BRANCA, AMARELA ou AZUL");

        RuleFor(x => x.Medications)
            .NotEmpty().WithMessage("Medicamentos são obrigatórios")
            .MinimumLength(10).WithMessage("Descrição dos medicamentos muito curta");

        RuleFor(x => x.Posology)
            .NotEmpty().WithMessage("Posologia é obrigatória")
            .MinimumLength(10).WithMessage("Posologia muito curta");
    }
}

public class UpdatePrescriptionValidator : AbstractValidator<UpdatePrescriptionDto>
{
    public UpdatePrescriptionValidator()
    {
        RuleFor(x => x.PrescriptionDate)
            .NotEmpty().WithMessage("Data da prescrição é obrigatória")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser futura");

        RuleFor(x => x.DoctorName)
            .NotEmpty().WithMessage("Nome do médico é obrigatório");

        RuleFor(x => x.DoctorCrm)
            .NotEmpty().WithMessage("CRM é obrigatório");

        RuleFor(x => x.Medications)
            .NotEmpty().WithMessage("Medicamentos são obrigatórios");

        RuleFor(x => x.Posology)
            .NotEmpty().WithMessage("Posologia é obrigatória");
    }
}

public class ValidatePrescriptionValidator : AbstractValidator<ValidatePrescriptionDto>
{
    public ValidatePrescriptionValidator()
    {
        RuleFor(x => x.ValidationNotes)
            .NotEmpty().When(x => !x.IsValid)
            .WithMessage("Motivo da rejeição é obrigatório");
    }
}

public class CancelPrescriptionValidator : AbstractValidator<CancelPrescriptionDto>
{
    public CancelPrescriptionValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Motivo do cancelamento é obrigatório")
            .MinimumLength(10).WithMessage("Motivo deve ter no mínimo 10 caracteres");
    }
}

public class GenerateManipulationValidator : AbstractValidator<GenerateManipulationFromPrescriptionDto>
{
    public GenerateManipulationValidator()
    {
        RuleFor(x => x.FormulaId)
            .NotEmpty().WithMessage("Fórmula é obrigatória");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.ExpectedDate)
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser no passado");
    }
}