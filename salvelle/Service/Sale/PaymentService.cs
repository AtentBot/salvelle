using Data;
using DTOs.Payments;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Service;

public class PaymentService
{
    private readonly AppDbContext _context;

    public PaymentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, SalePayment? Payment)> AddPaymentToSaleAsync(
        Guid saleId,
        CreatePaymentDto dto,
        Guid employeeId)
    {
        var sale = await _context.Sales
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            return (false, "Venda não encontrada", null);

        if (sale.Status == "CANCELADA")
            return (false, "Não é possível adicionar pagamentos a uma venda cancelada", null);

        // Validar se a soma dos pagamentos não ultrapassa o total
        var currentPaidAmount = sale.Payments?.Sum(p => p.Amount) ?? 0;
        var remainingAmount = sale.TotalAmount - currentPaidAmount;

        if (dto.Amount > remainingAmount)
            return (false, $"Valor do pagamento (R$ {dto.Amount:F2}) excede o valor restante (R$ {remainingAmount:F2})", null);

        // Verificar pagamento duplicado por identificadores externos
        if (!string.IsNullOrEmpty(dto.Nsu))
        {
            var duplicateNsu = await _context.SalePayments.AnyAsync(p => p.Nsu == dto.Nsu);
            if (duplicateNsu)
                return (false, $"Pagamento com NSU {dto.Nsu} já registrado", null);
        }
        if (!string.IsNullOrEmpty(dto.PixTransactionId))
        {
            var duplicatePix = await _context.SalePayments.AnyAsync(p => p.PixTransactionId == dto.PixTransactionId);
            if (duplicatePix)
                return (false, $"Pagamento com transação PIX {dto.PixTransactionId} já registrado", null);
        }

        var payment = new SalePayment
        {
            SaleId = saleId,
            PaymentMethod = dto.PaymentMethod.ToUpper(),
            Amount = dto.Amount,
            ProcessedByEmployeeId = employeeId,
            PaymentDate = DateTime.UtcNow,
            Observations = dto.Observations
        };

        // Preencher campos específicos por método
        switch (dto.PaymentMethod.ToUpper())
        {
            case "DINHEIRO":
                payment.CashReceived = dto.CashReceived ?? dto.Amount;
                payment.ChangeAmount = payment.CashReceived - dto.Amount;
                break;

            case "CARTAO_DEBITO":
            case "CARTAO_CREDITO":
                payment.CardBrand = dto.CardBrand;
                payment.CardLastDigits = dto.CardLastDigits;
                payment.Installments = dto.Installments ?? 1;
                payment.Nsu = dto.Nsu;
                payment.AuthorizationCode = dto.AuthorizationCode;
                break;

            case "PIX":
                payment.PixKey = dto.PixKey;
                payment.PixTransactionId = dto.PixTransactionId;
                payment.PaymentStatus = string.IsNullOrEmpty(dto.PixTransactionId) ? "PENDING" : "APPROVED";
                break;

            case "BOLETO":
                payment.BoletoBarcode = dto.BoletoBarcode;
                payment.BoletoDueDate = dto.BoletoDueDate;
                payment.PaymentStatus = "PENDING"; // Boleto sempre começa pendente
                break;
        }

        _context.SalePayments.Add(payment);

        // Atualizar status da venda se totalmente pago
        var newTotalPaid = currentPaidAmount + dto.Amount;
        if (newTotalPaid >= sale.TotalAmount)
        {
            sale.Status = "FINALIZADA";
        }
        else
        {
            sale.Status = "PAGAMENTO_PARCIAL";
        }

        await _context.SaveChangesAsync();

        return (true, "Pagamento registrado com sucesso", payment);
    }

    public async Task<List<PaymentDto>> GetSalePaymentsAsync(Guid saleId)
    {
        return await _context.SalePayments
            .Where(p => p.SaleId == saleId)
            .Include(p => p.ProcessedByEmployee)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount,
                PaymentStatus = p.PaymentStatus,
                PaymentDate = p.PaymentDate,
                CashReceived = p.CashReceived,
                ChangeAmount = p.ChangeAmount,
                CardBrand = p.CardBrand,
                CardLastDigits = p.CardLastDigits,
                Installments = p.Installments,
                Nsu = p.Nsu,
                AuthorizationCode = p.AuthorizationCode,
                PixKey = p.PixKey,
                PixTransactionId = p.PixTransactionId,
                BoletoBarcode = p.BoletoBarcode,
                BoletoDueDate = p.BoletoDueDate,
                BoletoNumber = p.BoletoNumber,
                GatewayProvider = p.GatewayProvider,
                GatewayTransactionId = p.GatewayTransactionId,
                GatewayStatus = p.GatewayStatus,
                ProcessedByEmployeeName = p.ProcessedByEmployee.FullName,
                Observations = p.Observations
            })
            .ToListAsync();
    }

    public async Task<SaleWithPaymentsDto?> GetSaleWithPaymentsAsync(Guid saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.CreatedByEmployee)
            .Include(s => s.Payments)
                .ThenInclude(p => p.ProcessedByEmployee)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            return null;

        return new SaleWithPaymentsDto
        {
            Id = sale.Id,
            Code = sale.Code,
            SaleDate = sale.SaleDate,
            CustomerName = sale.Customer?.FullName,
            Subtotal = sale.Subtotal,
            DiscountAmount = sale.DiscountAmount,
            TotalAmount = sale.TotalAmount,
            Status = sale.Status,
            CreatedByEmployeeName = sale.CreatedByEmployee.FullName,
            Payments = sale.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount,
                PaymentStatus = p.PaymentStatus,
                PaymentDate = p.PaymentDate,
                CashReceived = p.CashReceived,
                ChangeAmount = p.ChangeAmount,
                CardBrand = p.CardBrand,
                CardLastDigits = p.CardLastDigits,
                Installments = p.Installments,
                Nsu = p.Nsu,
                AuthorizationCode = p.AuthorizationCode,
                PixKey = p.PixKey,
                PixTransactionId = p.PixTransactionId,
                BoletoBarcode = p.BoletoBarcode,
                BoletoDueDate = p.BoletoDueDate,
                BoletoNumber = p.BoletoNumber,
                GatewayProvider = p.GatewayProvider,
                GatewayTransactionId = p.GatewayTransactionId,
                GatewayStatus = p.GatewayStatus,
                ProcessedByEmployeeName = p.ProcessedByEmployee.FullName,
                Observations = p.Observations
            }).ToList()
        };
    }

    public async Task<DailyCashFlowDto> GetDailyCashFlowAsync(Guid establishmentId, DateTime date)
    {
        var startDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var endDate = startDate.AddDays(1);

        var payments = await _context.SalePayments
            .Include(p => p.Sale)
            .Where(p => p.Sale.EstablishmentId == establishmentId &&
                       p.PaymentDate >= startDate &&
                       p.PaymentDate < endDate &&
                       p.PaymentStatus == "APPROVED")
            .ToListAsync();

        var totalCash = payments.Where(p => p.PaymentMethod == "DINHEIRO").Sum(p => p.Amount);
        var totalDebit = payments.Where(p => p.PaymentMethod == "CARTAO_DEBITO").Sum(p => p.Amount);
        var totalCredit = payments.Where(p => p.PaymentMethod == "CARTAO_CREDITO").Sum(p => p.Amount);
        var totalPix = payments.Where(p => p.PaymentMethod == "PIX").Sum(p => p.Amount);
        var totalBoleto = payments.Where(p => p.PaymentMethod == "BOLETO").Sum(p => p.Amount);
        var grandTotal = payments.Sum(p => p.Amount);

        var breakdown = payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new PaymentMethodSummary
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount),
                Percentage = grandTotal > 0 ? (g.Sum(p => p.Amount) / grandTotal) * 100 : 0
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

        return new DailyCashFlowDto
        {
            Date = date,
            TotalCash = totalCash,
            TotalDebitCard = totalDebit,
            TotalCreditCard = totalCredit,
            TotalPix = totalPix,
            TotalBoleto = totalBoleto,
            GrandTotal = grandTotal,
            TotalSales = await _context.Sales
                .Where(s => s.EstablishmentId == establishmentId &&
                           s.SaleDate >= startDate &&
                           s.SaleDate < endDate &&
                           s.Status == "FINALIZADA")
                .CountAsync(),
            PaymentMethodBreakdown = breakdown
        };
    }

    public async Task<(bool Success, string Message)> CancelPaymentAsync(Guid paymentId, Guid employeeId)
    {
        var payment = await _context.SalePayments
            .Include(p => p.Sale)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            return (false, "Pagamento não encontrado");

        if (payment.PaymentStatus == "REFUNDED")
            return (false, "Pagamento já foi estornado");

        payment.PaymentStatus = "REFUNDED";
        payment.UpdatedAt = DateTime.UtcNow;

        // Recalcular status da venda
        var sale = payment.Sale;
        var activePaidAmount = await _context.SalePayments
            .Where(p => p.SaleId == sale.Id && p.PaymentStatus == "APPROVED")
            .SumAsync(p => p.Amount);

        if (activePaidAmount == 0)
        {
            sale.Status = "PENDENTE";
        }
        else if (activePaidAmount < sale.TotalAmount)
        {
            sale.Status = "PAGAMENTO_PARCIAL";
        }

        await _context.SaveChangesAsync();

        return (true, "Pagamento estornado com sucesso");
    }
}