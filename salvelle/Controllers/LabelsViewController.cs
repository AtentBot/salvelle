using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Labels;
using Models;

namespace Controllers;

[Route("Labels")]
public class LabelsViewController : Controller
{
    private readonly AppDbContext _context;

    public LabelsViewController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }

    private Guid? GetEmployeeId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        return employee?.Id;
    }

    private Guid? GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        return employee?.EstablishmentId;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return View(new List<LabelTemplateResponseDto>());
        }

        var templates = await _context.Set<LabelTemplate>()
            .Where(t => t.EstablishmentId == establishmentId.Value && t.IsActive)
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
                HtmlTemplate = t.HtmlTemplate,
                CssStyles = t.CssStyles,
                IsActive = t.IsActive,
                IsDefault = t.IsDefault,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return View(templates);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View(new CreateLabelTemplateDto());
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLabelTemplateDto dto)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        if (!establishmentId.HasValue || !employeeId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return View(dto);
        }

        if (!ModelState.IsValid)
            return View(dto);

        var template = new LabelTemplate
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId.Value,
            Name = dto.Name,
            Description = dto.Description,
            TemplateType = dto.TemplateType ?? "PADRAO",
            PharmaceuticalForm = dto.PharmaceuticalForm,
            Width = dto.Width > 0 ? dto.Width : 100,
            Height = dto.Height > 0 ? dto.Height : 60,
            HtmlTemplate = dto.HtmlTemplate ?? "",
            CssStyles = dto.CssStyles,
            IsActive = true,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId.Value
        };

        _context.Set<LabelTemplate>().Add(template);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Template criado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return RedirectToAction(nameof(Index));
        }

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
        {
            TempData["Error"] = "Template não encontrado";
            return RedirectToAction(nameof(Index));
        }

        var dto = new UpdateLabelTemplateDto
        {
            Name = template.Name,
            Description = template.Description,
            HtmlTemplate = template.HtmlTemplate,
            CssStyles = template.CssStyles,
            IsActive = template.IsActive,
            IsDefault = template.IsDefault
        };

        ViewBag.TemplateId = id;
        ViewBag.TemplateType = template.TemplateType;
        ViewBag.PharmaceuticalForm = template.PharmaceuticalForm;
        ViewBag.Width = template.Width;
        ViewBag.Height = template.Height;

        return View(dto);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateLabelTemplateDto dto)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        if (!establishmentId.HasValue || !employeeId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return RedirectToAction(nameof(Index));
        }

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
        {
            TempData["Error"] = "Template não encontrado";
            return RedirectToAction(nameof(Index));
        }

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.HtmlTemplate = dto.HtmlTemplate ?? "";
        template.CssStyles = dto.CssStyles;
        template.IsActive = dto.IsActive;
        template.IsDefault = dto.IsDefault;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Template atualizado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!IsAuthenticated())
            return Json(new { success = false, message = "Sessão inválida" });

        var employeeId = GetEmployeeId();
        var establishmentId = GetEstablishmentId();

        if (!establishmentId.HasValue || !employeeId.HasValue)
            return Json(new { success = false, message = "Estabelecimento não encontrado" });

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return Json(new { success = false, message = "Template não encontrado" });

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Template desativado com sucesso" });
    }

    [HttpGet("Generate/{manipulationOrderId}")]
    public async Task<IActionResult> Generate(Guid manipulationOrderId)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return RedirectToAction(nameof(Index));
        }

        var templates = await _context.Set<LabelTemplate>()
            .Where(t => t.EstablishmentId == establishmentId.Value && t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.TemplateType, t.IsDefault })
            .ToListAsync();

        ViewBag.ManipulationOrderId = manipulationOrderId;
        ViewBag.Templates = templates;

        return View();
    }

    [HttpGet("View/{labelId}")]
    public async Task<IActionResult> ViewLabel(Guid labelId)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return RedirectToAction(nameof(History));
        }

        var label = await _context.Set<GeneratedLabel>()
            .FirstOrDefaultAsync(l => l.Id == labelId && l.EstablishmentId == establishmentId.Value);

        if (label == null)
        {
            TempData["Error"] = "Rótulo não encontrado";
            return RedirectToAction(nameof(History));
        }

        ViewBag.LabelId = labelId;
        return View("View", label);
    }

    [HttpGet("History")]
    public async Task<IActionResult> History(int page = 1, int pageSize = 20)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return View(new List<GeneratedLabel>());
        }

        var query = _context.Set<GeneratedLabel>()
            .Where(l => l.EstablishmentId == establishmentId.Value)
            .OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync();
        var labels = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(labels);
    }

    [HttpGet("Search")]
    public IActionResult Search()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    [HttpGet("Preview/{id}")]
    public async Task<IActionResult> Preview(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
        {
            TempData["Error"] = "Estabelecimento não encontrado";
            return RedirectToAction(nameof(Index));
        }

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
        {
            TempData["Error"] = "Template não encontrado";
            return RedirectToAction(nameof(Index));
        }

        return View(template);
    }

    [HttpGet("PreviewHtml/{id}")]
    public async Task<IActionResult> PreviewHtml(Guid id)
    {
        if (!IsAuthenticated())
            return Unauthorized();

        var establishmentId = GetEstablishmentId();
        if (!establishmentId.HasValue)
            return NotFound();

        var template = await _context.Set<LabelTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == establishmentId.Value);

        if (template == null)
            return NotFound();

        // Substituir placeholders com dados de exemplo
        var html = template.HtmlTemplate
            .Replace("{{CSS_STYLES}}", template.CssStyles ?? "")
            .Replace("{{PHARMACY_NAME}}", "Farmácia Salvelle")
            .Replace("{{PHARMACY_CNPJ}}", "12.345.678/0001-90")
            .Replace("{{PHARMACY_ADDRESS}}", "Rua Exemplo, 123 - Centro")
            .Replace("{{PHARMACY_PHONE}}", "(11) 3333-4444")
            .Replace("{{PATIENT_NAME}}", "João da Silva")
            .Replace("{{FORMULA_NAME}}", "Vitamina C 500mg + Zinco 30mg")
            .Replace("{{PHARMACEUTICAL_FORM}}", "Cápsulas")
            .Replace("{{COMPOSITION}}", "Vitamina C 500mg, Zinco quelato 30mg, Excipiente q.s.p. 1 cápsula")
            .Replace("{{QUANTITY}}", "60")
            .Replace("{{UNIT}}", "cápsulas")
            .Replace("{{POSOLOGY}}", "Tomar 1 cápsula ao dia, preferencialmente pela manhã")
            .Replace("{{BATCH_NUMBER}}", "LOT-2024-001234")
            .Replace("{{MANIPULATION_DATE}}", Helpers.BrazilTime.Now.ToString("dd/MM/yyyy"))
            .Replace("{{EXPIRATION_DATE}}", Helpers.BrazilTime.Now.AddMonths(6).ToString("dd/MM/yyyy"))
            .Replace("{{PRESCRIBER_NAME}}", "Dr. Carlos Medicina")
            .Replace("{{PRESCRIBER_REGISTRATION}}", "CRM 12345/SP")
            .Replace("{{PHARMACIST_NAME}}", "Dr. João Farmacêutico")
            .Replace("{{PHARMACIST_CRF}}", "CRF-SP 54321")
            .Replace("{{WARNINGS}}", "Manter fora do alcance de crianças. Conservar em local seco e fresco.")
            .Replace("{{STORAGE_CONDITIONS}}", "Conservar em temperatura ambiente (15-30°C)")
            .Replace("{{USAGE_TYPE}}", "INTERNO")
            .Replace("{{CONTROL_SCHEDULE}}", "C1")
            .Replace("{{LABEL_CODE}}", "ROT-2024-001234")
            .Replace("{{QR_CODE_URL}}", "");

        return Content(html, "text/html");
    }
}
