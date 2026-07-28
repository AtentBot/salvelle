using FluentValidation;
using DTOs.Purchasing;

namespace Validators.Purchasing;

public class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderDto>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEqual(Guid.Empty)  // ✅ MUDOU: GreaterThan(0) → NotEqual(Guid.Empty)
            .WithMessage("Fornecedor é obrigatório");

        RuleFor(x => x.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.ExpectedDeliveryDate.HasValue)
            .WithMessage("Data prevista deve ser maior ou igual a hoje");

        RuleFor(x => x.DiscountValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Desconto não pode ser negativo");

        RuleFor(x => x.ShippingValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Frete não pode ser negativo");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Observações não podem exceder 500 caracteres");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Pedido deve ter pelo menos um item");

        RuleForEach(x => x.Items)
            .SetValidator(new CreatePurchaseOrderItemValidator());
    }
}

public class CreatePurchaseOrderItemValidator : AbstractValidator<CreatePurchaseOrderItemDto>
{
    public CreatePurchaseOrderItemValidator()
    {
        RuleFor(x => x.RawMaterialId)
            .NotEqual(Guid.Empty)  // ✅ MUDOU: GreaterThan(0) → NotEqual(Guid.Empty)
            .WithMessage("Matéria-prima é obrigatória");

        RuleFor(x => x.QuantityOrdered)
            .GreaterThan(0)
            .WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithMessage("Unidade é obrigatória")
            .Must(unit => new[] { "g", "kg", "mL", "L", "UN" }.Contains(unit))
            .WithMessage("Unidade inválida. Use: g, kg, mL, L ou UN");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Preço unitário não pode ser negativo");

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Desconto deve estar entre 0 e 100%");

        RuleFor(x => x.Notes)
            .MaximumLength(200)
            .WithMessage("Observações não podem exceder 200 caracteres");
    }
}

public class UpdatePurchaseOrderValidator : AbstractValidator<UpdatePurchaseOrderDto>
{
    public UpdatePurchaseOrderValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEqual(Guid.Empty)  // ✅ MUDOU: GreaterThan(0) → NotEqual(Guid.Empty)
            .WithMessage("Fornecedor é obrigatório");

        RuleFor(x => x.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.ExpectedDeliveryDate.HasValue)
            .WithMessage("Data prevista deve ser maior ou igual a hoje");

        RuleFor(x => x.DiscountValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Desconto não pode ser negativo");

        RuleFor(x => x.ShippingValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Frete não pode ser negativo");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Observações não podem exceder 500 caracteres");
    }
}

public class ReceivePurchaseOrderValidator : AbstractValidator<ReceivePurchaseOrderDto>
{
    public ReceivePurchaseOrderValidator()
    {
        RuleFor(x => x.ActualDeliveryDate)
            .LessThanOrEqualTo(Helpers.BrazilTime.Now)
            .WithMessage("Data de recebimento não pode ser futura");

        RuleFor(x => x.SupplierInvoiceNumber)
            .MaximumLength(100)
            .WithMessage("Número da nota fiscal não pode exceder 100 caracteres");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Deve receber pelo menos um item");

        RuleForEach(x => x.Items)
            .SetValidator(new ReceivePurchaseOrderItemValidator());
    }
}

public class ReceivePurchaseOrderItemValidator : AbstractValidator<ReceivePurchaseOrderItemDto>
{
    public ReceivePurchaseOrderItemValidator()
    {
        RuleFor(x => x.PurchaseOrderItemId)
            .GreaterThan(0)  // ✅ MANTÉM int (PurchaseOrderItem.Id é int)
            .WithMessage("Item do pedido é obrigatório");

        RuleFor(x => x.BatchNumber)
            .NotEmpty()
            .WithMessage("Número do lote é obrigatório")
            .MaximumLength(50)
            .WithMessage("Número do lote não pode exceder 50 caracteres");

        RuleFor(x => x.ManufactureDate)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Data de fabricação não pode ser futura");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.Today)
            .WithMessage("Data de validade deve ser futura")
            .GreaterThan(x => x.ManufactureDate)
            .WithMessage("Data de validade deve ser posterior à data de fabricação");

        RuleFor(x => x.QuantityReceived)
            .GreaterThan(0)
            .WithMessage("Quantidade recebida deve ser maior que zero");

        RuleFor(x => x.CertificateOfAnalysis)
            .MaximumLength(200)
            .WithMessage("Certificado de análise não pode exceder 200 caracteres");

        RuleFor(x => x.Notes)
            .MaximumLength(200)
            .WithMessage("Observações não podem exceder 200 caracteres");
    }
}
