using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Models;
using System.Text.Json;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionPlansController> _logger;

    public SubscriptionPlansController(AppDbContext context, ILogger<SubscriptionPlansController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = _context.Set<SubscriptionPlan>().AsQueryable();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var plans = await query
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                MaxEmployees = p.MaxEmployees,
                MaxMonthlyOrders = p.MaxMonthlyOrders,
                Features = JsonSerializer.Deserialize<Dictionary<string, bool>>(p.Features) ?? new(),
                IsActive = p.IsActive
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _context.Set<SubscriptionPlan>()
            .Where(p => p.Id == id)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                MaxEmployees = p.MaxEmployees,
                MaxMonthlyOrders = p.MaxMonthlyOrders,
                Features = JsonSerializer.Deserialize<Dictionary<string, bool>>(p.Features) ?? new(),
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync();

        if (plan == null)
            return NotFound(new { message = "Plano não encontrado" });

        return Ok(plan);
    }
}
