using FluentValidation;
using DTOs.BatchQuality;

namespace Validators.BatchQuality;

public class ApproveBatchValidator : AbstractValidator<ApproveBatchDto>
{
    public ApproveBatchValidator()
    {
        RuleFor(x => x.CertificateNumber)
            .MaximumLength(100)
            .WithMessage("Número do certificado não pode exceder 100 caracteres");

        RuleFor(x => x.QualityNotes)
            .MaximumLength(500)
            .WithMessage("Observações não podem exceder 500 caracteres");
    }
}

public class RejectBatchValidator : AbstractValidator<RejectBatchDto>
{
    public RejectBatchValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Motivo da reprovação é obrigatório")
            .MaximumLength(500)
            .WithMessage("Motivo não pode exceder 500 caracteres");

        RuleFor(x => x.QualityNotes)
            .MaximumLength(500)
            .WithMessage("Observações não podem exceder 500 caracteres");
    }
}
