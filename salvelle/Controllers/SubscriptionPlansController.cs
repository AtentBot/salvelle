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
        try
        {
            var query = _context.Set<SubscriptionPlan>().AsQueryable();

            if (activeOnly)
                query = query.Where(p => p.IsActive);

            var plans = await query
                .OrderBy(p => p.PriceMonthly)
                .ToListAsync();

            var result = plans.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                MaxEmployees = p.MaxEmployees,
                MaxMonthlyOrders = p.MaxMonthlyOrders,
                Features = ParseFeatures(p.Features),
                IsActive = p.IsActive
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar planos");
            return StatusCode(500, new { message = "Erro ao buscar planos" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var plan = await _context.Set<SubscriptionPlan>()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            var result = new SubscriptionPlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                PriceMonthly = plan.PriceMonthly,
                PriceYearly = plan.PriceYearly,
                MaxEmployees = plan.MaxEmployees,
                MaxMonthlyOrders = plan.MaxMonthlyOrders,
                Features = ParseFeatures(plan.Features),
                IsActive = plan.IsActive
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar plano" });
        }
    }

    /// <summary>
    /// Parseia o campo Features de forma robusta, tratando diferentes formatos
    /// </summary>
    private Dictionary<string, bool> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
            return new Dictionary<string, bool>();

        try
        {
            // Tentar primeiro como Dictionary<string, bool>
            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(featuresJson);
            if (dict != null)
                return dict;
        }
        catch
        {
            // Ignorar e tentar outros formatos
        }

        try
        {
            // Tentar como array de strings ["feature1", "feature2"]
            var array = JsonSerializer.Deserialize<string[]>(featuresJson);
            if (array != null)
            {
                return array.ToDictionary(f => f, f => true);
            }
        }
        catch
        {
            // Ignorar e tentar outros formatos
        }

        try
        {
            // Tentar como lista de objetos [{"name": "feature1", "enabled": true}]
            var list = JsonSerializer.Deserialize<List<FeatureItem>>(featuresJson);
            if (list != null)
            {
                return list.ToDictionary(f => f.Name ?? "", f => f.Enabled);
            }
        }
        catch
        {
            // Ignorar
        }

        // Se não conseguiu parsear, retornar dicionário vazio
        _logger.LogWarning("Não foi possível parsear Features: {Features}", featuresJson);
        return new Dictionary<string, bool>();
    }

    private class FeatureItem
    {
        public string? Name { get; set; }
        public bool Enabled { get; set; }
    }
}