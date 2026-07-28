using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Controllers.Api;

/// <summary>
/// Catálogo GLOBAL compartilhado de matérias-primas (read-only para funcionários).
/// Cada farmácia importa do catálogo para sua tabela RawMaterials via
/// POST /api/RawMaterials/import-from-catalog.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RawMaterialsCatalogController : ControllerBase
{
    private readonly AppDbContext _db;

    public RawMaterialsCatalogController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Autocomplete por nome/sinônimos/dcb (mínimo 2 caracteres).
    /// GET /api/RawMaterialsCatalog/search?q=vitamina&category=&usage=&limit=20
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? category = null,
        [FromQuery] string? usage = null,
        [FromQuery] int limit = 20)
    {
        if (HttpContext.Items["Employee"] is not Employee)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new { success = true, data = Array.Empty<object>() });

        if (limit <= 0 || limit > 50) limit = 20;

        var term = q.Trim().ToLower();

        var query = _db.RawMaterialsCatalog
            .Where(c => c.IsActive)
            .Where(c => c.Name.ToLower().Contains(term)
                     || (c.Synonyms != null && c.Synonyms.ToLower().Contains(term))
                     || (c.DcbCode != null && c.DcbCode.ToLower().Contains(term))
                     || (c.CasNumber != null && c.CasNumber.ToLower().Contains(term)));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(usage))
        {
            var u = usage.Trim().ToUpperInvariant();
            query = query.Where(c => c.AllowedUsage == "BOTH" || c.AllowedUsage == u);
        }

        var results = await query
            .OrderByDescending(c => c.Popularity)
            .ThenBy(c => c.Name)
            .Take(limit)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.DcbCode,
                c.CasNumber,
                c.Category,
                c.ControlType,
                c.AllowedUsage,
                c.PhysicalState,
                c.Unit,
                c.Synonyms,
                c.Indications,
                c.Popularity
            })
            .ToListAsync();

        return Ok(new { success = true, data = results });
    }

    /// <summary>
    /// Listagem paginada do catálogo.
    /// GET /api/RawMaterialsCatalog?q=&category=&usage=&page=1&pageSize=50
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] string? category = null,
        [FromQuery] string? usage = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (HttpContext.Items["Employee"] is not Employee)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (page < 1) page = 1;
        if (pageSize is <= 0 or > 200) pageSize = 50;

        var query = _db.RawMaterialsCatalog.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term)
                                  || (c.Synonyms != null && c.Synonyms.ToLower().Contains(term))
                                  || (c.DcbCode != null && c.DcbCode.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(usage))
        {
            var u = usage.Trim().ToUpperInvariant();
            query = query.Where(c => c.AllowedUsage == "BOTH" || c.AllowedUsage == u);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.Popularity)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.DcbCode,
                c.CasNumber,
                c.Category,
                c.ControlType,
                c.AllowedUsage,
                c.PhysicalState,
                c.Unit,
                c.Synonyms,
                c.Indications,
                c.Popularity
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = items,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalRecords = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    /// <summary>
    /// Lista de categorias disponíveis no catálogo (para filtros).
    /// GET /api/RawMaterialsCatalog/categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        if (HttpContext.Items["Employee"] is not Employee)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var categories = await _db.RawMaterialsCatalog
            .Where(c => c.IsActive && !string.IsNullOrEmpty(c.Category))
            .GroupBy(c => c.Category)
            .Select(g => new { category = g.Key, count = g.Count() })
            .OrderBy(x => x.category)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    /// <summary>
    /// Detalhe de um item do catálogo.
    /// GET /api/RawMaterialsCatalog/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (HttpContext.Items["Employee"] is not Employee)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        var item = await _db.RawMaterialsCatalog
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.DcbCode,
                c.CasNumber,
                c.Category,
                c.ControlType,
                c.AllowedUsage,
                c.PhysicalState,
                c.Unit,
                c.DefaultPurityFactor,
                c.DefaultCorrectionFactor,
                c.Synonyms,
                c.Indications,
                c.Popularity
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { error = "Item não encontrado no catálogo" });

        return Ok(new { success = true, data = item });
    }
}
