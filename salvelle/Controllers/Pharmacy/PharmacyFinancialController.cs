using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Pharmacy.Marketplace;
using Service.Marketplace;

namespace Controllers.Pharmacy;

/// <summary>
/// Dashboard financeiro da farmácia no marketplace
/// </summary>
[ApiController]
[Route("api/pharmacy/marketplace/financial")]
public class PharmacyFinancialController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CommissionService _commission;

    public PharmacyFinancialController(AppDbContext db, CommissionService commission)
    {
        _db = db;
        _commission = commission;
    }

    /// <summary>
    /// Dashboard financeiro completo
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<PharmacyFinancialDashboardDto>>> GetDashboard(
        [FromQuery] FinancialFilterRequest filter)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var from = filter.From ?? DateTime.UtcNow.AddDays(-30);
        var to = filter.To ?? DateTime.UtcNow;
        var periodDays = (to - from).TotalDays;

        // Período anterior para comparação
        var prevFrom = from.AddDays(-periodDays);
        var prevTo = from;

        // Resumo financeiro atual
        var summary = await _commission.GetPharmacyFinancialSummaryAsync(establishmentId.Value, from, to);
        var prevSummary = await _commission.GetPharmacyFinancialSummaryAsync(establishmentId.Value, prevFrom, prevTo);

        // Vendas da semana (para calcular tier de comissão)
        var weekStart = GetWeekStart(DateTime.UtcNow);
        var weeklySalesCount = await _db.OnlineOrders
            .CountAsync(o => o.EstablishmentId == establishmentId.Value
                             && o.CreatedAt >= weekStart
                             && o.Status != "CANCELLED"
                             && o.PaymentStatus == "PAID");

        var commissionRate = _commission.GetCommissionRate(weeklySalesCount);

        // Receita diária
        var dailyRevenue = await _db.PlatformTransactions
            .Where(t => t.EstablishmentId == establishmentId.Value
                        && t.CreatedAt >= from && t.CreatedAt <= to
                        && t.Status != "ESTORNADO")
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Gross = g.Sum(t => t.GrossAmount),
                Net = g.Sum(t => t.NetAmountToPharmacy),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Top produtos
        var topProducts = await _db.OnlineOrderItems
            .Include(i => i.Order)
            .Where(i => i.Order != null
                        && i.Order.EstablishmentId == establishmentId.Value
                        && i.Order.CreatedAt >= from && i.Order.CreatedAt <= to
                        && i.Order.Status != "CANCELLED")
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId ?? Guid.Empty,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(10)
            .ToListAsync();

        // Breakdown por status
        var statusCounts = await _db.OnlineOrders
            .Where(o => o.EstablishmentId == establishmentId.Value
                        && o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Rating
        var establishment = await _db.Establishments.FindAsync(establishmentId.Value);

        var dashboard = new PharmacyFinancialDashboardDto
        {
            GrossRevenue = summary.GrossRevenue,
            TotalCommission = summary.TotalCommission,
            NetRevenue = summary.NetRevenue,
            TotalOrders = summary.TotalOrders,
            CurrentCommissionRate = commissionRate,
            WeeklySalesCount = weeklySalesCount,
            CommissionTier = $"{commissionRate * 100:F0}%",

            RevenueChange = prevSummary.GrossRevenue > 0
                ? Math.Round(((summary.GrossRevenue - prevSummary.GrossRevenue) / prevSummary.GrossRevenue) * 100, 1)
                : 0,
            OrdersChange = prevSummary.TotalOrders > 0
                ? (int)Math.Round(((double)(summary.TotalOrders - prevSummary.TotalOrders) / prevSummary.TotalOrders) * 100)
                : 0,

            AverageOrderValue = summary.TotalOrders > 0
                ? Math.Round(summary.GrossRevenue / summary.TotalOrders, 2)
                : 0,
            AverageRating = establishment?.AverageRating ?? 0,
            TotalRatings = establishment?.TotalRatings ?? 0,

            DailyRevenue = dailyRevenue,
            TopProducts = topProducts,
            StatusBreakdown = new OrderStatusBreakdownDto
            {
                Pending = statusCounts.FirstOrDefault(s => s.Status == "PENDING")?.Count ?? 0,
                Confirmed = statusCounts.FirstOrDefault(s => s.Status == "CONFIRMED")?.Count ?? 0,
                Preparing = statusCounts.FirstOrDefault(s => s.Status == "PREPARING")?.Count ?? 0,
                Ready = statusCounts.FirstOrDefault(s => s.Status == "READY")?.Count ?? 0,
                Delivered = statusCounts.FirstOrDefault(s => s.Status == "DELIVERED")?.Count ?? 0,
                Cancelled = statusCounts.FirstOrDefault(s => s.Status == "CANCELLED")?.Count ?? 0
            }
        };

        return Ok(ApiResponse<PharmacyFinancialDashboardDto>.SuccessResponse(dashboard));
    }

    /// <summary>
    /// Histórico de comissões semanais
    /// </summary>
    [HttpGet("commissions")]
    public async Task<ActionResult<ApiResponse<List<WeeklyCommissionDto>>>> GetCommissions(
        [FromQuery] int weeks = 8)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        weeks = Math.Min(weeks, 52);

        var commissions = await _db.PlatformCommissions
            .Where(c => c.EstablishmentId == establishmentId.Value)
            .OrderByDescending(c => c.WeekStartDate)
            .Take(weeks)
            .Select(c => new WeeklyCommissionDto
            {
                WeekStart = c.WeekStartDate,
                WeekEnd = c.WeekEndDate,
                SalesCount = c.TotalSalesCount,
                CommissionRate = c.CommissionRate,
                GrossAmount = c.TotalSalesAmount,
                CommissionAmount = c.TotalCommissionAmount,
                NetAmount = c.TotalSalesAmount - c.TotalCommissionAmount,
                Status = c.Status
            })
            .ToListAsync();

        return Ok(ApiResponse<List<WeeklyCommissionDto>>.SuccessResponse(commissions));
    }

    /// <summary>
    /// Transações detalhadas
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetTransactions(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        pageSize = Math.Min(pageSize, 50);
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var transactions = await _db.PlatformTransactions
            .Include(t => t.Order)
            .Where(t => t.EstablishmentId == establishmentId.Value
                        && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                OrderNumber = t.Order != null ? t.Order.OrderNumber : "",
                GrossAmount = t.GrossAmount,
                CommissionRate = t.CommissionRate,
                CommissionAmount = t.CommissionAmount,
                NetAmount = t.NetAmountToPharmacy,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(transactions));
    }

    // ==================== HELPERS ====================

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private Guid? GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var id) && id is Guid estId)
            return estId;
        return null;
    }
}

// DTOs auxiliares para este controller
public class WeeklyCommissionDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int SalesCount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
