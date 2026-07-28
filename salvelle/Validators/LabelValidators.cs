using FluentValidation;
using DTOs.Labels;

namespace Validators.Labels;

public class CreateLabelTemplateValidator : AbstractValidator<CreateLabelTemplateDto>
{
    public CreateLabelTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres");

        RuleFor(x => x.TemplateType)
            .NotEmpty().WithMessage("Tipo de template é obrigatório")
            .Must(type => new[] { "PADRAO", "CONTROLADO", "HOMEOPATICO", "FITOTERAPICO", "VETERINARIO" }
                .Contains(type.ToUpper()))
            .WithMessage("Tipo de template inválido");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Largura deve ser maior que zero");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Altura deve ser maior que zero");

        RuleFor(x => x.HtmlTemplate)
            .NotEmpty().WithMessage("Template HTML é obrigatório");
    }
}

public class UpdateLabelTemplateValidator : AbstractValidator<UpdateLabelTemplateDto>
{
    public UpdateLabelTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres");
    }
}

public class GenerateLabelValidator : AbstractValidator<GenerateLabelDto>
{
    public GenerateLabelValidator()
    {
        RuleFor(x => x.ManipulationOrderId)
            .NotEmpty().WithMessage("ID da ordem de manipulação é obrigatório");
    }
}

public class PrintLabelValidator : AbstractValidator<PrintLabelDto>
{
    public PrintLabelValidator()
    {
        RuleFor(x => x.Copies)
            .GreaterThan(0).WithMessage("Número de cópias deve ser maior que zero")
            .LessThanOrEqualTo(10).WithMessage("Máximo de 10 cópias por impressão");

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Formato é obrigatório")
            .Must(format => new[] { "HTML", "PDF", "ZPL" }.Contains(format.ToUpper()))
            .WithMessage("Formato inválido (HTML, PDF ou ZPL)");

        RuleFor(x => x.PrintReason)
            .NotEmpty().WithMessage("Motivo da impressão é obrigatório")
            .Must(reason => new[] { "IMPRESSAO", "REIMPRESSAO", "TESTE" }.Contains(reason.ToUpper()))
            .WithMessage("Motivo inválido");
    }
}

public class BatchPrintValidator : AbstractValidator<BatchPrintDto>
{
    public BatchPrintValidator()
    {
        RuleFor(x => x.LabelIds)
            .NotEmpty().WithMessage("Lista de IDs não pode estar vazia")
            .Must(list => list.Count <= 50).WithMessage("Máximo de 50 rótulos por lote");

        RuleFor(x => x.Copies)
            .GreaterThan(0).WithMessage("Número de cópias deve ser maior que zero")
            .LessThanOrEqualTo(10).WithMessage("Máximo de 10 cópias por impressão");

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Formato é obrigatório")
            .Must(format => new[] { "HTML", "PDF", "ZPL" }.Contains(format.ToUpper()))
            .WithMessage("Formato inválido (HTML, PDF ou ZPL)");
    }
}