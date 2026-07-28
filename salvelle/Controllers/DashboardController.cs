using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Helpers;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Dashboard Principal - Tela Inicial do Sistema
/// Rota: /Dashboard
/// </summary>
[Route("Dashboard")]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;
    private Guid GetEstablishmentId() => GetCurrentEmployee()?.EstablishmentId ?? Guid.Empty;

    /// <summary>
    /// Dashboard Principal - Tela Inicial
    /// GET /Dashboard
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var dashboard = new DashboardHomeDto();

        // ══════════════════════════════════════════════════════════════
        // ALERTAS E PENDÊNCIAS
        // ══════════════════════════════════════════════════════════════
        
        // Ordens atrasadas (não fechadas: nem finalizadas/entregues nem canceladas)
        dashboard.TotalAtrasados = await _context.ManipulationOrders
            .CountAsync(o => o.EstablishmentId == establishmentId &&
                            o.ExpectedDate < DateTime.UtcNow &&
                            !ManipulationStatus.Closed.Contains(o.Status));

        // Prescrições pendentes
        dashboard.PrescriptionsPendentes = await _context.Prescriptions
            .CountAsync(p => p.EstablishmentId == establishmentId && 
                            p.Status == "PENDENTE");

        // Orçamentos pendentes
        dashboard.OrcamentosPendentes = await _context.PrescriptionQuotes
            .CountAsync(q => q.EstablishmentId == establishmentId && 
                            q.Status == "PENDENTE");

        // Estoque baixo
        dashboard.LowStockItems = await _context.RawMaterials
            .CountAsync(r => r.EstablishmentId == establishmentId && 
                            r.CurrentStock <= r.MinimumStock && 
                            r.IsActive);

        // ══════════════════════════════════════════════════════════════
        // PERMISSÕES
        // ══════════════════════════════════════════════════════════════
        
        var jobCode = employee.JobPosition?.Code?.ToUpper() ?? "";
        var employeeCodes = new[] { "OWNER", "MANAGER", "HR", "ADMIN", "GENERAL_MANAGER" };
        dashboard.CanManageEmployees = employeeCodes.Contains(jobCode);

        // ══════════════════════════════════════════════════════════════
        // VIEWBAG PARA DADOS EXTRAS
        // ══════════════════════════════════════════════════════════════
        
        // Pedidos prontos para retirada
        ViewBag.PedidosProntos = await _context.ManipulationOrders
            .CountAsync(o => o.EstablishmentId == establishmentId && 
                            o.Status == "PRONTO_RETIRADA");

        // Pedidos online pendentes
        ViewBag.PedidosPendentes = await _context.ManipulationOrders
            .CountAsync(o => o.EstablishmentId == establishmentId && 
                            o.Status == "PENDENTE" );

        // Establishment info
        var establishment = await _context.Establishments.FindAsync(establishmentId);
        ViewBag.Establishment = establishment;
        ViewBag.Employee = employee;

        return View("Index", dashboard);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DTO para Dashboard Principal
// ════════════════════════════════════════════════════════════════════════════

public class DashboardHomeDto
{
    // Alertas e Pendências (usados pelo Index.cshtml existente)
    public int TotalAtrasados { get; set; }
    public int PrescriptionsPendentes { get; set; }
    public int OrcamentosPendentes { get; set; }
    public int LowStockItems { get; set; }
    
    // Permissões
    public bool CanManageEmployees { get; set; }
}
