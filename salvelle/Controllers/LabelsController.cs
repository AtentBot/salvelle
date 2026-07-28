using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Labels;
using Service;
using Validators.Labels;
using Models;
using Validators;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabelsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly LabelService _service;

    public LabelsController(AppDbContext context, LabelService service)
    {
        _context = context;
        _service = service;
    }

    private Guid? GetEmployeeId()
    {
        var claim = User.FindFirst("employee_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private async Task<Guid?> GetEstablishmentId(Guid employeeId)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId);
        return employee?.EstablishmentId;
    }

    // ============================================
    // TEMPLATES
    // ============================================

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] bool? activeOnly = true)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<LabelTemplate>()
            .Where(t => t.EstablishmentId == establishmentId.Value);

        if (activeOnly.HasValue && activeOnly.Value)
            query = query.Where(t => t.IsActive);

        var templates = await query
            .OrderBy(t => t.Name)
            .Select(t => new LabelTemplateResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                TemplateType = t.TemplateType,
                PharmaceuticalForm = t.PharmaceuticalForm,
                Width = t.Width,
                Height = t.Height,
                IsActive = t.IsActive,
                IsDefault = t.IsDefault,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplateById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound(new { message = "Template não encontrado" });

        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateLabelTemplateDto dto)
    {
        var validator = new CreateLabelTemplateValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, template) = await _service.CreateTemplateAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetTemplateById),
            new { id = template!.Id },
            new { message, templateId = template.Id });
    }

    [HttpPut("templates/{id}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateLabelTemplateDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound(new { message = "Template não encontrado" });

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.HtmlTemplate = dto.HtmlTemplate;
        template.CssStyles = dto.CssStyles;
        template.IsActive = dto.IsActive;
        template.IsDefault = dto.IsDefault;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Template atualizado com sucesso" });
    }

    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound(new { message = "Template não encontrado" });

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Template desativado com sucesso" });
    }

    // ============================================
    // GERAÇÃO E IMPRESSÃO
    // ============================================

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateLabel([FromBody] GenerateLabelDto dto)
    {
        var validator = new GenerateLabelValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, label) = await _service.GenerateLabelAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, labelId = label!.Id, html = label.GeneratedHtml });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLabel(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var label = await _context.Set<GeneratedLabel>()
            .Where(l => l.Id == id && l.EstablishmentId == establishmentId.Value)
            .Select(l => new GeneratedLabelResponseDto
            {
                Id = l.Id,
                LabelCode = l.LabelCode,
                ManipulationOrderId = l.ManipulationOrderId,
                ManipulationOrderCode = _context.ManipulationOrders
                    .Where(o => o.Id == l.ManipulationOrderId)
                    .Select(o => o.Code)
                    .FirstOrDefault() ?? "",
                PatientName = l.PatientName,
                PrescriberName = l.PrescriberName,
                PrescriberRegistration = l.PrescriberRegistration,
                FormulaName = l.FormulaName,
                PharmaceuticalForm = l.PharmaceuticalForm,
                Composition = l.Composition,
                Quantity = l.Quantity,
                Unit = l.Unit,
                BatchNumber = l.BatchNumber,
                ManipulationDate = l.ManipulationDate,
                ExpirationDate = l.ExpirationDate,
                Posology = l.Posology,
                AdministrationRoute = l.AdministrationRoute,
                StorageConditions = l.StorageConditions,
                Warnings = l.Warnings,
                UsageType = l.UsageType,
                IsControlled = l.IsControlled,
                ControlSchedule = l.ControlSchedule,
                PharmacyName = l.PharmacyName,
                PharmacistName = l.PharmacistName,
                PharmacistCrf = l.PharmacistCrf,
                QrCodeData = l.QrCodeData,
                QrCodeImageUrl = l.QrCodeImageUrl,
                BarcodeData = l.BarcodeData,
                GeneratedHtml = l.GeneratedHtml,
                PrintCount = l.PrintCount,
                LastPrintedAt = l.LastPrintedAt,
                Status = l.Status,
                CreatedAt = l.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (label == null)
            return NotFound(new { message = "Rótulo não encontrado" });

        return Ok(label);
    }

    [HttpPost("{id}/print")]
    public async Task<IActionResult> PrintLabel(Guid id, [FromBody] PrintLabelDto dto)
    {
        var validator = new PrintLabelValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _service.PrintLabelAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchLabels([FromQuery] LabelSearchDto dto)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Set<GeneratedLabel>()
            .Where(l => l.EstablishmentId == establishmentId.Value);

        if (dto.ManipulationOrderId.HasValue)
            query = query.Where(l => l.ManipulationOrderId == dto.ManipulationOrderId.Value);

        if (!string.IsNullOrEmpty(dto.PatientName))
            query = query.Where(l => l.PatientName != null && l.PatientName.Contains(dto.PatientName));

        if (!string.IsNullOrEmpty(dto.BatchNumber))
            query = query.Where(l => l.BatchNumber != null && l.BatchNumber.Contains(dto.BatchNumber));

        if (!string.IsNullOrEmpty(dto.Status))
            query = query.Where(l => l.Status == dto.Status.ToUpper());

        if (dto.DateFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= dto.DateFrom.Value);

        if (dto.DateTo.HasValue)
            query = query.Where(l => l.CreatedAt <= dto.DateTo.Value);

        if (dto.IsControlled.HasValue)
            query = query.Where(l => l.IsControlled == dto.IsControlled.Value);

        var totalCount = await query.CountAsync();

        var labels = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .Select(l => new GeneratedLabelResponseDto
            {
                Id = l.Id,
                LabelCode = l.LabelCode,
                ManipulationOrderId = l.ManipulationOrderId,
                ManipulationOrderCode = _context.ManipulationOrders
                    .Where(o => o.Id == l.ManipulationOrderId)
                    .Select(o => o.Code)
                    .FirstOrDefault() ?? "",
                PatientName = l.PatientName,
                FormulaName = l.FormulaName,
                BatchNumber = l.BatchNumber,
                ManipulationDate = l.ManipulationDate,
                ExpirationDate = l.ExpirationDate,
                PharmacistName = l.PharmacistName,
                PrintCount = l.PrintCount,
                LastPrintedAt = l.LastPrintedAt,
                Status = l.Status,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            labels,
            pagination = new
            {
                currentPage = dto.Page,
                pageSize = dto.PageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)dto.PageSize)
            }
        });
    }
}