using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using Models.Employees;
using DTOs.PharmaceuticalForms;
using Service.PharmaceuticalForms;

namespace Controllers.Api;

/// <summary>
/// API Controller para Formas Farmacêuticas e Precificação
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PharmaceuticalFormsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CapsuleCalculationService _capsuleService;
    private readonly ILogger<PharmaceuticalFormsController> _logger;

    public PharmaceuticalFormsController(
        AppDbContext context,
        CapsuleCalculationService capsuleService,
        ILogger<PharmaceuticalFormsController> logger)
    {
        _context = context;
        _capsuleService = capsuleService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORMAS FARMACÊUTICAS - CRUD
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista todas as formas farmacêuticas do estabelecimento
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PharmaceuticalFormListDto>>> GetAll(
        [FromQuery] bool? onlyActive = null,
        [FromQuery] string? usageType = null)
    {
        var establishmentId = GetEstablishmentId();

        var query = _context.PharmaceuticalForms
            .Where(f => f.EstablishmentId == establishmentId)
            .AsQueryable();

        if (onlyActive == true)
            query = query.Where(f => f.IsActive);

        if (!string.IsNullOrEmpty(usageType))
            query = query.Where(f => f.UsageType == usageType || f.UsageType == "BOTH");

        var forms = await query
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .Select(f => new PharmaceuticalFormListDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                IsActive = f.IsActive,
                IsSystemDefault = f.IsSystemDefault,
                IsCustom = f.IsCustom,
                MinimumPrice = f.MinimumPrice,
                MaxQuantityLimit = f.MaxQuantityLimit,
                DefaultValidityDays = f.DefaultValidityDays,
                DefaultUnit = f.DefaultUnit,
                UsageType = f.UsageType,
                Icon = f.Icon,
                SortOrder = f.SortOrder,
                SubtypesCount = f.Subtypes != null ? f.Subtypes.Count(s => s.IsActive) : 0
            })
            .ToListAsync();

        return Ok(forms);
    }

    /// <summary>
    /// Busca uma forma farmacêutica por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PharmaceuticalFormDetailDto>> GetById(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        var form = await _context.PharmaceuticalForms
            .Include(f => f.Subtypes!.Where(s => s.IsActive))
            .FirstOrDefaultAsync(f => f.Id == id && f.EstablishmentId == establishmentId);

        if (form == null)
            return NotFound(new { message = "Forma farmacêutica não encontrada" });

        var dto = new PharmaceuticalFormDetailDto
        {
            Id = form.Id,
            Code = form.Code,
            Name = form.Name,
            Description = form.Description,
            IsActive = form.IsActive,
            IsSystemDefault = form.IsSystemDefault,
            IsCustom = form.IsCustom,
            MinimumPrice = form.MinimumPrice,
            MaxQuantityLimit = form.MaxQuantityLimit,
            DefaultValidityDays = form.DefaultValidityDays,
            DefaultUnit = form.DefaultUnit,
            PreparationTimeHours = form.PreparationTimeHours,
            UsageInstructions = form.UsageInstructions,
            UsageType = form.UsageType,
            Icon = form.Icon,
            SortOrder = form.SortOrder,
            CreatedAt = form.CreatedAt,
            UpdatedAt = form.UpdatedAt,
            Subtypes = form.Subtypes?.OrderBy(s => s.SortOrder).Select(s => new PharmaceuticalFormSubtypeListDto
            {
                Id = s.Id,
                PharmaceuticalFormId = s.PharmaceuticalFormId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                MinimumPrice = s.MinimumPrice,
                BaseCost = s.BaseCost,
                YieldQuantity = s.YieldQuantity,
                YieldUnit = s.YieldUnit,
                ValidityDays = s.ValidityDays,
                CapsuleSize = s.CapsuleSize,
                CapsuleVolumeMl = s.CapsuleVolumeMl,
                CapsuleCapacityMgMin = s.CapsuleCapacityMgMin,
                CapsuleCapacityMgMax = s.CapsuleCapacityMgMax,
                CapsuleColor = s.CapsuleColor,
                SortOrder = s.SortOrder,
                CompositionsCount = s.Compositions != null ? s.Compositions.Count : 0
            }).ToList() ?? new()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Cria uma nova forma farmacêutica personalizada
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PharmaceuticalFormListDto>> Create([FromBody] CreatePharmaceuticalFormDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        // Verificar se código já existe
        var exists = await _context.PharmaceuticalForms
            .AnyAsync(f => f.EstablishmentId == establishmentId && f.Code == dto.Code);

        if (exists)
            return BadRequest(new { message = "Já existe uma forma farmacêutica com este código" });

        var form = new PharmaceuticalForm
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = dto.Code.ToUpper(),
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            IsSystemDefault = false,
            IsCustom = true,
            MinimumPrice = dto.MinimumPrice,
            MaxQuantityLimit = dto.MaxQuantityLimit,
            DefaultValidityDays = dto.DefaultValidityDays,
            DefaultUnit = dto.DefaultUnit,
            PreparationTimeHours = dto.PreparationTimeHours,
            UsageInstructions = dto.UsageInstructions,
            UsageType = dto.UsageType,
            Icon = dto.Icon,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId,
            UpdatedByEmployeeId = employeeId
        };

        _context.PharmaceuticalForms.Add(form);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Forma farmacêutica criada: {Name} ({Code})", form.Name, form.Code);

        return CreatedAtAction(nameof(GetById), new { id = form.Id }, new PharmaceuticalFormListDto
        {
            Id = form.Id,
            Code = form.Code,
            Name = form.Name,
            Description = form.Description,
            IsActive = form.IsActive,
            IsSystemDefault = form.IsSystemDefault,
            IsCustom = form.IsCustom,
            MinimumPrice = form.MinimumPrice,
            MaxQuantityLimit = form.MaxQuantityLimit,
            DefaultValidityDays = form.DefaultValidityDays,
            DefaultUnit = form.DefaultUnit,
            UsageType = form.UsageType,
            Icon = form.Icon,
            SortOrder = form.SortOrder,
            SubtypesCount = 0
        });
    }

    /// <summary>
    /// Atualiza uma forma farmacêutica
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePharmaceuticalFormDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var form = await _context.PharmaceuticalForms
            .FirstOrDefaultAsync(f => f.Id == id && f.EstablishmentId == establishmentId);

        if (form == null)
            return NotFound(new { message = "Forma farmacêutica não encontrada" });

        // Atualizar campos não nulos
        if (dto.Name != null) form.Name = dto.Name;
        if (dto.Description != null) form.Description = dto.Description;
        if (dto.IsActive.HasValue) form.IsActive = dto.IsActive.Value;
        if (dto.MinimumPrice.HasValue) form.MinimumPrice = dto.MinimumPrice.Value;
        if (dto.MaxQuantityLimit.HasValue) form.MaxQuantityLimit = dto.MaxQuantityLimit.Value;
        if (dto.DefaultValidityDays.HasValue) form.DefaultValidityDays = dto.DefaultValidityDays.Value;
        if (dto.DefaultUnit != null) form.DefaultUnit = dto.DefaultUnit;
        if (dto.PreparationTimeHours.HasValue) form.PreparationTimeHours = dto.PreparationTimeHours.Value;
        if (dto.UsageInstructions != null) form.UsageInstructions = dto.UsageInstructions;
        if (dto.UsageType != null) form.UsageType = dto.UsageType;
        if (dto.Icon != null) form.Icon = dto.Icon;
        if (dto.SortOrder.HasValue) form.SortOrder = dto.SortOrder.Value;

        form.UpdatedAt = DateTime.UtcNow;
        form.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Forma farmacêutica atualizada: {Name} (ID: {Id})", form.Name, form.Id);

        return Ok(new { message = "Forma farmacêutica atualizada com sucesso" });
    }

    /// <summary>
    /// Alterna status ativo/inativo
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult> ToggleActive(Guid id, [FromBody] ToggleActiveDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var form = await _context.PharmaceuticalForms
            .FirstOrDefaultAsync(f => f.Id == id && f.EstablishmentId == establishmentId);

        if (form == null)
            return NotFound(new { message = "Forma farmacêutica não encontrada" });

        form.IsActive = dto.IsActive;
        form.UpdatedAt = DateTime.UtcNow;
        form.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Forma farmacêutica {Name} {Status}", 
            form.Name, dto.IsActive ? "ativada" : "desativada");

        return Ok(new { message = $"Forma farmacêutica {(dto.IsActive ? "ativada" : "desativada")} com sucesso" });
    }

    /// <summary>
    /// Exclui uma forma farmacêutica personalizada
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        var form = await _context.PharmaceuticalForms
            .FirstOrDefaultAsync(f => f.Id == id && f.EstablishmentId == establishmentId);

        if (form == null)
            return NotFound(new { message = "Forma farmacêutica não encontrada" });

        if (form.IsSystemDefault)
            return BadRequest(new { message = "Não é possível excluir formas farmacêuticas padrão do sistema" });

        _context.PharmaceuticalForms.Remove(form);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Forma farmacêutica excluída: {Name} (ID: {Id})", form.Name, form.Id);

        return Ok(new { message = "Forma farmacêutica excluída com sucesso" });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SUBTIPOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista subtipos de uma forma farmacêutica
    /// </summary>
    [HttpGet("{formId}/subtypes")]
    public async Task<ActionResult<List<PharmaceuticalFormSubtypeListDto>>> GetSubtypes(
        Guid formId,
        [FromQuery] bool? onlyActive = null)
    {
        var establishmentId = GetEstablishmentId();

        var query = _context.PharmaceuticalFormSubtypes
            .Include(s => s.Compositions)
            .Where(s => s.PharmaceuticalFormId == formId && s.EstablishmentId == establishmentId);

        if (onlyActive == true)
            query = query.Where(s => s.IsActive);

        var subtypes = await query
            .OrderBy(s => s.SortOrder)
            .Select(s => new PharmaceuticalFormSubtypeListDto
            {
                Id = s.Id,
                PharmaceuticalFormId = s.PharmaceuticalFormId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                MinimumPrice = s.MinimumPrice,
                BaseCost = s.BaseCost,
                YieldQuantity = s.YieldQuantity,
                YieldUnit = s.YieldUnit,
                ValidityDays = s.ValidityDays,
                CapsuleSize = s.CapsuleSize,
                CapsuleVolumeMl = s.CapsuleVolumeMl,
                CapsuleCapacityMgMin = s.CapsuleCapacityMgMin,
                CapsuleCapacityMgMax = s.CapsuleCapacityMgMax,
                CapsuleColor = s.CapsuleColor,
                SortOrder = s.SortOrder,
                CompositionsCount = s.Compositions != null ? s.Compositions.Count : 0
            })
            .ToListAsync();

        return Ok(subtypes);
    }

    /// <summary>
    /// Busca um subtipo por ID
    /// </summary>
    [HttpGet("subtypes/{id}")]
    public async Task<ActionResult<PharmaceuticalFormSubtypeDetailDto>> GetSubtypeById(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        var subtype = await _context.PharmaceuticalFormSubtypes
            .Include(s => s.PharmaceuticalForm)
            .Include(s => s.Compositions!)
                .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(s => s.Id == id && s.EstablishmentId == establishmentId);

        if (subtype == null)
            return NotFound(new { message = "Subtipo não encontrado" });

        var dto = new PharmaceuticalFormSubtypeDetailDto
        {
            Id = subtype.Id,
            PharmaceuticalFormId = subtype.PharmaceuticalFormId,
            PharmaceuticalFormName = subtype.PharmaceuticalForm?.Name ?? "",
            Code = subtype.Code,
            Name = subtype.Name,
            Description = subtype.Description,
            IsActive = subtype.IsActive,
            IsDefault = subtype.IsDefault,
            MinimumPrice = subtype.MinimumPrice,
            BaseCost = subtype.BaseCost,
            YieldQuantity = subtype.YieldQuantity,
            YieldUnit = subtype.YieldUnit,
            ValidityDays = subtype.ValidityDays,
            MaxQuantityLimit = subtype.MaxQuantityLimit,
            CapsuleSize = subtype.CapsuleSize,
            CapsuleVolumeMl = subtype.CapsuleVolumeMl,
            CapsuleCapacityMgMin = subtype.CapsuleCapacityMgMin,
            CapsuleCapacityMgMax = subtype.CapsuleCapacityMgMax,
            CapsuleColor = subtype.CapsuleColor,
            PreparationInstructions = subtype.PreparationInstructions,
            SortOrder = subtype.SortOrder,
            CreatedAt = subtype.CreatedAt,
            UpdatedAt = subtype.UpdatedAt,
            Compositions = subtype.Compositions?.OrderBy(c => c.SortOrder).Select(c => new PharmaceuticalFormCompositionDto
            {
                Id = c.Id,
                SubtypeId = c.SubtypeId,
                RawMaterialId = c.RawMaterialId,
                RawMaterialName = c.RawMaterial?.Name ?? "",
                RawMaterialCode = c.RawMaterial?.DcbCode ?? c.RawMaterial?.CasNumber,
                Percentage = c.Percentage,
                QuantityPerYield = c.QuantityPerYield,
                Unit = c.Unit,
                IsQsp = c.IsQsp,
                IsOptional = c.IsOptional,
                SortOrder = c.SortOrder
            }).ToList() ?? new()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Cria um novo subtipo
    /// </summary>
    [HttpPost("{formId}/subtypes")]
    public async Task<ActionResult> CreateSubtype(Guid formId, [FromBody] CreatePharmaceuticalFormSubtypeDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var form = await _context.PharmaceuticalForms
            .FirstOrDefaultAsync(f => f.Id == formId && f.EstablishmentId == establishmentId);

        if (form == null)
            return NotFound(new { message = "Forma farmacêutica não encontrada" });

        var subtype = new PharmaceuticalFormSubtype
        {
            Id = Guid.NewGuid(),
            PharmaceuticalFormId = formId,
            EstablishmentId = establishmentId,
            Code = dto.Code.ToUpper(),
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            MinimumPrice = dto.MinimumPrice,
            YieldQuantity = dto.YieldQuantity,
            YieldUnit = dto.YieldUnit,
            ValidityDays = dto.ValidityDays,
            MaxQuantityLimit = dto.MaxQuantityLimit,
            CapsuleSize = dto.CapsuleSize,
            CapsuleVolumeMl = dto.CapsuleVolumeMl,
            CapsuleCapacityMgMin = dto.CapsuleCapacityMgMin,
            CapsuleCapacityMgMax = dto.CapsuleCapacityMgMax,
            CapsuleColor = dto.CapsuleColor,
            PreparationInstructions = dto.PreparationInstructions,
            SortOrder = dto.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId,
            UpdatedByEmployeeId = employeeId
        };

        // Se marcado como default, desmarcar outros
        if (dto.IsDefault)
        {
            var others = await _context.PharmaceuticalFormSubtypes
                .Where(s => s.PharmaceuticalFormId == formId && s.EstablishmentId == establishmentId && s.IsDefault)
                .ToListAsync();

            foreach (var other in others)
                other.IsDefault = false;
        }

        _context.PharmaceuticalFormSubtypes.Add(subtype);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubtypeById), new { id = subtype.Id }, 
            new { message = "Subtipo criado com sucesso", id = subtype.Id });
    }

    /// <summary>
    /// Atualiza um subtipo
    /// </summary>
    [HttpPut("subtypes/{id}")]
    public async Task<ActionResult> UpdateSubtype(Guid id, [FromBody] UpdatePharmaceuticalFormSubtypeDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var subtype = await _context.PharmaceuticalFormSubtypes
            .FirstOrDefaultAsync(s => s.Id == id && s.EstablishmentId == establishmentId);

        if (subtype == null)
            return NotFound(new { message = "Subtipo não encontrado" });

        if (dto.Name != null) subtype.Name = dto.Name;
        if (dto.Description != null) subtype.Description = dto.Description;
        if (dto.IsActive.HasValue) subtype.IsActive = dto.IsActive.Value;
        if (dto.IsDefault.HasValue) subtype.IsDefault = dto.IsDefault.Value;
        if (dto.MinimumPrice.HasValue) subtype.MinimumPrice = dto.MinimumPrice.Value;
        if (dto.YieldQuantity.HasValue) subtype.YieldQuantity = dto.YieldQuantity.Value;
        if (dto.YieldUnit != null) subtype.YieldUnit = dto.YieldUnit;
        if (dto.ValidityDays.HasValue) subtype.ValidityDays = dto.ValidityDays.Value;
        if (dto.MaxQuantityLimit.HasValue) subtype.MaxQuantityLimit = dto.MaxQuantityLimit.Value;
        if (dto.CapsuleSize != null) subtype.CapsuleSize = dto.CapsuleSize;
        if (dto.CapsuleVolumeMl.HasValue) subtype.CapsuleVolumeMl = dto.CapsuleVolumeMl.Value;
        if (dto.CapsuleCapacityMgMin.HasValue) subtype.CapsuleCapacityMgMin = dto.CapsuleCapacityMgMin.Value;
        if (dto.CapsuleCapacityMgMax.HasValue) subtype.CapsuleCapacityMgMax = dto.CapsuleCapacityMgMax.Value;
        if (dto.CapsuleColor != null) subtype.CapsuleColor = dto.CapsuleColor;
        if (dto.PreparationInstructions != null) subtype.PreparationInstructions = dto.PreparationInstructions;
        if (dto.SortOrder.HasValue) subtype.SortOrder = dto.SortOrder.Value;

        subtype.UpdatedAt = DateTime.UtcNow;
        subtype.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Subtipo atualizado com sucesso" });
    }

    /// <summary>
    /// Exclui um subtipo
    /// </summary>
    [HttpDelete("subtypes/{id}")]
    public async Task<ActionResult> DeleteSubtype(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        var subtype = await _context.PharmaceuticalFormSubtypes
            .FirstOrDefaultAsync(s => s.Id == id && s.EstablishmentId == establishmentId);

        if (subtype == null)
            return NotFound(new { message = "Subtipo não encontrado" });

        _context.PharmaceuticalFormSubtypes.Remove(subtype);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subtipo excluído com sucesso" });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CÁLCULO DE CÁPSULAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula automaticamente tamanho de cápsula e quantidades
    /// </summary>
    [HttpPost("capsules/calculate")]
    public async Task<ActionResult<CapsuleCalculationResultDto>> CalculateCapsules(
        [FromBody] CapsuleCalculationRequestDto request)
    {
        var establishmentId = GetEstablishmentId();
        var result = await _capsuleService.CalculateAsync(request, establishmentId);
        return Ok(result);
    }

    /// <summary>
    /// Lista tamanhos de cápsulas disponíveis
    /// </summary>
    [HttpGet("capsules/sizes")]
    public async Task<ActionResult<List<CapsuleSizeReferenceDto>>> GetCapsuleSizes(
        [FromQuery] bool onlyActive = true)
    {
        var establishmentId = GetEstablishmentId();
        var sizes = await _capsuleService.GetCapsuleSizesAsync(establishmentId, onlyActive);
        return Ok(sizes);
    }

    /// <summary>
    /// Atualiza configuração de tamanho de cápsula
    /// </summary>
    [HttpPut("capsules/sizes/{id}")]
    public async Task<ActionResult> UpdateCapsuleSize(Guid id, [FromBody] UpdateCapsuleSizeReferenceDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var (success, message) = await _capsuleService.UpdateCapsuleSizeAsync(id, dto, establishmentId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONFIGURAÇÃO DE PRECIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Busca configuração de precificação do estabelecimento
    /// </summary>
    [HttpGet("pricing-config")]
    public async Task<ActionResult<EstablishmentPricingConfigDto>> GetPricingConfig()
    {
        var establishmentId = GetEstablishmentId();

        var config = await _context.EstablishmentPricingConfigs
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId);

        if (config == null)
            return NotFound(new { message = "Configuração de precificação não encontrada" });

        return Ok(new EstablishmentPricingConfigDto
        {
            Id = config.Id,
            EstablishmentId = config.EstablishmentId,
            TaxPercentage = config.TaxPercentage,
            Fee1Name = config.Fee1Name,
            Fee1Percentage = config.Fee1Percentage,
            Fee2Name = config.Fee2Name,
            Fee2Percentage = config.Fee2Percentage,
            Fee3Name = config.Fee3Name,
            Fee3Percentage = config.Fee3Percentage,
            MarkupPercentage = config.MarkupPercentage,
            PackagingPercentage = config.PackagingPercentage,
            ApplyMinimumPrice = config.ApplyMinimumPrice,
            RoundToCents = config.RoundToCents,
            UpdatedAt = config.UpdatedAt
        });
    }

    /// <summary>
    /// Atualiza configuração de precificação
    /// </summary>
    [HttpPut("pricing-config")]
    public async Task<ActionResult> UpdatePricingConfig([FromBody] UpdateEstablishmentPricingConfigDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var config = await _context.EstablishmentPricingConfigs
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId);

        if (config == null)
            return NotFound(new { message = "Configuração de precificação não encontrada" });

        if (dto.TaxPercentage.HasValue) config.TaxPercentage = dto.TaxPercentage.Value;
        if (dto.Fee1Name != null) config.Fee1Name = dto.Fee1Name;
        if (dto.Fee1Percentage.HasValue) config.Fee1Percentage = dto.Fee1Percentage.Value;
        if (dto.Fee2Name != null) config.Fee2Name = dto.Fee2Name;
        if (dto.Fee2Percentage.HasValue) config.Fee2Percentage = dto.Fee2Percentage.Value;
        if (dto.Fee3Name != null) config.Fee3Name = dto.Fee3Name;
        if (dto.Fee3Percentage.HasValue) config.Fee3Percentage = dto.Fee3Percentage.Value;
        if (dto.MarkupPercentage.HasValue) config.MarkupPercentage = dto.MarkupPercentage.Value;
        if (dto.PackagingPercentage.HasValue) config.PackagingPercentage = dto.PackagingPercentage.Value;
        if (dto.ApplyMinimumPrice.HasValue) config.ApplyMinimumPrice = dto.ApplyMinimumPrice.Value;
        if (dto.RoundToCents.HasValue) config.RoundToCents = dto.RoundToCents.Value;

        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Configuração de precificação atualizada pelo funcionário {EmployeeId}", employeeId);

        return Ok(new { message = "Configuração de precificação atualizada com sucesso" });
    }

    /// <summary>
    /// Calcula preço final com base no custo
    /// </summary>
    [HttpPost("pricing-config/calculate")]
    public async Task<ActionResult<PriceBreakdownDto>> CalculatePrice(
        [FromBody] CalculatePriceRequestDto request)
    {
        var establishmentId = GetEstablishmentId();

        var config = await _context.EstablishmentPricingConfigs
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId);

        if (config == null)
            return NotFound(new { message = "Configuração de precificação não encontrada" });

        var breakdown = config.GetPriceBreakdown(request.BaseCost, request.MinimumPrice);

        return Ok(new PriceBreakdownDto
        {
            BaseCost = breakdown.BaseCost,
            TaxValue = breakdown.TaxValue,
            TaxPercentage = breakdown.TaxPercentage,
            Fee1Name = breakdown.Fee1Name,
            Fee1Value = breakdown.Fee1Value,
            Fee1Percentage = breakdown.Fee1Percentage,
            Fee2Name = breakdown.Fee2Name,
            Fee2Value = breakdown.Fee2Value,
            Fee2Percentage = breakdown.Fee2Percentage,
            Fee3Name = breakdown.Fee3Name,
            Fee3Value = breakdown.Fee3Value,
            Fee3Percentage = breakdown.Fee3Percentage,
            MarkupValue = breakdown.MarkupValue,
            MarkupPercentage = breakdown.MarkupPercentage,
            PackagingValue = breakdown.PackagingValue,
            PackagingPercentage = breakdown.PackagingPercentage,
            TotalAdditions = breakdown.TotalAdditions,
            CalculatedPrice = breakdown.CalculatedPrice,
            MinimumPrice = breakdown.MinimumPrice,
            MinimumPriceApplied = breakdown.MinimumPriceApplied,
            FinalPrice = breakdown.FinalPrice
        });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }

    private Guid GetEmployeeId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.Id ?? Guid.Empty;
    }
}

/// <summary>
/// DTO para requisição de cálculo de preço
/// </summary>
public class CalculatePriceRequestDto
{
    public decimal BaseCost { get; set; }
    public decimal? MinimumPrice { get; set; }
}
