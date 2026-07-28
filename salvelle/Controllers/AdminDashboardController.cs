using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Models;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(AppDbContext context, ILogger<AdminDashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var totalEstablishments = await _context.Establishments.CountAsync();
            var activeEstablishments = await _context.Establishments.CountAsync(e => e.IsActive);
            var inactiveEstablishments = totalEstablishments - activeEstablishments;

            var subscriptions = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .ToListAsync();

            var trialingCount = subscriptions.Count(s => s.Status == "TRIALING");
            var pastDueCount = subscriptions.Count(s => s.Status == "PAST_DUE");

            var monthlyRevenue = subscriptions
                .Where(s => s.Status == "ACTIVE" && s.BillingCycle == "MONTHLY")
                .Sum(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceMonthly : 0);

            var yearlyRevenue = subscriptions
                .Where(s => s.Status == "ACTIVE" && s.BillingCycle == "YEARLY")
                .Sum(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceYearly / 12 : 0);

            var mrr = monthlyRevenue + yearlyRevenue;
            var arr = mrr * 12;

            // IMPORTANTE: Usar DateTimeKind.Utc para PostgreSQL
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Calcular em memória para evitar problemas de timezone
            var newSubscriptionsThisMonth = subscriptions.Count(s => s.CreatedAt >= firstDayOfMonth);

            var subscriptionsByPlan = subscriptions
                .Where(s => s.Status == "ACTIVE")
                .GroupBy(s => s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "Sem Plano")
                .ToDictionary(g => g.Key, g => g.Count());

            // Cálculo simples de churn (cancelamentos no mês / total ativo no início do mês)
            var canceledThisMonth = subscriptions.Count(s => s.CanceledAt.HasValue && s.CanceledAt.Value >= firstDayOfMonth);

            var activeAtStartOfMonth = activeEstablishments + canceledThisMonth;
            var churnRate = activeAtStartOfMonth > 0
                ? (decimal)canceledThisMonth / activeAtStartOfMonth * 100
                : 0;

            var metrics = new DashboardMetricsDto
            {
                TotalEstablishments = totalEstablishments,
                ActiveEstablishments = activeEstablishments,
                InactiveEstablishments = inactiveEstablishments,
                TrialingEstablishments = trialingCount,
                PastDueEstablishments = pastDueCount,
                MonthlyRecurringRevenue = mrr,
                AnnualRecurringRevenue = arr,
                NewSubscriptionsThisMonth = newSubscriptionsThisMonth,
                ChurnRate = churnRate,
                SubscriptionsByPlan = subscriptionsByPlan
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar métricas do dashboard");
            return StatusCode(500, new { message = "Erro ao buscar métricas" });
        }
    }

    [HttpGet("recent-signups")]
    public async Task<IActionResult> GetRecentSignups([FromQuery] int limit = 10)
    {
        try
        {
            var recentEstablishments = await _context.Establishments
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .Select(e => new
                {
                    e.Id,
                    e.NomeFantasia,
                    e.Email,
                    e.City,
                    e.State,
                    e.CreatedAt,
                    e.IsActive,
                    e.OnboardingCompleted,
                    e.SubscriptionStatus
                })
                .ToListAsync();

            return Ok(recentEstablishments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar cadastros recentes");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int months = 6)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            // Usar SubscriptionInvoices (nome correto do DbSet)
            var invoices = await _context.SubscriptionInvoices
                .Where(i => i.Status == "PAID" && i.PaidAt != null && i.PaidAt >= startDate)
                .ToListAsync();

            // Agrupar em memória para evitar problemas com tradução de query
            var grouped = invoices
                .GroupBy(i => new {
                    Year = i.PaidAt!.Value.Year,
                    Month = i.PaidAt.Value.Month
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(i => i.Amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            // Preencher meses sem dados
            var chartData = new List<object>();
            for (int i = months - 1; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var existing = grouped.FirstOrDefault(g => g.Year == date.Year && g.Month == date.Month);

                chartData.Add(new
                {
                    Label = $"{date.Month:D2}/{date.Year}",
                    Value = existing?.Revenue ?? 0
                });
            }

            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados do gráfico de receita");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }
}