using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs.Sales;

namespace Service;

public class SaleService
{
    private readonly AppDbContext _context;

    public SaleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, Sale? Sale)> CreateSaleAsync(
        CreateSaleDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verificar cliente se informado
            if (dto.CustomerId.HasValue)
            {
                var customerExists = await _context.Customers
                    .AnyAsync(c => c.Id == dto.CustomerId.Value &&
                                  c.EstablishmentId == establishmentId);

                if (!customerExists)
                    return (false, "Cliente não encontrado", null);
            }

            // Calcular valores
            decimal subtotal = 0;
            foreach (var item in dto.Items)
            {
                var itemTotal = item.Quantity * item.UnitPrice;
                var itemDiscount = item.DiscountPercentage > 0
                    ? itemTotal * (item.DiscountPercentage / 100)
                    : 0;
                subtotal += itemTotal - itemDiscount;
            }

            var discountAmount = dto.DiscountPercentage > 0
                ? subtotal * (dto.DiscountPercentage / 100)
                : dto.DiscountAmount;

            var totalAmount = subtotal - discountAmount;
            var changeAmount = dto.PaidAmount - totalAmount;

            if (changeAmount < 0)
                return (false, "Valor pago insuficiente", null);

            // Gerar código
            var code = await GenerateSaleCodeAsync(establishmentId);

            var sale = new Sale
            {
                EstablishmentId = establishmentId,
                CustomerId = dto.CustomerId,
                Code = code,
                SaleDate = dto.SaleDate,
                Subtotal = subtotal,
                DiscountPercentage = dto.DiscountPercentage,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                PaymentMethod = dto.PaymentMethod.ToUpper(),
                PaymentStatus = "PAGO",
                PaidAmount = dto.PaidAmount,
                ChangeAmount = changeAmount,
                PaymentDate = DateTime.UtcNow,
                Status = "FINALIZADA",
                Observations = dto.Observations,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // Adicionar itens
            foreach (var itemDto in dto.Items)
            {
                var itemTotal = itemDto.Quantity * itemDto.UnitPrice;
                var itemDiscount = itemDto.DiscountPercentage > 0
                    ? itemTotal * (itemDto.DiscountPercentage / 100)
                    : 0;

                var item = new SaleItem
                {
                    SaleId = sale.Id,
                    ManipulationOrderId = itemDto.ManipulationOrderId,
                    PrescriptionId = itemDto.PrescriptionId,
                    FormulaId = itemDto.FormulaId,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountPercentage = itemDto.DiscountPercentage,
                    DiscountAmount = itemDiscount,
                    TotalPrice = itemTotal - itemDiscount,
                    CostPrice = 0, // TODO: calcular custo real
                    ProfitMargin = 0
                };

                _context.SaleItems.Add(item);

                // Atualizar status da OM se vinculada
                if (itemDto.ManipulationOrderId.HasValue)
                {
                    var order = await _context.ManipulationOrders
                        .FirstOrDefaultAsync(o => o.Id == itemDto.ManipulationOrderId.Value);

                    if (order != null)
                    {
                        order.Status = "ENTREGUE";
                        order.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Atualizar status da prescrição se vinculada
                if (itemDto.PrescriptionId.HasValue)
                {
                    var prescription = await _context.Prescriptions
                        .FirstOrDefaultAsync(p => p.Id == itemDto.PrescriptionId.Value);

                    if (prescription != null && prescription.Status == "VALIDADA")
                    {
                        prescription.Status = "MANIPULADA";
                        prescription.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Venda registrada com sucesso", sale);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao registrar venda: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> CancelSaleAsync(
        Guid saleId,
        string reason,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId &&
                                         s.EstablishmentId == establishmentId);

            if (sale == null)
                return (false, "Venda não encontrada");

            if (sale.Status == "CANCELADA")
                return (false, "Venda já está cancelada");

            // Reverter status das OMs
            foreach (var item in sale.Items)
            {
                if (item.ManipulationOrderId.HasValue)
                {
                    var order = await _context.ManipulationOrders
                        .FirstOrDefaultAsync(o => o.Id == item.ManipulationOrderId.Value);

                    if (order != null && order.Status == "ENTREGUE")
                    {
                        order.Status = "FINALIZADO";
                        order.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            sale.Status = "CANCELADA";
            sale.PaymentStatus = "CANCELADO";
            sale.CancelledAt = DateTime.UtcNow;
            sale.CancelledByEmployeeId = employeeId;
            sale.CancellationReason = reason;
            sale.UpdatedAt = DateTime.UtcNow;
            sale.UpdatedByEmployeeId = employeeId;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Venda cancelada com sucesso");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao cancelar venda: {ex.Message}");
        }
    }

    public async Task<DailySalesReportDto> GetDailySalesReportAsync(
        Guid establishmentId,
        DateTime date)
    {
        var sales = await _context.Sales
            .Include(s => s.Items)
            .Where(s => s.EstablishmentId == establishmentId &&
                       s.SaleDate.Date == date.Date &&
                       s.Status == "FINALIZADA")
            .ToListAsync();

        var report = new DailySalesReportDto
        {
            Date = date,
            TotalSales = sales.Count,
            TotalAmount = sales.Sum(s => s.TotalAmount),
            TotalCost = sales.SelectMany(s => s.Items).Sum(i => i.CostPrice * i.Quantity),
            AverageTicket = sales.Count > 0 ? sales.Average(s => s.TotalAmount) : 0
        };

        report.TotalProfit = report.TotalAmount - report.TotalCost;

        // Agrupar por forma de pagamento
        var byPaymentMethod = sales.GroupBy(s => s.PaymentMethod);
        foreach (var group in byPaymentMethod)
        {
            report.SalesByPaymentMethod[group.Key] = group.Count();
            report.AmountByPaymentMethod[group.Key] = group.Sum(s => s.TotalAmount);
        }

        return report;
    }

    private async Task<string> GenerateSaleCodeAsync(Guid establishmentId)
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;
        var prefix = $"VD{year}{month:D2}";

        var lastSale = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId &&
                       s.Code.StartsWith(prefix))
            .OrderByDescending(s => s.Code)
            .Select(s => s.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastSale != null && lastSale.Length > prefix.Length)
        {
            var numberPart = lastSale.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }
}