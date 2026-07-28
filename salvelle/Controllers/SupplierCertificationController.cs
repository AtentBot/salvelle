using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;

namespace Controllers;

/// <summary>
/// Controller MVC para gerenciar Certificações AFE de Fornecedores
/// Usa o model SupplierCertificate (Models.Pharmacy)
/// </summary>
[Route("[controller]")]
public class SupplierCertificationController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<SupplierCertificationController> _logger;

    public SupplierCertificationController(AppDbContext context, ILogger<SupplierCertificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee()
    {
        return HttpContext.Items["Employee"] as Employee;
    }

    private bool CanManageCertifications(Employee employee)
    {
        var code = (employee.JobPosition?.Code ?? "").ToUpper();
        return code == "PHARMACIST_RT" || code == "PHARMACIST" || code == "ADMIN" || 
               code == "OWNER" || code == "GENERAL_MANAGER" || code == "MANAGER";
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? status, string? search)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var query = _context.SupplierCertificates
            .Include(c => c.Supplier)
            .Where(c => c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Supplier!.CompanyName.Contains(search) || 
                                    c.Supplier.TradeName!.Contains(search) ||
                                    c.Number!.Contains(search) ||
                                    c.Name.Contains(search));

        var certificates = await query
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();

        // Estatísticas
        var establishmentId = employee.EstablishmentId;
        var stats = new CertificationStats
        {
            TotalActive = await _context.SupplierCertificates
                .Where(c => c.Supplier!.EstablishmentId == establishmentId && c.IsActive && c.Status == "Válido")
                .CountAsync(),
            
            ExpiringIn30Days = await _context.SupplierCertificates
                .Where(c => c.Supplier!.EstablishmentId == establishmentId && 
                           c.IsActive &&
                           c.ExpiryDate != null &&
                           c.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                           c.ExpiryDate > DateTime.UtcNow)
                .CountAsync(),
            
            Expired = await _context.SupplierCertificates
                .Where(c => c.Supplier!.EstablishmentId == establishmentId && 
                           c.IsActive &&
                           c.ExpiryDate != null &&
                           c.ExpiryDate <= DateTime.UtcNow)
                .CountAsync(),
            
            PendingVerification = await _context.SupplierCertificates
                .Where(c => c.Supplier!.EstablishmentId == establishmentId && c.Status == "Pendente")
                .CountAsync()
        };

        ViewBag.Stats = stats;
        ViewBag.SelectedStatus = status;
        ViewBag.Search = search;

        return View(certificates);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var suppliers = await _context.Suppliers
            .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive)
            .OrderBy(s => s.CompanyName)
            .ToListAsync();

        ViewBag.Suppliers = suppliers;

        return View();
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubmit([FromForm] SupplierCertificateForm form)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        if (!ModelState.IsValid)
        {
            ViewBag.CurrentEmployee = employee;
            var suppliers = await _context.Suppliers
                .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .OrderBy(s => s.CompanyName)
                .ToListAsync();
            ViewBag.Suppliers = suppliers;
            return View("Create", form);
        }

        var certificate = new SupplierCertificate
        {
            SupplierId = form.SupplierId,
            CertificateType = form.CertificateType ?? "AFE",
            Name = form.Name ?? "Autorização de Funcionamento",
            Number = form.Number,
            IssuingAuthority = form.IssuingAuthority ?? "ANVISA",
            IssueDate = form.IssueDate,
            ExpiryDate = form.ExpiryDate,
            Status = "Válido",
            Notes = form.Notes,
            AlertBeforeExpiry = true,
            AlertDaysBefore = 30,
            IsActive = true,
            CreatedByEmployeeId = employee.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SupplierCertificates.Add(certificate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificado cadastrado: {Number} para fornecedor {SupplierId} por {Employee}",
            form.Number, form.SupplierId, employee.FullName);

        TempData["Success"] = "Certificado cadastrado com sucesso!";
        return RedirectToAction("Index");
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var certificate = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id && c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (certificate == null)
        {
            TempData["Error"] = "Certificado não encontrado.";
            return RedirectToAction("Index");
        }

        return View(certificate);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var certificate = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id && c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (certificate == null)
        {
            TempData["Error"] = "Certificado não encontrado.";
            return RedirectToAction("Index");
        }

        var suppliers = await _context.Suppliers
            .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive)
            .OrderBy(s => s.CompanyName)
            .ToListAsync();

        ViewBag.Suppliers = suppliers;

        return View(certificate);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSubmit(Guid id, [FromForm] SupplierCertificateForm form)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        var certificate = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id && c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (certificate == null)
        {
            TempData["Error"] = "Certificado não encontrado.";
            return RedirectToAction("Index");
        }

        certificate.CertificateType = form.CertificateType ?? certificate.CertificateType;
        certificate.Name = form.Name ?? certificate.Name;
        certificate.Number = form.Number;
        certificate.IssuingAuthority = form.IssuingAuthority ?? "ANVISA";
        certificate.IssueDate = form.IssueDate;
        certificate.ExpiryDate = form.ExpiryDate;
        certificate.Notes = form.Notes;
        certificate.UpdatedByEmployeeId = employee.Id;
        certificate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificado atualizado: {Id} por {Employee}", id, employee.FullName);

        TempData["Success"] = "Certificado atualizado com sucesso!";
        return RedirectToAction("Details", new { id });
    }

    [HttpGet("Renew/{id}")]
    public async Task<IActionResult> Renew(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var certificate = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id && c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (certificate == null)
        {
            TempData["Error"] = "Certificado não encontrado.";
            return RedirectToAction("Index");
        }

        return View(certificate);
    }

    [HttpPost("Renew/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenewSubmit(Guid id, [FromForm] RenewCertificateForm form)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        var certificate = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id && c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (certificate == null)
        {
            TempData["Error"] = "Certificado não encontrado.";
            return RedirectToAction("Index");
        }

        certificate.Number = form.NewNumber ?? certificate.Number;
        certificate.IssueDate = form.NewIssueDate ?? DateTime.UtcNow;
        certificate.ExpiryDate = form.NewExpiryDate;
        certificate.Status = "Válido";
        certificate.UpdatedByEmployeeId = employee.Id;
        certificate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificado renovado: {Id} até {NewExpiry} por {Employee}",
            id, form.NewExpiryDate, employee.FullName);

        TempData["Success"] = "Certificado renovado com sucesso!";
        return RedirectToAction("Index");
    }

    [HttpPost("Revoke/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id, [FromForm] string reason)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        var certificate = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id && c.Supplier!.EstablishmentId == employee.EstablishmentId);

        if (certificate == null)
        {
            TempData["Error"] = "Certificado não encontrado.";
            return RedirectToAction("Index");
        }

        certificate.Status = "Revogado";
        certificate.IsActive = false;
        certificate.Notes = (certificate.Notes ?? "") + $"\n[{DateTime.UtcNow:dd/MM/yyyy}] Revogado: {reason}";
        certificate.UpdatedByEmployeeId = employee.Id;
        certificate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificado revogado: {Id} por {Employee}. Motivo: {Reason}",
            id, employee.FullName, reason);

        TempData["Warning"] = "Certificado revogado.";
        return RedirectToAction("Index");
    }

    [HttpGet("Expiring")]
    public async Task<IActionResult> Expiring()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!CanManageCertifications(employee)) return Forbid();

        ViewBag.CurrentEmployee = employee;

        var expiringCertificates = await _context.SupplierCertificates
            .Include(c => c.Supplier)
            .Where(c => c.Supplier!.EstablishmentId == employee.EstablishmentId &&
                       c.IsActive &&
                       c.ExpiryDate != null &&
                       c.ExpiryDate <= DateTime.UtcNow.AddDays(60))
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();

        return View(expiringCertificates);
    }

    [HttpGet("VerifyAnvisa/{number}")]
    public async Task<IActionResult> VerifyAnvisa(string number)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return Json(new { error = "Não autenticado" });

        await Task.Delay(500);

        return Json(new
        {
            valid = true,
            number = number,
            message = "Verifique manualmente em https://consultas.anvisa.gov.br/"
        });
    }
}

public class CertificationStats
{
    public int TotalActive { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int Expired { get; set; }
    public int PendingVerification { get; set; }
}

public class SupplierCertificateForm
{
    public Guid SupplierId { get; set; }
    public string? CertificateType { get; set; }
    public string? Name { get; set; }
    public string? Number { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class RenewCertificateForm
{
    public string? NewNumber { get; set; }
    public DateTime? NewIssueDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
}
