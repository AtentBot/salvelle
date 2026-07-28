using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service.CustomerFormulas;
using Models.Pharmacy;

namespace Controllers.Api;

/// <summary>
/// API para autocomplete de ingredientes com preços
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IngredientsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PricingService _pricingService;
    private readonly ILogger<IngredientsController> _logger;

    public IngredientsController(
        AppDbContext context,
        PricingService pricingService,
        ILogger<IngredientsController> logger)
    {
        _context = context;
        _pricingService = pricingService;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var value) && value is Guid id)
            return id;
        return Guid.Empty;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? category = null,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { success = true, data = new List<object>() });

        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Sessao invalida" });

        limit = Math.Clamp(limit, 1, 50);
        var searchTerm = q.ToLower().Trim();

        var query = _context.RawMaterials
            .Where(rm => rm.EstablishmentId == establishmentId && rm.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(rm => rm.Category == category);

        var ingredients = await query
            .Where(rm => 
                rm.Name.ToLower().Contains(searchTerm) ||
                (rm.Synonyms != null && rm.Synonyms.ToLower().Contains(searchTerm)) ||
                (rm.Category != null && rm.Category.ToLower().Contains(searchTerm)) ||
                (rm.DcbCode != null && rm.DcbCode.ToLower().Contains(searchTerm)) ||
                (rm.CasNumber != null && rm.CasNumber.ToLower().Contains(searchTerm)))
            .OrderByDescending(rm => rm.Popularity)
            .ThenBy(rm => rm.Name)
            .Take(limit)
            .ToListAsync();

        var results = ingredients.Select(ing => {
            decimal price;
            string priceSource;
            decimal confidence;

            // LastKnownPrice = último preço pago (maior confiança)
            // BasePrice = preço base de referência (menor confiança)
            if (ing.LastKnownPrice.HasValue && ing.LastKnownPrice.Value > 0)
            {
                price = ing.LastKnownPrice.Value;
                priceSource = "ESTOQUE";
                confidence = 1.0m;
            }
            else if (ing.BasePrice.HasValue && ing.BasePrice.Value > 0)
            {
                price = ing.BasePrice.Value;
                priceSource = "BASE";
                confidence = 0.5m;
            }
            else
            {
                price = 0.50m;
                priceSource = "ESTIMADO";
                confidence = 0.3m;
            }

            return new
            {
                id = ing.Id,
                name = ing.Name,
                category = ing.Category ?? "",
                unit = ing.Unit,
                dcbCode = ing.DcbCode,
                synonyms = ing.Synonyms,
                price,
                priceSource,
                confidence,
                inStock = ing.CurrentStock > 0,
                availableStock = ing.CurrentStock,
                isControlled = !string.IsNullOrEmpty(ing.ControlType) && ing.ControlType != "COMUM",
                controlType = ing.ControlType,
                popularity = ing.Popularity
            };
        }).ToList();

        return Ok(new { success = true, data = results });
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int limit = 6)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Sessao invalida" });

        limit = Math.Clamp(limit, 1, 20);

        var popular = await _context.RawMaterials
            .Where(rm => rm.EstablishmentId == establishmentId && rm.IsActive && rm.CurrentStock > 0)
            .OrderByDescending(rm => rm.Popularity)
            .ThenBy(rm => rm.Name)
            .Take(limit)
            .ToListAsync();

        var results = popular.Select(rm => {
            decimal price = rm.LastKnownPrice ?? rm.BasePrice ?? 0.50m;
            string priceSource = rm.LastKnownPrice.HasValue ? "ESTOQUE" : 
                                rm.BasePrice.HasValue ? "BASE" : "ESTIMADO";

            return new
            {
                id = rm.Id,
                name = rm.Name,
                category = rm.Category ?? "",
                unit = rm.Unit,
                dcbCode = rm.DcbCode,
                price,
                priceSource,
                inStock = rm.CurrentStock > 0,
                availableStock = rm.CurrentStock,
                isControlled = !string.IsNullOrEmpty(rm.ControlType) && rm.ControlType != "COMUM",
                controlType = rm.ControlType,
                popularity = rm.Popularity
            };
        }).ToList();

        return Ok(new { success = true, data = results });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Sessao invalida" });

        var categories = await _context.RawMaterials
            .Where(rm => rm.EstablishmentId == establishmentId && rm.IsActive && rm.Category != null)
            .GroupBy(rm => rm.Category)
            .Select(g => new
            {
                name = g.Key ?? "Outros",
                count = g.Count(),
                inStockCount = g.Count(rm => rm.CurrentStock > 0)
            })
            .OrderBy(c => c.name)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Sessao invalida" });

        var ingredient = await _context.RawMaterials
            .FirstOrDefaultAsync(rm => rm.Id == id && rm.EstablishmentId == establishmentId);

        if (ingredient == null)
            return NotFound(new { success = false, error = "Ingrediente não encontrado" });

        decimal price = ingredient.LastKnownPrice ?? ingredient.BasePrice ?? 0.50m;
        string priceSource = ingredient.LastKnownPrice.HasValue ? "ESTOQUE" : 
                            ingredient.BasePrice.HasValue ? "BASE" : "ESTIMADO";

        var detail = new
        {
            id = ingredient.Id,
            name = ingredient.Name,
            category = ingredient.Category ?? "",
            unit = ingredient.Unit,
            dcbCode = ingredient.DcbCode,
            casNumber = ingredient.CasNumber,
            synonyms = ingredient.Synonyms,
            description = ingredient.Description,
            price,
            priceSource,
            currentStock = ingredient.CurrentStock,
            minimumStock = ingredient.MinimumStock,
            inStock = ingredient.CurrentStock > 0,
            isControlled = !string.IsNullOrEmpty(ingredient.ControlType) && ingredient.ControlType != "COMUM",
            controlType = ingredient.ControlType,
            requiresPrescription = !string.IsNullOrEmpty(ingredient.ControlType) && ingredient.ControlType != "COMUM",
            storageConditions = ingredient.StorageConditions,
            popularity = ingredient.Popularity
        };

        return Ok(new { success = true, data = detail });
    }

    [HttpPost("batch")]
    public async Task<IActionResult> GetBatch([FromBody] BatchIngredientRequest request)
    {
        if (request.Ids == null || !request.Ids.Any())
            return Ok(new { success = true, data = new List<object>() });

        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Sessao invalida" });

        var ingredients = await _context.RawMaterials
            .Where(rm => request.Ids.Contains(rm.Id) && rm.EstablishmentId == establishmentId)
            .ToListAsync();

        var results = ingredients.Select(rm => {
            decimal price = rm.LastKnownPrice ?? rm.BasePrice ?? 0.50m;
            string priceSource = rm.LastKnownPrice.HasValue ? "ESTOQUE" : 
                                rm.BasePrice.HasValue ? "BASE" : "ESTIMADO";

            return new
            {
                id = rm.Id,
                name = rm.Name,
                category = rm.Category ?? "",
                unit = rm.Unit,
                dcbCode = rm.DcbCode,
                price,
                priceSource,
                inStock = rm.CurrentStock > 0,
                availableStock = rm.CurrentStock,
                isControlled = !string.IsNullOrEmpty(rm.ControlType) && rm.ControlType != "COMUM",
                controlType = rm.ControlType,
                popularity = rm.Popularity
            };
        }).ToList();

        return Ok(new { success = true, data = results });
    }
}

public class BatchIngredientRequest
{
    public List<Guid> Ids { get; set; } = new();
}
