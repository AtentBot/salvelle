using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;

namespace Controllers;

[Route("OnlineOrders")]
public class OnlineOrdersController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<OnlineOrdersController> _logger;

    public OnlineOrdersController(AppDbContext context, ILogger<OnlineOrdersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /OnlineOrders
    [HttpGet("")]
    public async Task<IActionResult> Index(string? status = null, string? search = null, int page = 1)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Redirect("/Account/Login");

        var establishmentId = employee.EstablishmentId;

        var query = _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.EstablishmentId == establishmentId);

        // Filtro por status
        if (!string.IsNullOrEmpty(status) && status != "todos")
        {
            query = query.Where(o => o.Status == status);
        }

        // Busca por número do pedido ou nome do cliente
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                o.Customer!.FullName.Contains(search) ||
                o.Customer.Phone!.Contains(search));
        }

        // Contadores por status
        var allOrders = _context.Set<OnlineOrder>()
            .Where(o => o.EstablishmentId == establishmentId);

        ViewBag.CountPending = await allOrders.CountAsync(o => o.Status == "PENDING");
        ViewBag.CountConfirmed = await allOrders.CountAsync(o => o.Status == "CONFIRMED");
        ViewBag.CountPreparing = await allOrders.CountAsync(o => o.Status == "PREPARING");
        ViewBag.CountReady = await allOrders.CountAsync(o => o.Status == "READY");
        ViewBag.CountDelivered = await allOrders.CountAsync(o => o.Status == "DELIVERED");
        ViewBag.CountCancelled = await allOrders.CountAsync(o => o.Status == "CANCELLED");
        ViewBag.CountTotal = await allOrders.CountAsync();

        // Paginação
        var pageSize = 20;
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Orders = orders;
        ViewBag.CurrentStatus = status;
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.Establishment = employee.Establishment;

        return View();
    }

    // GET: /OnlineOrders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Redirect("/Account/Login");

        var establishmentId = employee.EstablishmentId;

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Items!)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound();

        ViewBag.Establishment = employee.Establishment;

        return View(order);
    }

    // GET: /OnlineOrders/Dashboard
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Redirect("/Account/Login");

        var establishmentId = employee.EstablishmentId;
        var today = DateTime.UtcNow.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        // Pedidos do dia
        var ordersToday = await _context.Set<OnlineOrder>()
            .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt >= today)
            .ToListAsync();

        // Pedidos da semana
        var ordersWeek = await _context.Set<OnlineOrder>()
            .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt >= startOfWeek)
            .ToListAsync();

        // Pedidos do mês
        var ordersMonth = await _context.Set<OnlineOrder>()
            .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt >= startOfMonth)
            .ToListAsync();

        // Pedidos pendentes de ação
        var pendingAction = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Where(o => o.EstablishmentId == establishmentId &&
                       (o.Status == "PENDING" || o.Status == "CONFIRMED" || o.Status == "PREPARING"))
            .OrderBy(o => o.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Pedidos prontos para retirada
        var readyOrders = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Where(o => o.EstablishmentId == establishmentId && o.Status == "READY")
            .OrderBy(o => o.ReadyAt)
            .ToListAsync();

        ViewBag.OrdersToday = ordersToday;
        ViewBag.OrdersWeek = ordersWeek;
        ViewBag.OrdersMonth = ordersMonth;
        ViewBag.PendingAction = pendingAction;
        ViewBag.ReadyOrders = readyOrders;

        // Métricas
        ViewBag.TodayCount = ordersToday.Count;
        ViewBag.TodayTotal = ordersToday.Sum(o => o.Total);
        ViewBag.WeekCount = ordersWeek.Count;
        ViewBag.WeekTotal = ordersWeek.Sum(o => o.Total);
        ViewBag.MonthCount = ordersMonth.Count;
        ViewBag.MonthTotal = ordersMonth.Sum(o => o.Total);

        ViewBag.Establishment = employee.Establishment;

        return View();
    }
}