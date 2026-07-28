using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;

namespace Controllers;

[Route("QRCodes")]
public class QRCodesController : Controller
{
    private readonly AppDbContext _context;

    public QRCodesController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // MÉTODOS AUXILIARES - Usando nomes corretos do middleware
    // ============================================================

    private Guid GetEmployeeId()
    {
        // O middleware adiciona como: context.Items["EmployeeId"]
        if (HttpContext.Items["EmployeeId"] is Guid employeeId)
            return employeeId;

        return Guid.Empty;
    }

    private Guid GetEstablishmentId()
    {
        // O middleware adiciona como: context.Items["EstablishmentId"]
        if (HttpContext.Items["EstablishmentId"] is Guid establishmentId)
            return establishmentId;

        return Guid.Empty;
    }

    private Employee? GetEmployee()
    {
        // O middleware adiciona como: context.Items["Employee"]
        return HttpContext.Items["Employee"] as Employee;
    }

    // ============================================================
    // VIEWS
    // ============================================================

    // GET: /QRCodes
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var establishmentId = GetEstablishmentId();

        if (establishmentId == Guid.Empty)
            return RedirectToAction("Login", "Account");

        var qrcodes = await _context.Set<EstablishmentQRCode>()
            .Where(q => q.EstablishmentId == establishmentId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == establishmentId);

        ViewBag.QRCodes = qrcodes;
        ViewBag.Establishment = establishment;

        return View();
    }

    // GET: /QRCodes/Print/{id}
    [HttpGet("Print/{id:guid}")]
    public async Task<IActionResult> Print(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        if (establishmentId == Guid.Empty)
            return RedirectToAction("Login", "Account");

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .Include(q => q.Establishment)
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound();

        return View(qrcode);
    }
}