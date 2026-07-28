using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Service.PharmaceuticalForms;

namespace Controllers.Api;

/// <summary>
/// API Controller para Formulações Tópicas (Cremes, Géis, Pomadas, Loções)
/// Rota: /api/TopicalFormulations/*
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TopicalFormulationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TopicalFormulationService _formulationService;
    private readonly ILogger<TopicalFormulationsController> _logger;

    public TopicalFormulationsController(
        AppDbContext context,
        TopicalFormulationService formulationService,
        ILogger<TopicalFormulationsController> logger)
    {
        _context = context;
        _formulationService = formulationService;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CÁLCULO DE FORMULAÇÃO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula uma formulação tópica completa
    /// POST /api/TopicalFormulations/calculate
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<TopicalCalculationResultDto>> Calculate([FromBody] TopicalCalculationRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { message = "Estabelecimento não identificado" });

        var result = await _formulationService.CalculateAsync(request, establishmentId);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Cálculo rápido apenas com percentuais (sem base definida)
    /// POST /api/TopicalFormulations/quick-calculate
    /// </summary>
    [HttpPost("quick-calculate")]
    public ActionResult<QuickCalculationResultDto> QuickCalculate([FromBody] QuickCalculationRequestDto request)
    {
        if (request.TotalQuantity <= 0)
            return BadRequest(new { message = "Quantidade deve ser maior que zero" });

        var results = new List<QuickCalculationItemDto>();
        decimal totalPercentage = 0;

        foreach (var item in request.Items)
        {
            var quantityGrams = request.TotalQuantity * (item.Percentage / 100m);
            totalPercentage += item.Percentage;

            results.Add(new QuickCalculationItemDto
            {
                Name = item.Name,
                Percentage = item.Percentage,
                QuantityGrams = Math.Round(quantityGrams, 4),
                QuantityDisplay = FormatQuantity(quantityGrams)
            });
        }

        // Adicionar QSP se total < 100%
        if (totalPercentage < 100)
        {
            var qspPercentage = 100 - totalPercentage;
            var qspQuantity = request.TotalQuantity * (qspPercentage / 100m);

            results.Add(new QuickCalculationItemDto
            {
                Name = "Base/Veículo (QSP)",
                Percentage = qspPercentage,
                QuantityGrams = Math.Round(qspQuantity, 4),
                QuantityDisplay = FormatQuantity(qspQuantity),
                IsQsp = true
            });
        }

        return Ok(new QuickCalculationResultDto
        {
            TotalQuantity = request.TotalQuantity,
            TotalUnit = request.TotalUnit,
            TotalPercentage = totalPercentage,
            IsValid = totalPercentage <= 100,
            Items = results,
            Message = totalPercentage > 100 
                ? $"⚠️ Total de ativos ({totalPercentage:F1}%) excede 100%" 
                : $"✅ Formulação válida - QSP: {100 - totalPercentage:F1}%"
        });
    }

    private string FormatQuantity(decimal grams)
    {
        if (grams >= 1000)
            return $"{grams / 1000:F2} kg";
        if (grams >= 1)
            return $"{grams:F2} g";
        return $"{grams * 1000:F1} mg";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BASES DISPONÍVEIS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista bases disponíveis para uma forma farmacêutica
    /// GET /api/TopicalFormulations/bases?formId={formId}
    /// </summary>
    [HttpGet("bases")]
    public async Task<ActionResult<List<BaseSubtypeDto>>> GetBases([FromQuery] Guid formId)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var bases = await _formulationService.GetAvailableBasesAsync(formId, establishmentId);
        return Ok(bases);
    }

    /// <summary>
    /// Lista todas as bases de formulações tópicas (Cremes, Géis, Pomadas)
    /// GET /api/TopicalFormulations/bases/all
    /// </summary>
    [HttpGet("bases/all")]
    public async Task<ActionResult<List<BaseWithFormDto>>> GetAllTopicalBases()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        // Códigos de formas tópicas
        var topicalFormCodes = new[] { "CREME", "GEL", "POMADA", "LOCAO", "PASTA" };

        var bases = await _context.PharmaceuticalFormSubtypes
            .Include(s => s.PharmaceuticalForm)
            .Where(s => s.EstablishmentId == establishmentId 
                     && s.IsActive 
                     && topicalFormCodes.Contains(s.PharmaceuticalForm!.Code))
            .OrderBy(s => s.PharmaceuticalForm!.Name)
            .ThenBy(s => s.SortOrder)
            .Select(s => new BaseWithFormDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                FormId = s.PharmaceuticalFormId,
                FormCode = s.PharmaceuticalForm!.Code,
                FormName = s.PharmaceuticalForm.Name,
                IsDefault = s.IsDefault,
                YieldQuantity = s.YieldQuantity,
                YieldUnit = s.YieldUnit,
                MinimumPrice = s.MinimumPrice,
                BaseCost = s.BaseCost
            })
            .ToListAsync();

        return Ok(bases);
    }

    /// <summary>
    /// Busca composição detalhada de uma base
    /// GET /api/TopicalFormulations/bases/{id}/composition
    /// </summary>
    [HttpGet("bases/{id}/composition")]
    public async Task<ActionResult<BaseCompositionDetailDto>> GetBaseComposition(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var composition = await _formulationService.GetBaseCompositionAsync(id, establishmentId);
        
        if (composition == null)
            return NotFound(new { message = "Base não encontrada" });

        return Ok(composition);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CRUD DE BASES (SUBTIPOS)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cria uma nova base
    /// POST /api/TopicalFormulations/bases
    /// </summary>
    [HttpPost("bases")]
    public async Task<ActionResult<BaseSubtypeDto>> CreateBase([FromBody] CreateBaseDto dto)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        // Verificar se forma existe
        var form = await _context.PharmaceuticalForms
            .FirstOrDefaultAsync(f => f.Id == dto.PharmaceuticalFormId && f.EstablishmentId == establishmentId);

        if (form == null)
            return BadRequest(new { message = "Forma farmacêutica não encontrada" });

        // Verificar código único
        var codeExists = await _context.PharmaceuticalFormSubtypes
            .AnyAsync(s => s.EstablishmentId == establishmentId 
                        && s.PharmaceuticalFormId == dto.PharmaceuticalFormId 
                        && s.Code == dto.Code);

        if (codeExists)
            return BadRequest(new { message = $"Código '{dto.Code}' já existe para esta forma" });

        var subtype = new Models.Pharmacy.PharmaceuticalFormSubtype
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            PharmaceuticalFormId = dto.PharmaceuticalFormId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true,
            IsDefault = dto.IsDefault,
            YieldQuantity = dto.YieldQuantity,
            YieldUnit = dto.YieldUnit,
            MinimumPrice = dto.MinimumPrice,
            BaseCost = dto.BaseCost,
            ValidityDays = dto.ValidityDays,
            PreparationInstructions = dto.PreparationInstructions,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Se marcado como default, desmarcar outros
        if (dto.IsDefault)
        {
            var otherDefaults = await _context.PharmaceuticalFormSubtypes
                .Where(s => s.PharmaceuticalFormId == dto.PharmaceuticalFormId && s.IsDefault)
                .ToListAsync();

            foreach (var other in otherDefaults)
                other.IsDefault = false;
        }

        _context.PharmaceuticalFormSubtypes.Add(subtype);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Base criada: {Name} ({Code}) para forma {Form}", 
            dto.Name, dto.Code, form.Name);

        return CreatedAtAction(nameof(GetBaseComposition), new { id = subtype.Id }, new BaseSubtypeDto
        {
            Id = subtype.Id,
            Code = subtype.Code,
            Name = subtype.Name,
            Description = subtype.Description,
            IsDefault = subtype.IsDefault,
            YieldQuantity = subtype.YieldQuantity,
            YieldUnit = subtype.YieldUnit,
            MinimumPrice = subtype.MinimumPrice
        });
    }

    /// <summary>
    /// Atualiza uma base
    /// PUT /api/TopicalFormulations/bases/{id}
    /// </summary>
    [HttpPut("bases/{id}")]
    public async Task<ActionResult> UpdateBase(Guid id, [FromBody] UpdateBaseDto dto)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var subtype = await _context.PharmaceuticalFormSubtypes
            .FirstOrDefaultAsync(s => s.Id == id && s.EstablishmentId == establishmentId);

        if (subtype == null)
            return NotFound();

        if (!string.IsNullOrEmpty(dto.Name))
            subtype.Name = dto.Name;

        if (dto.Description != null)
            subtype.Description = dto.Description;

        if (dto.YieldQuantity.HasValue)
            subtype.YieldQuantity = dto.YieldQuantity.Value;

        if (!string.IsNullOrEmpty(dto.YieldUnit))
            subtype.YieldUnit = dto.YieldUnit;

        if (dto.MinimumPrice.HasValue)
            subtype.MinimumPrice = dto.MinimumPrice.Value;

        if (dto.BaseCost.HasValue)
            subtype.BaseCost = dto.BaseCost.Value;

        if (dto.ValidityDays.HasValue)
            subtype.ValidityDays = dto.ValidityDays.Value;

        if (dto.PreparationInstructions != null)
            subtype.PreparationInstructions = dto.PreparationInstructions;

        if (dto.IsActive.HasValue)
            subtype.IsActive = dto.IsActive.Value;

        if (dto.SortOrder.HasValue)
            subtype.SortOrder = dto.SortOrder.Value;

        subtype.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Remove uma base
    /// DELETE /api/TopicalFormulations/bases/{id}
    /// </summary>
    [HttpDelete("bases/{id}")]
    public async Task<ActionResult> DeleteBase(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var subtype = await _context.PharmaceuticalFormSubtypes
            .Include(s => s.Compositions)
            .FirstOrDefaultAsync(s => s.Id == id && s.EstablishmentId == establishmentId);

        if (subtype == null)
            return NotFound();

        // Verificar se está em uso (seria necessário verificar em formulas/orders)
        // Por segurança, apenas desativar em vez de deletar
        subtype.IsActive = false;
        subtype.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Base desativada: {Name} ({Id})", subtype.Name, id);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COMPOSIÇÃO DE BASES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adiciona componente à composição de uma base
    /// POST /api/TopicalFormulations/bases/{id}/compositions
    /// </summary>
    [HttpPost("bases/{id}/compositions")]
    public async Task<ActionResult> AddComposition(Guid id, [FromBody] AddCompositionDto dto)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var subtype = await _context.PharmaceuticalFormSubtypes
            .FirstOrDefaultAsync(s => s.Id == id && s.EstablishmentId == establishmentId);

        if (subtype == null)
            return NotFound(new { message = "Base não encontrada" });

        // Verificar se matéria-prima existe
        var rawMaterial = await _context.RawMaterials
            .FirstOrDefaultAsync(r => r.Id == dto.RawMaterialId && r.EstablishmentId == establishmentId);

        if (rawMaterial == null)
            return BadRequest(new { message = "Matéria-prima não encontrada" });

        // Verificar se já existe na composição
        var exists = await _context.PharmaceuticalFormCompositions
            .AnyAsync(c => c.SubtypeId == id && c.RawMaterialId == dto.RawMaterialId);

        if (exists)
            return BadRequest(new { message = "Matéria-prima já está na composição" });

        var composition = new Models.Pharmacy.PharmaceuticalFormComposition
        {
            Id = Guid.NewGuid(),
            SubtypeId = id,
            RawMaterialId = dto.RawMaterialId,
            Percentage = dto.Percentage,
            QuantityPerYield = dto.QuantityPerYield,
            Unit = dto.Unit ?? "g",
            IsQsp = dto.IsQsp,
            IsOptional = dto.IsOptional,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PharmaceuticalFormCompositions.Add(composition);
        await _context.SaveChangesAsync();

        return Ok(new { 
            id = composition.Id, 
            message = $"Componente '{rawMaterial.Name}' adicionado à base" 
        });
    }

    /// <summary>
    /// Atualiza componente da composição
    /// PUT /api/TopicalFormulations/compositions/{compositionId}
    /// </summary>
    [HttpPut("compositions/{compositionId}")]
    public async Task<ActionResult> UpdateComposition(Guid compositionId, [FromBody] UpdateCompositionDto dto)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var composition = await _context.PharmaceuticalFormCompositions
            .Include(c => c.Subtype)
            .FirstOrDefaultAsync(c => c.Id == compositionId && c.Subtype!.EstablishmentId == establishmentId);

        if (composition == null)
            return NotFound();

        if (dto.Percentage.HasValue)
            composition.Percentage = dto.Percentage.Value;

        if (dto.QuantityPerYield.HasValue)
            composition.QuantityPerYield = dto.QuantityPerYield.Value;

        if (!string.IsNullOrEmpty(dto.Unit))
            composition.Unit = dto.Unit;

        if (dto.IsQsp.HasValue)
            composition.IsQsp = dto.IsQsp.Value;

        if (dto.IsOptional.HasValue)
            composition.IsOptional = dto.IsOptional.Value;

        if (dto.SortOrder.HasValue)
            composition.SortOrder = dto.SortOrder.Value;

        composition.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Remove componente da composição
    /// DELETE /api/TopicalFormulations/compositions/{compositionId}
    /// </summary>
    [HttpDelete("compositions/{compositionId}")]
    public async Task<ActionResult> DeleteComposition(Guid compositionId)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        var composition = await _context.PharmaceuticalFormCompositions
            .Include(c => c.Subtype)
            .FirstOrDefaultAsync(c => c.Id == compositionId && c.Subtype!.EstablishmentId == establishmentId);

        if (composition == null)
            return NotFound();

        _context.PharmaceuticalFormCompositions.Remove(composition);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MATÉRIAS-PRIMAS (BUSCA)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Busca matérias-primas para uso em formulações tópicas
    /// GET /api/TopicalFormulations/raw-materials/search?q={query}
    /// </summary>
    [HttpGet("raw-materials/search")]
    public async Task<ActionResult<List<RawMaterialSearchDto>>> SearchRawMaterials(
        [FromQuery] string q,
        [FromQuery] string? usage = null,
        [FromQuery] int limit = 20)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<RawMaterialSearchDto>());

        var query = _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .Where(r => r.Name.ToLower().Contains(q.ToLower()) 
                     || (r.DcbCode != null && r.DcbCode.ToLower().Contains(q.ToLower()))
                     || (r.CasNumber != null && r.CasNumber.ToLower().Contains(q.ToLower()))
                     || (r.Synonyms != null && r.Synonyms.ToLower().Contains(q.ToLower())));

        // Filtrar por uso permitido
        if (!string.IsNullOrEmpty(usage))
        {
            query = query.Where(r => r.AllowedUsage == "BOTH" || r.AllowedUsage == usage);
        }

        var results = await query
            .OrderByDescending(r => r.Popularity)
            .ThenBy(r => r.Name)
            .Take(limit)
            .Select(r => new RawMaterialSearchDto
            {
                Id = r.Id,
                Name = r.Name,
                DcbCode = r.DcbCode,
                CasNumber = r.CasNumber,
                Category = r.Category,
                Unit = r.Unit,
                AllowedUsage = r.AllowedUsage,
                PhysicalState = r.PhysicalState,
                BasePrice = r.BasePrice,
                CurrentStock = r.CurrentStock,
                CorrectionFactor = r.CorrectionFactor,
                PurityFactor = r.PurityFactor
            })
            .ToListAsync();

        return Ok(results);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs ADICIONAIS
