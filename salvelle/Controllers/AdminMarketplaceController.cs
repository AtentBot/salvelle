using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Service.Marketplace;

namespace Controllers;

/// <summary>
/// Painel administrativo do marketplace — visão da plataforma.
/// Protegido por AdminAuthMiddleware.
/// </summary>
[ApiController]
[Route("api/admin/marketplace")]
public class AdminMarketplaceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CommissionService _commission;

    public AdminMarketplaceController(AppDbContext db, CommissionService commission)
    {
        _db = db;
        _commission = commission;
    }

    /// <summary>
    /// Dashboard geral do marketplace
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminMarketplaceDashboardDto>>> GetDashboard(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var endDate = to ?? DateTime.UtcNow;

        var activePharmacies = await _db.Establishments
            .CountAsync(e => e.IsMarketplaceActive);

        var totalOrders = await _db.OnlineOrders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .CountAsync();

        var totalRevenue = await _db.PlatformTransactions
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status != "ESTORNADO")
            .SumAsync(t => t.GrossAmount);

        var totalCommission = await _db.PlatformTransactions
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status != "ESTORNADO")
            .SumAsync(t => t.CommissionAmount);

        var totalCustomers = await _db.Customers
            .CountAsync(c => c.Status == "ATIVO");

        var avgRating = await _db.PharmacyRatings
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .AverageAsync(r => (decimal?)r.Rating) ?? 0;

        var ordersByStatus = await _db.OnlineOrders
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Top farmácias por receita
        var topPharmacies = await _db.PlatformTransactions
            .Include(t => t.Establishment)
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status != "ESTORNADO")
            .GroupBy(t => new { t.EstablishmentId, Name = t.Establishment!.NomeFantasia })
            .Select(g => new TopPharmacyDto
            {
                EstablishmentId = g.Key.EstablishmentId,
                Name = g.Key.Name,
                OrderCount = g.Count(),
                GrossRevenue = g.Sum(t => t.GrossAmount),
                CommissionEarned = g.Sum(t => t.CommissionAmount)
            })
            .OrderByDescending(p => p.GrossRevenue)
            .Take(10)
            .ToListAsync();

        // Receita diária da plataforma
        var dailyRevenue = await _db.PlatformTransactions
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status != "ESTORNADO")
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new AdminDailyRevenueDto
            {
                Date = g.Key,
                GrossRevenue = g.Sum(t => t.GrossAmount),
                CommissionRevenue = g.Sum(t => t.CommissionAmount),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return Ok(ApiResponse<AdminMarketplaceDashboardDto>.SuccessResponse(new AdminMarketplaceDashboardDto
        {
            ActivePharmacies = activePharmacies,
            TotalOrders = totalOrders,
            TotalGrossRevenue = totalRevenue,
            TotalPlatformCommission = totalCommission,
            TotalCustomers = totalCustomers,
            AverageRating = Math.Round(avgRating, 2),
            CancelledOrders = ordersByStatus.FirstOrDefault(s => s.Status == "CANCELLED")?.Count ?? 0,
            PendingOrders = ordersByStatus.FirstOrDefault(s => s.Status == "PENDING")?.Count ?? 0,
            TopPharmacies = topPharmacies,
            DailyRevenue = dailyRevenue
        }));
    }

    /// <summary>
    /// Listar farmácias do marketplace
    /// </summary>
    [HttpGet("pharmacies")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminPharmacyDto>>>> GetPharmacies(
        [FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 50);

        var query = _db.Establishments.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(e => e.NomeFantasia.ToLower().Contains(s) ||
                                      (e.RazaoSocial != null && e.RazaoSocial.ToLower().Contains(s)));
        }

        if (isActive.HasValue)
            query = query.Where(e => e.IsMarketplaceActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.IsMarketplaceActive)
            .ThenBy(e => e.NomeFantasia)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new AdminPharmacyDto
            {
                Id = e.Id,
                NomeFantasia = e.NomeFantasia,
                RazaoSocial = e.RazaoSocial,
                IsMarketplaceActive = e.IsMarketplaceActive,
                AcceptingOrders = e.AcceptingOrders,
                AverageRating = e.AverageRating,
                TotalRatings = e.TotalRatings,
                ProductCount = _db.CatalogProducts.Count(p => p.EstablishmentId == e.Id && p.IsActive),
                City = e.City,
                State = e.State
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<AdminPharmacyDto>>.SuccessResponse(
            new PaginatedResponse<AdminPharmacyDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }));
    }

    /// <summary>
    /// Ativar/desativar farmácia no marketplace — requer SUPER_ADMIN
    /// </summary>
    [HttpPut("pharmacies/{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse>> TogglePharmacy(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, ApiResponse.ErrorResponse("Acesso negado. Requer perfil SUPER_ADMIN."));

        var pharmacy = await _db.Establishments.FindAsync(id);
        if (pharmacy == null)
            return NotFound(ApiResponse.ErrorResponse("Farmácia não encontrada"));

        pharmacy.IsMarketplaceActive = !pharmacy.IsMarketplaceActive;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            pharmacy.IsMarketplaceActive ? "Farmácia ativada no marketplace" : "Farmácia desativada do marketplace"));
    }

    /// <summary>
    /// Resumo financeiro de uma farmácia específica
    /// </summary>
    [HttpGet("pharmacies/{id:guid}/financial")]
    public async Task<ActionResult<ApiResponse<PharmacyFinancialSummary>>> GetPharmacyFinancial(
        Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var summary = await _commission.GetPharmacyFinancialSummaryAsync(id, from, to);
        return Ok(ApiResponse<PharmacyFinancialSummary>.SuccessResponse(summary));
    }

    /// <summary>
    /// Histórico de comissões semanais (todas as farmácias)
    /// </summary>
    [HttpGet("commissions")]
    public async Task<ActionResult<ApiResponse<List<AdminCommissionDto>>>> GetAllCommissions(
        [FromQuery] int weeks = 4)
    {
        weeks = Math.Min(weeks, 52);
        var cutoff = DateTime.UtcNow.AddDays(-weeks * 7);

        var commissions = await _db.PlatformCommissions
            .Include(c => c.Establishment)
            .Where(c => c.WeekStartDate >= cutoff)
            .OrderByDescending(c => c.WeekStartDate)
            .ThenBy(c => c.Establishment!.NomeFantasia)
            .Select(c => new AdminCommissionDto
            {
                Id = c.Id,
                PharmacyName = c.Establishment != null ? c.Establishment.NomeFantasia : "",
                WeekStart = c.WeekStartDate,
                WeekEnd = c.WeekEndDate,
                SalesCount = c.TotalSalesCount,
                CommissionRate = c.CommissionRate,
                GrossAmount = c.TotalSalesAmount,
                CommissionAmount = c.TotalCommissionAmount,
                Status = c.Status
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AdminCommissionDto>>.SuccessResponse(commissions));
    }

    /// <summary>
    /// Forçar recálculo de comissões de uma semana — requer SUPER_ADMIN
    /// </summary>
    [HttpPost("commissions/recalculate")]
    public async Task<ActionResult<ApiResponse>> RecalculateCommissions([FromQuery] DateTime weekDate)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, ApiResponse.ErrorResponse("Acesso negado. Requer perfil SUPER_ADMIN."));

        var commissions = await _commission.CalculateWeeklyCommissionsAsync(weekDate);
        return Ok(ApiResponse.SuccessResponse($"Comissões recalculadas: {commissions.Count} farmácias"));
    }

    private bool IsSuperAdmin()
    {
        var role = HttpContext.Items["SaasAdminRole"] as string;
        return string.Equals(role, "SUPER_ADMIN", StringComparison.Ordinal);
    }
}

// ==================== ADMIN DTOs ====================

public class AdminMarketplaceDashboardDto
{
    public int ActivePharmacies { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalPlatformCommission { get; set; }
    public int TotalCustomers { get; set; }
    public decimal AverageRating { get; set; }
    public int CancelledOrders { get; set; }
    public int PendingOrders { get; set; }
    public List<TopPharmacyDto> TopPharmacies { get; set; } = new();
    public List<AdminDailyRevenueDto> DailyRevenue { get; set; } = new();
}

public class TopPharmacyDto
{
    public Guid EstablishmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal CommissionEarned { get; set; }
}

public class AdminDailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal CommissionRevenue { get; set; }
    public int OrderCount { get; set; }
}

public class AdminPharmacyDto
{
    public Guid Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public bool IsMarketplaceActive { get; set; }
    public bool AcceptingOrders { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int ProductCount { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class AdminCommissionDto
{
    public Guid Id { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int SalesCount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
