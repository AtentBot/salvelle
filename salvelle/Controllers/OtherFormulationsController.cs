using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Service.PharmaceuticalForms;

namespace Controllers.Api;

/// <summary>
/// API Controller para Outras Formas Farmacêuticas
/// (Soluções, Xaropes, Sachês, etc.)
/// Rota: /api/OtherFormulations/*
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OtherFormulationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly LiquidFormulationService _liquidService;
    private readonly PowderFormulationService _powderService;
    private readonly ILogger<OtherFormulationsController> _logger;

    public OtherFormulationsController(
        AppDbContext context,
        LiquidFormulationService liquidService,
        PowderFormulationService powderService,
        ILogger<OtherFormulationsController> logger)
    {
        _context = context;
        _liquidService = liquidService;
        _powderService = powderService;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FORMULAÇÕES LÍQUIDAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula uma formulação líquida (solução, xarope, etc.)
    /// POST /api/OtherFormulations/liquid/calculate
    /// </summary>
    [HttpPost("liquid/calculate")]
    public async Task<ActionResult<LiquidCalculationResultDto>> CalculateLiquid([FromBody] LiquidCalculationRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Estabelecimento não identificado" });

        var result = await _liquidService.CalculateAsync(request, establishmentId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Lista veículos disponíveis para uma forma líquida
    /// GET /api/OtherFormulations/liquid/vehicles?formId={formId}
    /// </summary>
    [HttpGet("liquid/vehicles")]
    public async Task<ActionResult<List<VehicleSubtypeDto>>> GetLiquidVehicles([FromQuery] Guid formId)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var vehicles = await _liquidService.GetAvailableVehiclesAsync(formId, establishmentId);
        return Ok(vehicles);
    }

    /// <summary>
    /// Lista todas as formas líquidas disponíveis
    /// GET /api/OtherFormulations/liquid/forms
    /// </summary>
    [HttpGet("liquid/forms")]
    public async Task<ActionResult<List<FormTypeDto>>> GetLiquidForms()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var liquidCodes = new[] { "SOLUCAO", "XAROPE", "SUSPENSAO", "ELIXIR", "GOTAS", "TINTURA" };

        var forms = await _context.PharmaceuticalForms
            .Where(f => f.EstablishmentId == establishmentId && f.IsActive && liquidCodes.Contains(f.Code))
            .OrderBy(f => f.SortOrder)
            .Select(f => new FormTypeDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                DefaultUnit = f.DefaultUnit ?? "mL",
                DefaultValidityDays = f.DefaultValidityDays,
                MinimumPrice = f.MinimumPrice,
                Icon = f.Icon ?? "bi-droplet"
            })
            .ToListAsync();

        return Ok(forms);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SACHÊS E PÓS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula uma formulação de sachê/pó
    /// POST /api/OtherFormulations/powder/calculate
    /// </summary>
    [HttpPost("powder/calculate")]
    public async Task<ActionResult<PowderCalculationResultDto>> CalculatePowder([FromBody] PowderCalculationRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Estabelecimento não identificado" });

        var result = await _powderService.CalculateAsync(request, establishmentId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Lista excipientes comuns para sachês
    /// GET /api/OtherFormulations/powder/excipients
    /// </summary>
    [HttpGet("powder/excipients")]
    public async Task<ActionResult<List<ExcipientOptionDto>>> GetPowderExcipients()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var excipients = await _powderService.GetCommonExcipientsAsync(establishmentId);
        return Ok(excipients);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FORMAS DISPONÍVEIS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista todas as formas farmacêuticas disponíveis organizadas por tipo
    /// GET /api/OtherFormulations/all-forms
    /// </summary>
    [HttpGet("all-forms")]
    public async Task<ActionResult<AllFormsDto>> GetAllForms()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var allForms = await _context.PharmaceuticalForms
            .Where(f => f.EstablishmentId == establishmentId && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FormTypeDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                DefaultUnit = f.DefaultUnit ?? "un",
                DefaultValidityDays = f.DefaultValidityDays,
                MinimumPrice = f.MinimumPrice,
                Icon = f.Icon ?? "bi-capsule"
            })
            .ToListAsync();

        // Organizar por categoria
        var solidCodes = new[] { "CAPSULA", "COMPRIMIDO", "PASTILHA" };
        var semiSolidCodes = new[] { "CREME", "GEL", "POMADA", "PASTA", "LOCAO" };
        var liquidCodes = new[] { "SOLUCAO", "XAROPE", "SUSPENSAO", "ELIXIR", "GOTAS", "TINTURA" };
        var powderCodes = new[] { "PO", "SACHE", "PAPEL", "GRANULADO" };
        var otherCodes = new[] { "SUPOSITORIO", "OVULO", "CAPSULA_VAGINAL", "SPRAY", "AEROSOL" };

        return Ok(new AllFormsDto
        {
            Solids = allForms.Where(f => solidCodes.Contains(f.Code)).ToList(),
            SemiSolids = allForms.Where(f => semiSolidCodes.Contains(f.Code)).ToList(),
            Liquids = allForms.Where(f => liquidCodes.Contains(f.Code)).ToList(),
            Powders = allForms.Where(f => powderCodes.Contains(f.Code)).ToList(),
            Others = allForms.Where(f => otherCodes.Contains(f.Code) || 
                (!solidCodes.Contains(f.Code) && !semiSolidCodes.Contains(f.Code) && 
                 !liquidCodes.Contains(f.Code) && !powderCodes.Contains(f.Code))).ToList()
        });
    }

    /// <summary>
    /// Busca matérias-primas para uso em formulações
    /// GET /api/OtherFormulations/raw-materials/search?q={query}&usage={ORAL|TOPICAL}
    /// </summary>
    [HttpGet("raw-materials/search")]
    public async Task<ActionResult<List<RawMaterialSearchResultDto>>> SearchRawMaterials(
        [FromQuery] string q,
        [FromQuery] string? usage = null,
        [FromQuery] int limit = 20)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<RawMaterialSearchResultDto>());

        var query = _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .Where(r => r.Name.ToLower().Contains(q.ToLower())
                     || (r.DcbCode != null && r.DcbCode.ToLower().Contains(q.ToLower()))
                     || (r.Synonyms != null && r.Synonyms.ToLower().Contains(q.ToLower())));

        if (!string.IsNullOrEmpty(usage))
        {
            query = query.Where(r => r.AllowedUsage == "BOTH" || r.AllowedUsage == usage);
        }

        var results = await query
            .OrderByDescending(r => r.Popularity)
            .ThenBy(r => r.Name)
            .Take(limit)
            .Select(r => new RawMaterialSearchResultDto
            {
                Id = r.Id,
                Name = r.Name,
                DcbCode = r.DcbCode,
                Category = r.Category,
                Unit = r.Unit ?? "g",
                AllowedUsage = r.AllowedUsage,
                BasePrice = r.BasePrice,
                BulkDensity = r.BulkDensity,
                CorrectionFactor = r.CorrectionFactor,
                PurityFactor = r.PurityFactor,
                DilutionFactor = r.DilutionFactor,
                IsControlled = r.ControlType != "COMUM" && !string.IsNullOrEmpty(r.ControlType),
                ControlType = r.ControlType
            })
            .ToListAsync();

        return Ok(results);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs ADICIONAIS
// ═══════════════════════════════════════════════════════════════════════════════

public class FormTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DefaultUnit { get; set; } = "un";
    public int DefaultValidityDays { get; set; }
    public decimal MinimumPrice { get; set; }
    public string Icon { get; set; } = "bi-capsule";
}

public class AllFormsDto
{
    public List<FormTypeDto> Solids { get; set; } = new();
    public List<FormTypeDto> SemiSolids { get; set; } = new();
    public List<FormTypeDto> Liquids { get; set; } = new();
    public List<FormTypeDto> Powders { get; set; } = new();
    public List<FormTypeDto> Others { get; set; } = new();
}

public class RawMaterialSearchResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "g";
    public string? AllowedUsage { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? BulkDensity { get; set; }
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
    public decimal? DilutionFactor { get; set; }
    public bool IsControlled { get; set; }
    public string? ControlType { get; set; }
}