// ═══════════════════════════════════════════════════════════════════════════════

public class QuickCalculationRequestDto
{
    public decimal TotalQuantity { get; set; }
    public string TotalUnit { get; set; } = "g";
    public List<QuickCalculationInputDto> Items { get; set; } = new();
}

public class QuickCalculationInputDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

public class QuickCalculationResultDto
{
    public decimal TotalQuantity { get; set; }
    public string TotalUnit { get; set; } = "g";
    public decimal TotalPercentage { get; set; }
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public List<QuickCalculationItemDto> Items { get; set; } = new();
}

public class QuickCalculationItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal QuantityGrams { get; set; }
    public string QuantityDisplay { get; set; } = string.Empty;
    public bool IsQsp { get; set; }
}

public class BaseWithFormDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid FormId { get; set; }
    public string FormCode { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public decimal YieldQuantity { get; set; }
    public string YieldUnit { get; set; } = "g";
    public decimal? MinimumPrice { get; set; }
    public decimal BaseCost { get; set; }
}

public class CreateBaseDto
{
    public Guid PharmaceuticalFormId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public decimal YieldQuantity { get; set; } = 100;
    public string YieldUnit { get; set; } = "g";
    public decimal? MinimumPrice { get; set; }
    public decimal BaseCost { get; set; }
    public int? ValidityDays { get; set; }
    public string? PreparationInstructions { get; set; }
    public int SortOrder { get; set; } = 100;
}

public class UpdateBaseDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? YieldQuantity { get; set; }
    public string? YieldUnit { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? BaseCost { get; set; }
    public int? ValidityDays { get; set; }
    public string? PreparationInstructions { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}

public class AddCompositionDto
{
    public Guid RawMaterialId { get; set; }
    public decimal? Percentage { get; set; }
    public decimal? QuantityPerYield { get; set; }
    public string? Unit { get; set; }
    public bool IsQsp { get; set; }
    public bool IsOptional { get; set; }
    public int SortOrder { get; set; } = 100;
}

public class UpdateCompositionDto
{
    public decimal? Percentage { get; set; }
    public decimal? QuantityPerYield { get; set; }
    public string? Unit { get; set; }
    public bool? IsQsp { get; set; }
    public bool? IsOptional { get; set; }
    public int? SortOrder { get; set; }
}

public class RawMaterialSearchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public string? CasNumber { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "g";
    public string? AllowedUsage { get; set; }
    public string? PhysicalState { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal CorrectionFactor { get; set; }
    public decimal PurityFactor { get; set; }
}
