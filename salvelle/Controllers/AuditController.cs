using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Controlled;
using Models.Pharmacy;

namespace Controllers;

[Route("[controller]")]
public class AuditController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditController> _logger;

    public AuditController(AppDbContext context, ILogger<AuditController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee()
    {
        return HttpContext.Items["Employee"] as Employee;
    }

    private bool CanAccessAudit(Employee employee)
    {
        var code = (employee.JobPosition?.Code ?? "").ToUpper();
        return code == "PHARMACIST_RT" || code == "ADMIN" || code == "OWNER" || code == "GENERAL_MANAGER";
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee))
        {
            TempData["Error"] = "Acesso restrito ao Farmacêutico RT e Administradores.";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.CurrentEmployee = employee;
        var establishmentId = employee.EstablishmentId;

        var stats = new AuditDashboardStats
        {
            TotalControlledMovements = await _context.ControlledSubstanceMovements
                .Where(m => m.EstablishmentId == establishmentId).CountAsync(),
            
            PendingSngpcTransmissions = await _context.ControlledSubstanceMovements
                .Where(m => m.EstablishmentId == establishmentId && !m.SngpcSent).CountAsync(),
            
            TotalPrescriptionsThisMonth = await _context.SpecialPrescriptionControls
                .Where(p => p.EstablishmentId == establishmentId &&
                           p.CreatedAt.Month == DateTime.UtcNow.Month &&
                           p.CreatedAt.Year == DateTime.UtcNow.Year).CountAsync(),
            
            TotalApprovals = await _context.PharmacistApprovals
                .Where(a => a.EstablishmentId == establishmentId).CountAsync(),
            
            ActiveSupplierCertificates = await _context.SupplierCertificates
                .Where(c => c.Supplier!.EstablishmentId == establishmentId &&
                           c.IsActive &&
                           (c.ExpiryDate == null || c.ExpiryDate > DateTime.UtcNow)).CountAsync(),
            
            ExpiringCertificates = await _context.SupplierCertificates
                .Where(c => c.Supplier!.EstablishmentId == establishmentId &&
                           c.IsActive &&
                           c.ExpiryDate != null &&
                           c.ExpiryDate <= DateTime.UtcNow.AddDays(30)).CountAsync()
        };

        ViewBag.Stats = stats;
        ViewBag.Establishment = await _context.Establishments.FirstOrDefaultAsync(e => e.Id == establishmentId);

        return View();
    }

    [HttpGet("Reports/BMPO")]
    public async Task<IActionResult> ReportBMPO(int? month, int? year)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;
        month ??= DateTime.UtcNow.Month;
        year ??= DateTime.UtcNow.Year;

        var movements = await _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == employee.EstablishmentId &&
                       (m.ControlledList == "B1" || m.ControlledList == "B2") &&
                       m.MovementDate.Month == month && m.MovementDate.Year == year)
            .OrderBy(m => m.SubstanceName).ThenBy(m => m.MovementDate)
            .ToListAsync();

        ViewBag.Month = month;
        ViewBag.Year = year;
        ViewBag.ReportType = "BMPO";
        ViewBag.ReportTitle = "Balanço Mensal de Psicotrópicos";

        return View("Report", movements);
    }

    [HttpGet("Reports/BSPO")]
    public async Task<IActionResult> ReportBSPO(int? quarter, int? year)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;
        quarter ??= (DateTime.UtcNow.Month - 1) / 3 + 1;
        year ??= DateTime.UtcNow.Year;

        var startMonth = (quarter.Value - 1) * 3 + 1;
        var endMonth = startMonth + 2;

        var movements = await _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == employee.EstablishmentId &&
                       (m.ControlledList == "A1" || m.ControlledList == "A2" || m.ControlledList == "A3") &&
                       m.MovementDate.Month >= startMonth && m.MovementDate.Month <= endMonth &&
                       m.MovementDate.Year == year)
            .OrderBy(m => m.SubstanceName).ThenBy(m => m.MovementDate)
            .ToListAsync();

        ViewBag.Quarter = quarter;
        ViewBag.Year = year;
        ViewBag.ReportType = "BSPO";
        ViewBag.ReportTitle = "Balanço Trimestral de Substâncias Psicoativas";

        return View("Report", movements);
    }

    [HttpGet("Reports/Movements")]
    public async Task<IActionResult> ReportMovements(DateTime? startDate, DateTime? endDate, string? list)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var query = _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == employee.EstablishmentId &&
                       m.MovementDate >= startDate && m.MovementDate <= endDate);

        if (!string.IsNullOrEmpty(list))
            query = query.Where(m => m.ControlledList == list);

        var movements = await query.OrderByDescending(m => m.MovementDate).ToListAsync();

        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.SelectedList = list;
        ViewBag.ReportType = "Movements";
        ViewBag.ReportTitle = "Relatório de Movimentações";

        return View("Report", movements);
    }

    [HttpGet("Reports/Prescriptions")]
    public async Task<IActionResult> ReportPrescriptions(DateTime? startDate, DateTime? endDate, string? type)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var query = _context.SpecialPrescriptionControls
            .Where(p => p.EstablishmentId == employee.EstablishmentId &&
                       p.CreatedAt >= startDate && p.CreatedAt <= endDate);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.PrescriptionType == type);

        var prescriptions = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.SelectedType = type;

        return View("ReportPrescriptions", prescriptions);
    }

    [HttpGet("Reports/Approvals")]
    public async Task<IActionResult> ReportApprovals(DateTime? startDate, DateTime? endDate)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var approvals = await _context.PharmacistApprovals
            .Where(a => a.EstablishmentId == employee.EstablishmentId &&
                       a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        return View("ReportApprovals", approvals);
    }

    [HttpGet("AccessRequest")]
    [AllowAnonymous]
    public IActionResult AccessRequest()
    {
        return View();
    }

    [HttpPost("AccessRequest")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AccessRequestSubmit([FromForm] AuditorAccessRequestForm form)
    {
        if (!ModelState.IsValid)
            return View("AccessRequest", form);

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Cnpj == form.EstablishmentCnpj);

        if (establishment == null)
        {
            ModelState.AddModelError("EstablishmentCnpj", "Estabelecimento não encontrado.");
            return View("AccessRequest", form);
        }

        var accessRequest = new AuditorAccessRequest
        {
            EstablishmentId = establishment.Id,
            AuditorName = form.AuditorName,
            AuditorDocument = form.AuditorDocument,
            AuditorInstitution = form.AuditorInstitution,
            AuditorCredential = form.AuditorCredential,
            AccessReason = form.AccessReason,
            RequestedReports = form.RequestedReports?.Split(',').ToArray(),
            RequestedAt = DateTime.UtcNow
        };

        _context.AuditorAccessRequests.Add(accessRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Solicitação de acesso de auditor: {Auditor} ({Institution}) para {Establishment}",
            form.AuditorName, form.AuditorInstitution, establishment.NomeFantasia);

        TempData["Success"] = "Solicitação enviada com sucesso.";
        return RedirectToAction("AccessRequest");
    }

    [HttpGet("PendingAccessRequests")]
    public async Task<IActionResult> PendingAccessRequests()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var requests = await _context.AuditorAccessRequests
            .Where(r => r.EstablishmentId == employee.EstablishmentId && !r.AccessGranted)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost("ApproveAccess/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAccess(Guid id, int validityHours = 24)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanAccessAudit(employee)) return Forbid();

        var request = await _context.AuditorAccessRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.EstablishmentId == employee.EstablishmentId);

        if (request == null)
        {
            TempData["Error"] = "Solicitação não encontrada.";
            return RedirectToAction("PendingAccessRequests");
        }

        var token = Guid.NewGuid().ToString("N");
        request.ApprovedByEmployeeId = employee.Id;
        request.ApprovedAt = DateTime.UtcNow;
        request.AccessGranted = true;
        request.AccessToken = token;
        request.AccessValidFrom = DateTime.UtcNow;
        request.AccessValidUntil = DateTime.UtcNow.AddHours(validityHours);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Acesso de auditor aprovado: {Auditor} por {Approver}", request.AuditorName, employee.FullName);

        TempData["Success"] = $"Acesso aprovado para {request.AuditorName}. Token: {token}";
        return RedirectToAction("PendingAccessRequests");
    }
}

public class AuditDashboardStats
{
    public int TotalControlledMovements { get; set; }
    public int PendingSngpcTransmissions { get; set; }
    public int TotalPrescriptionsThisMonth { get; set; }
    public int TotalApprovals { get; set; }
    public int ActiveSupplierCertificates { get; set; }
    public int ExpiringCertificates { get; set; }
}

public class AuditorAccessRequestForm
{
    public string EstablishmentCnpj { get; set; } = "";
    public string AuditorName { get; set; } = "";
    public string AuditorDocument { get; set; } = "";
    public string AuditorInstitution { get; set; } = "";
    public string? AuditorCredential { get; set; }
    public string AccessReason { get; set; } = "";
    public string? RequestedReports { get; set; }
}
