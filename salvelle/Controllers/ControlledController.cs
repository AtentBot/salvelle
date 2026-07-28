using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Controlled;

namespace Controllers;

[Route("[controller]")]
public class ControlledController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ControlledController> _logger;

    public ControlledController(AppDbContext context, ILogger<ControlledController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtém o funcionário atual do HttpContext.Items (preenchido pelo middleware)
    /// </summary>
    private Employee? GetCurrentEmployee()
    {
        // O EmployeeAuthMiddleware já carrega o Employee com JobPosition e Establishment
        return HttpContext.Items["Employee"] as Employee;
    }

    private bool IsPharmacist(Employee employee)
    {
        var code = (employee.JobPosition?.Code ?? "").ToUpper();
        return code == "PHARMACIST" || code == "PHARMACIST_RT" || code == "ADMIN" || 
               code == "OWNER" || code == "GENERAL_MANAGER";
    }

    private bool CanApproveControlled(Employee employee)
    {
        var code = (employee.JobPosition?.Code ?? "").ToUpper();
        return code == "PHARMACIST_RT" || code == "ADMIN" || code == "OWNER";
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!IsPharmacist(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var establishmentId = employee.EstablishmentId;

        var pendingTransmissions = await _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == establishmentId && !m.SngpcSent)
            .CountAsync();

        var balancesByList = await _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == establishmentId)
            .GroupBy(m => m.ControlledList)
            .Select(g => new {
                List = g.Key,
                TotalMovements = g.Count(),
                LastMovement = g.Max(m => m.MovementDate)
            })
            .ToListAsync();

        var pendingApprovals = await _context.PharmacistApprovals
            .Where(a => a.EstablishmentId == establishmentId && a.ApprovalStatus == "PENDING")
            .CountAsync();

        var recentMovements = await _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == establishmentId)
            .OrderByDescending(m => m.MovementDate)
            .Take(10)
            .ToListAsync();

        ViewBag.PendingTransmissions = pendingTransmissions;
        ViewBag.BalancesByList = balancesByList;
        ViewBag.PendingApprovals = pendingApprovals;
        ViewBag.RecentMovements = recentMovements;

        return View();
    }

    [HttpGet("PendingApprovals")]
    public async Task<IActionResult> PendingApprovals()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanApproveControlled(employee))
        {
            TempData["Error"] = "Apenas o Farmacêutico RT pode aprovar manipulações de controlados.";
            return RedirectToAction("Index");
        }

        ViewBag.CurrentEmployee = employee;

        var pendingOrders = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == employee.EstablishmentId &&
                       o.Status == "AGUARDANDO_APROVACAO")
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return View(pendingOrders);
    }

    [HttpGet("Approve/{id}")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanApproveControlled(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == employee.EstablishmentId);

        if (order == null)
        {
            TempData["Error"] = "Ordem não encontrada.";
            return RedirectToAction("PendingApprovals");
        }

        var controlledComponents = order.Formula?.Components?
            .Where(c => c.RawMaterial != null && 
                       !string.IsNullOrEmpty(c.RawMaterial.ControlType) && 
                       c.RawMaterial.ControlType != "COMUM")
            .ToList();

        ViewBag.ControlledComponents = controlledComponents;

        return View(order);
    }

    [HttpPost("Approve/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveConfirm(Guid id, [FromForm] PharmacistApprovalForm form)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanApproveControlled(employee)) return Forbid();

        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == employee.EstablishmentId);

        if (order == null)
        {
            TempData["Error"] = "Ordem não encontrada.";
            return RedirectToAction("PendingApprovals");
        }

        if (!form.PrescriptionValid || !form.PrescriptionWithinValidity || 
            !form.DoseWithinLimits || !form.PatientDataComplete)
        {
            TempData["Error"] = "Todos os itens do checklist devem ser verificados para aprovar.";
            return RedirectToAction("Approve", new { id });
        }

        var approval = new PharmacistApproval
        {
            EstablishmentId = employee.EstablishmentId,
            ManipulationOrderId = order.Id,
            PharmacistEmployeeId = employee.Id,
            PharmacistName = employee.FullName ?? "",
            PharmacistCrf = "",
            PharmacistCrfState = "",
            ApprovalType = "PRODUCTION_RELEASE",
            ApprovalStatus = form.Approved ? "APPROVED" : "REJECTED",
            PrescriptionValid = form.PrescriptionValid,
            PrescriptionWithinValidity = form.PrescriptionWithinValidity,
            DoseWithinLimits = form.DoseWithinLimits,
            NoInteractionsDetected = form.NoInteractionsDetected,
            PatientDataComplete = form.PatientDataComplete,
            ControlledListVerified = form.ControlledListVerified,
            Observations = form.Observations,
            RejectionReason = form.RejectionReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.PharmacistApprovals.Add(approval);

        if (form.Approved)
        {
            order.Status = "APROVADO";
            TempData["Success"] = $"Ordem #{order.OrderNumber} aprovada com sucesso!";
        }
        else
        {
            order.Status = "REJEITADO";
            TempData["Warning"] = $"Ordem #{order.OrderNumber} rejeitada. Motivo: {form.RejectionReason}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Aprovação farmacêutica: Ordem {OrderId} {Status} por {Pharmacist}",
            order.Id, form.Approved ? "APROVADA" : "REJEITADA", employee.FullName);

        return RedirectToAction("PendingApprovals");
    }

    [HttpGet("Movements")]
    public async Task<IActionResult> Movements(string? list, string? status, DateTime? startDate, DateTime? endDate)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!IsPharmacist(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var query = _context.ControlledSubstanceMovements
            .Where(m => m.EstablishmentId == employee.EstablishmentId);

        if (!string.IsNullOrEmpty(list))
            query = query.Where(m => m.ControlledList == list);

        if (status == "pending")
            query = query.Where(m => !m.SngpcSent);
        else if (status == "sent")
            query = query.Where(m => m.SngpcSent);

        if (startDate.HasValue)
            query = query.Where(m => m.MovementDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MovementDate <= endDate.Value);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Take(100)
            .ToListAsync();

        ViewBag.SelectedList = list;
        ViewBag.SelectedStatus = status;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        return View(movements);
    }

    [HttpGet("Inventory")]
    public async Task<IActionResult> Inventory()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!IsPharmacist(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        var existingCheck = await _context.ControlledInventoryChecks
            .Where(c => c.EstablishmentId == employee.EstablishmentId &&
                       c.ReferenceMonth == currentMonth &&
                       c.ReferenceYear == currentYear)
            .FirstOrDefaultAsync();

        var controlledMaterials = await _context.RawMaterials
            .Include(r => r.Batches)
            .Where(r => r.EstablishmentId == employee.EstablishmentId &&
                       r.ControlType != null && r.ControlType != "COMUM")
            .OrderBy(r => r.ControlType)
            .ThenBy(r => r.Name)
            .ToListAsync();

        ViewBag.ExistingCheck = existingCheck;
        ViewBag.CurrentMonth = currentMonth;
        ViewBag.CurrentYear = currentYear;

        return View(controlledMaterials);
    }

    [HttpPost("Inventory/Start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartInventory()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanApproveControlled(employee)) return Forbid();

        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;

        var existing = await _context.ControlledInventoryChecks
            .AnyAsync(c => c.EstablishmentId == employee.EstablishmentId &&
                          c.ReferenceMonth == currentMonth &&
                          c.ReferenceYear == currentYear);

        if (existing)
        {
            TempData["Error"] = "Já existe um inventário para este mês.";
            return RedirectToAction("Inventory");
        }

        var inventoryCheck = new ControlledInventoryCheck
        {
            EstablishmentId = employee.EstablishmentId,
            ReferenceMonth = currentMonth,
            ReferenceYear = currentYear,
            CheckDate = DateTime.UtcNow,
            PerformedByEmployeeId = employee.Id,
            PharmacistEmployeeId = employee.Id,
            Status = "IN_PROGRESS",
            CreatedAt = DateTime.UtcNow
        };

        _context.ControlledInventoryChecks.Add(inventoryCheck);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Inventário iniciado. Registre as contagens físicas.";
        return RedirectToAction("InventoryDetail", new { id = inventoryCheck.Id });
    }

    [HttpGet("Inventory/{id}")]
    public async Task<IActionResult> InventoryDetail(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!IsPharmacist(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var inventoryCheck = await _context.ControlledInventoryChecks
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == employee.EstablishmentId);

        if (inventoryCheck == null)
        {
            TempData["Error"] = "Inventário não encontrado.";
            return RedirectToAction("Inventory");
        }

        var controlledMaterials = await _context.RawMaterials
            .Where(r => r.EstablishmentId == employee.EstablishmentId &&
                       r.ControlType != null && r.ControlType != "COMUM")
            .ToListAsync();

        ViewBag.ControlledMaterials = controlledMaterials;

        return View(inventoryCheck);
    }

    [HttpGet("Prescriptions")]
    public async Task<IActionResult> Prescriptions(string? type, DateTime? startDate, DateTime? endDate)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!IsPharmacist(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var query = _context.SpecialPrescriptionControls
            .Where(p => p.EstablishmentId == employee.EstablishmentId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.PrescriptionType == type);

        if (startDate.HasValue)
            query = query.Where(p => p.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.CreatedAt <= endDate.Value);

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync();

        ViewBag.SelectedType = type;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        return View(prescriptions);
    }
}

public class PharmacistApprovalForm
{
    public bool Approved { get; set; }
    public bool PrescriptionValid { get; set; }
    public bool PrescriptionWithinValidity { get; set; }
    public bool DoseWithinLimits { get; set; }
    public bool NoInteractionsDetected { get; set; }
    public bool PatientDataComplete { get; set; }
    public string? ControlledListVerified { get; set; }
    public string? Observations { get; set; }
    public string? RejectionReason { get; set; }
}
