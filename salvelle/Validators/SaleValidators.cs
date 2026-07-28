using FluentValidation;
using DTOs.Sales;

namespace Validators;

public class CreateSaleValidator : AbstractValidator<CreateSaleDto>
{
    public CreateSaleValidator()
    {
        RuleFor(x => x.SaleDate)
            .NotEmpty().WithMessage("Data da venda é obrigatória")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Data não pode ser futura");

        RuleFor(x => x.PaymentMethod)
            .Must(x => x == "DINHEIRO" || x == "CARTAO_CREDITO" ||
                      x == "CARTAO_DEBITO" || x == "PIX" || x == "BOLETO")
            .WithMessage("Forma de pagamento inválida");

        RuleFor(x => x.PaidAmount)
            .GreaterThan(0).WithMessage("Valor pago deve ser maior que zero");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Venda deve ter pelo menos um item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description)
                .NotEmpty().WithMessage("Descrição do item é obrigatória");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Preço unitário deve ser maior que zero");
        });
    }
}

public class CancelSaleValidator : AbstractValidator<CancelSaleDto>
{
    public CancelSaleValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Motivo do cancelamento é obrigatório")
            .MinimumLength(10).WithMessage("Motivo deve ter no mínimo 10 caracteres");
    }
}

public class CreateQuotationValidator : AbstractValidator<CreateQuotationDto>
{
    public CreateQuotationValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Orçamento deve ter pelo menos um item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description)
                .NotEmpty().WithMessage("Descrição do item é obrigatória");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Preço unitário deve ser maior que zero");
        });
    }
}