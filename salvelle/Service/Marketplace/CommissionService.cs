using Data;
using Microsoft.EntityFrameworkCore;
using Models.Marketplace;

namespace Service.Marketplace;

public class CommissionService
{
    private readonly AppDbContext _db;

    public CommissionService(AppDbContext db)
    {
        _db = db;
    }

    // Faixas de comissão escalonada
    private const decimal RATE_LOW = 0.07m;    // 7% - até 20 vendas/semana
    private const decimal RATE_MID = 0.05m;    // 5% - 21 a 100 vendas/semana
    private const decimal RATE_HIGH = 0.03m;   // 3% - acima de 100 vendas/semana

    private const int THRESHOLD_LOW = 20;
    private const int THRESHOLD_MID = 100;

    /// <summary>
    /// Calcula a taxa de comissão com base no volume de vendas semanal da farmácia
    /// </summary>
    public decimal GetCommissionRate(int weeklySalesCount)
    {
        return weeklySalesCount switch
        {
            <= THRESHOLD_LOW => RATE_LOW,
            <= THRESHOLD_MID => RATE_MID,
            _ => RATE_HIGH
        };
    }

    /// <summary>
    /// Obtém a taxa de comissão atual da farmácia com base na semana corrente
    /// </summary>
    public async Task<decimal> GetCurrentCommissionRateAsync(Guid establishmentId)
    {
        var weekStart = GetWeekStart(DateTime.UtcNow);
        var weekEnd = weekStart.AddDays(7);

        var salesCount = await _db.OnlineOrders
            .Where(o => o.EstablishmentId == establishmentId
                        && o.CreatedAt >= weekStart
                        && o.CreatedAt < weekEnd
                        && o.Status != "CANCELLED"
                        && o.PaymentStatus == "PAID")
            .CountAsync();

        return GetCommissionRate(salesCount);
    }

    /// <summary>
    /// Calcula a comissão para uma venda específica
    /// </summary>
    public async Task<(decimal rate, decimal commission, decimal netAmount)> CalculateCommissionAsync(
        Guid establishmentId, decimal orderTotal)
    {
        var rate = await GetCurrentCommissionRateAsync(establishmentId);
        var commission = Math.Round(orderTotal * rate, 2);
        var netAmount = orderTotal - commission;

        return (rate, commission, netAmount);
    }

    /// <summary>
    /// Registra a transação da plataforma para uma venda
    /// </summary>
    public async Task<PlatformTransaction> RegisterTransactionAsync(
        Guid orderId, Guid establishmentId, Guid? customerId,
        decimal grossAmount, decimal commissionRate)
    {
        var commissionAmount = Math.Round(grossAmount * commissionRate, 2);

        var transaction = new PlatformTransaction
        {
            OrderId = orderId,
            EstablishmentId = establishmentId,
            CustomerId = customerId,
            GrossAmount = grossAmount,
            CommissionRate = commissionRate,
            CommissionAmount = commissionAmount,
            NetAmountToPharmacy = grossAmount - commissionAmount,
            Status = "PENDENTE"
        };

        _db.PlatformTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return transaction;
    }

    /// <summary>
    /// Calcula e persiste as comissões semanais de todas as farmácias.
    /// Deve ser chamado pelo cron job semanal.
    /// </summary>
    public async Task<List<PlatformCommission>> CalculateWeeklyCommissionsAsync(DateTime? referenceDate = null)
    {
        var date = referenceDate ?? DateTime.UtcNow;
        var weekStart = GetWeekStart(date);
        var weekEnd = weekStart.AddDays(7);

        // Verificar se já foi calculado para esta semana
        var alreadyCalculated = await _db.PlatformCommissions
            .AnyAsync(c => c.WeekStartDate == weekStart);

        if (alreadyCalculated)
            return await _db.PlatformCommissions
                .Where(c => c.WeekStartDate == weekStart)
                .ToListAsync();

        // Agrupar vendas por farmácia
        var salesByEstablishment = await _db.PlatformTransactions
            .Where(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd && t.Status != "ESTORNADO")
            .GroupBy(t => t.EstablishmentId)
            .Select(g => new
            {
                EstablishmentId = g.Key,
                TotalSalesCount = g.Count(),
                TotalSalesAmount = g.Sum(t => t.GrossAmount),
                TotalCommissionAmount = g.Sum(t => t.CommissionAmount)
            })
            .ToListAsync();

        var commissions = new List<PlatformCommission>();

        foreach (var group in salesByEstablishment)
        {
            var commission = new PlatformCommission
            {
                EstablishmentId = group.EstablishmentId,
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                TotalSalesCount = group.TotalSalesCount,
                CommissionRate = GetCommissionRate(group.TotalSalesCount),
                TotalSalesAmount = group.TotalSalesAmount,
                TotalCommissionAmount = group.TotalCommissionAmount,
                Status = "CALCULADO"
            };

            commissions.Add(commission);
        }

        _db.PlatformCommissions.AddRange(commissions);
        await _db.SaveChangesAsync();

        return commissions;
    }

    /// <summary>
    /// Retorna o resumo financeiro de uma farmácia
    /// </summary>
    public async Task<PharmacyFinancialSummary> GetPharmacyFinancialSummaryAsync(
        Guid establishmentId, DateTime? from = null, DateTime? to = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var transactions = await _db.PlatformTransactions
            .Where(t => t.EstablishmentId == establishmentId
                        && t.CreatedAt >= startDate
                        && t.CreatedAt <= endDate
                        && t.Status != "ESTORNADO")
            .ToListAsync();

        return new PharmacyFinancialSummary
        {
            EstablishmentId = establishmentId,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TotalOrders = transactions.Count,
            GrossRevenue = transactions.Sum(t => t.GrossAmount),
            TotalCommission = transactions.Sum(t => t.CommissionAmount),
            NetRevenue = transactions.Sum(t => t.NetAmountToPharmacy),
            CurrentCommissionRate = await GetCurrentCommissionRateAsync(establishmentId)
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}

public class PharmacyFinancialSummary
{
    public Guid EstablishmentId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOrders { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal CurrentCommissionRate { get; set; }
}
