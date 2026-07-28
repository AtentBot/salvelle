using Microsoft.EntityFrameworkCore;
using Data;
using Models;

namespace Service;

/// <summary>
/// Serviço para gerenciamento de caixa
/// </summary>
public class CashRegisterService
{
    private readonly AppDbContext _context;

    public CashRegisterService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Abre um novo caixa
    /// </summary>
    public async Task<(bool Success, string Message, CashRegister? CashRegister)> OpenCashRegisterAsync(
        Guid establishmentId,
        Guid employeeId,
        decimal openingBalance,
        string? observations = null)
    {
        // Verificar se já existe caixa aberto
        var existingOpen = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && 
                                     c.Status == "ABERTO" &&
                                     c.ClosingDate == null);

        if (existingOpen != null)
            return (false, $"Já existe um caixa aberto: {existingOpen.Code}", null);

        var code = await GenerateCashRegisterCodeAsync(establishmentId);

        var cashRegister = new CashRegister
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = code,
            OpeningDate = DateTime.UtcNow,
            OpenedByEmployeeId = employeeId,
            OpeningBalance = openingBalance,
            TotalCash = openingBalance,
            TotalCard = 0,
            TotalPix = 0,
            TotalSales = 0,
            SalesCount = 0,
            Status = "ABERTO",
            Observations = observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CashRegisters.Add(cashRegister);
        await _context.SaveChangesAsync();

        return (true, $"Caixa {code} aberto com sucesso", cashRegister);
    }

    /// <summary>
    /// Fecha o caixa
    /// </summary>
    public async Task<(bool Success, string Message)> CloseCashRegisterAsync(
        Guid cashRegisterId,
        Guid employeeId,
        decimal actualClosingBalance,
        string? observations = null)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId && c.Status == "ABERTO");

        if (cashRegister == null)
            return (false, "Caixa não encontrado ou já fechado");

        // Calcular valor esperado (saldo inicial + entradas - sangrias + suprimentos)
        var expectedBalance = cashRegister.OpeningBalance + 
                             cashRegister.TotalCash - 
                             cashRegister.TotalWithdrawals + 
                             cashRegister.TotalSupplies;

        cashRegister.ClosingDate = DateTime.UtcNow;
        cashRegister.ClosedByEmployeeId = employeeId;
        cashRegister.ExpectedBalance = expectedBalance;
        cashRegister.ClosingBalance = actualClosingBalance;
        cashRegister.Difference = actualClosingBalance - expectedBalance;
        cashRegister.Status = "FECHADO";
        cashRegister.ClosingObservations = observations;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var differenceText = cashRegister.Difference switch
        {
            > 0 => $"Sobra de R$ {cashRegister.Difference:F2}",
            < 0 => $"Falta de R$ {Math.Abs(cashRegister.Difference.Value):F2}",
            _ => "Sem diferença"
        };

        return (true, $"Caixa fechado com sucesso. {differenceText}");
    }

    /// <summary>
    /// Busca caixa aberto
    /// </summary>
    public async Task<CashRegister?> GetOpenCashRegisterAsync(Guid establishmentId)
    {
        return await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && 
                                     c.Status == "ABERTO" &&
                                     c.ClosingDate == null);
    }

    /// <summary>
    /// Registra uma venda no caixa
    /// </summary>
    public async Task<bool> RegisterSaleInCashRegisterAsync(
        Guid cashRegisterId,
        Guid saleId,
        decimal amount,
        string paymentMethod,
        Guid employeeId)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId && c.Status == "ABERTO");

        if (cashRegister == null)
            return false;

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            SaleId = saleId,
            MovementType = "ENTRADA",
            PaymentMethod = paymentMethod,
            Amount = amount,
            Description = $"Venda registrada",
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashMovements.Add(movement);

        // Atualizar totais
        switch (paymentMethod.ToUpper())
        {
            case "DINHEIRO":
                cashRegister.TotalCash += amount;
                break;
            case "CARTAO_DEBITO":
            case "CARTAO_CREDITO":
                cashRegister.TotalCard += amount;
                break;
            case "PIX":
                cashRegister.TotalPix += amount;
                break;
        }

        cashRegister.TotalSales += amount;
        cashRegister.SalesCount++;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Realiza sangria (retirada de dinheiro)
    /// </summary>
    public async Task<(bool Success, string Message)> WithdrawCashAsync(
        Guid cashRegisterId,
        decimal amount,
        string reason,
        Guid employeeId)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId && c.Status == "ABERTO");

        if (cashRegister == null)
            return (false, "Caixa não encontrado ou fechado");

        var currentCash = cashRegister.OpeningBalance + cashRegister.TotalCash - cashRegister.TotalWithdrawals + cashRegister.TotalSupplies;
        if (amount > currentCash)
            return (false, $"Valor da sangria (R$ {amount:F2}) excede o disponível em caixa (R$ {currentCash:F2})");

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            MovementType = "SANGRIA",
            PaymentMethod = "DINHEIRO",
            Amount = amount,
            Description = reason,
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashMovements.Add(movement);

        // Registrar sangria
        cashRegister.TotalWithdrawals += amount;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, $"Sangria de R$ {amount:F2} realizada com sucesso");
    }

    /// <summary>
    /// Realiza suprimento (entrada de dinheiro)
    /// </summary>
    public async Task<(bool Success, string Message)> SupplyCashAsync(
        Guid cashRegisterId,
        decimal amount,
        string reason,
        Guid employeeId)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId && c.Status == "ABERTO");

        if (cashRegister == null)
            return (false, "Caixa não encontrado ou fechado");

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            MovementType = "SUPRIMENTO",
            PaymentMethod = "DINHEIRO",
            Amount = amount,
            Description = reason,
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashMovements.Add(movement);

        // Registrar suprimento
        cashRegister.TotalSupplies += amount;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, $"Suprimento de R$ {amount:F2} realizado com sucesso");
    }

    /// <summary>
    /// Busca histórico de caixas
    /// </summary>
    public async Task<List<CashRegister>> GetCashRegisterHistoryAsync(
        Guid establishmentId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 30)
    {
        var query = _context.CashRegisters
            .Where(c => c.EstablishmentId == establishmentId);

        if (startDate.HasValue)
            query = query.Where(c => c.OpeningDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.OpeningDate <= endDate.Value);

        return await query
            .OrderByDescending(c => c.OpeningDate)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Busca movimentos de um caixa
    /// </summary>
    public async Task<List<CashMovement>> GetCashMovementsAsync(Guid cashRegisterId)
    {
        return await _context.CashMovements
            .Where(m => m.CashRegisterId == cashRegisterId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    private async Task<string> GenerateCashRegisterCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var prefix = $"CX{today:yyyyMMdd}";

        var lastRegister = await _context.CashRegisters
            .Where(c => c.EstablishmentId == establishmentId && c.Code.StartsWith(prefix))
            .OrderByDescending(c => c.Code)
            .Select(c => c.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastRegister != null && lastRegister.Length > prefix.Length)
        {
            var numberPart = lastRegister.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D2}";
    }
}
