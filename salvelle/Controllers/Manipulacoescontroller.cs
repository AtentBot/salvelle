using Microsoft.AspNetCore.Mvc;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

/// <summary>
/// MVC Controller para Views de Manipulações
/// Serve páginas HTML para o gerenciamento visual do workflow
/// Rota: /Manipulacoes
/// </summary>
[Route("[controller]")]
public class ManipulacoesController : Controller
{
    private readonly AppDbContext _context;

    public ManipulacoesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista de ordens de manipulação
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Dashboard de produção
    /// </summary>
    [HttpGet("Dashboard")]
    public IActionResult Dashboard()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Workflow visual de manipulação
    /// </summary>
    [HttpGet("Workflow")]
    public IActionResult Workflow()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Detalhes de uma ordem específica
    /// </summary>
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == GetEstablishmentId());

        if (order == null)
            return NotFound();

        return View(order);
    }

    /// <summary>
    /// Criar nova ordem de manipulação
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Editar ordem de manipulação
    /// </summary>
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == GetEstablishmentId());

        if (order == null)
            return NotFound();

        return View(order);
    }

    /// <summary>
    /// Tela de produção (interface touch para laboratório)
    /// </summary>
    [HttpGet("Producao")]
    public IActionResult Producao()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Tela de produção para ordem específica
    /// </summary>
    [HttpGet("Producao/{id}")]
    public async Task<IActionResult> ProducaoOrdem(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .Include(o => o.ManipulatedByEmployee)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == GetEstablishmentId());

        if (order == null)
            return NotFound();

        return View(order);
    }

    /// <summary>
    /// Ficha de pesagem para impressão
    /// </summary>
    [HttpGet("FichaPesagem/{id}")]
    public async Task<IActionResult> FichaPesagem(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == GetEstablishmentId());

        if (order == null)
            return NotFound();

        return View(order);
    }

    /// <summary>
    /// Histórico de etapas de uma ordem
    /// </summary>
    [HttpGet("Historico/{id}")]
    public async Task<IActionResult> Historico(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == GetEstablishmentId());

        if (order == null)
            return NotFound();

        var steps = await _context.ManipulationSteps
            .Include(s => s.PerformedByEmployee)
            .Include(s => s.CheckedByEmployee)
            .Where(s => s.ManipulationOrderId == id)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        ViewBag.Order = order;
        return View(steps);
    }

    /// <summary>
    /// Tela de pesagem de componentes
    /// </summary>
    [HttpGet("Weighing/{id}")]
    public async Task<IActionResult> Weighing(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == GetEstablishmentId());

        if (order == null)
            return NotFound();

        // Validar status
        var validStatuses = new[] { "PENDENTE", "EM_PRODUCAO", "PESAGEM" };
        if (!validStatuses.Contains(order.Status))
        {
            TempData["Error"] = $"Ordem não pode iniciar pesagem. Status atual: {order.Status}";
            return RedirectToAction("Details", new { id });
        }

        var viewModel = new ViewModels.Manipulation.WeighingViewModel
        {
            ManipulationOrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            FormulaName = order.Formula?.Name ?? "Fórmula Livre",
            QuantityToProduce = order.QuantityToProduce,
            Unit = order.Unit
        };

        // Carregar componentes e lotes disponíveis
        if (order.Formula?.Components != null)
        {
            foreach (var component in order.Formula.Components.OrderBy(c => c.OrderIndex))
            {
                var totalQuantity = component.Quantity * order.QuantityToProduce;

                // Buscar lotes disponíveis (FEFO)
                var batches = await _context.Batches
                    .Where(b => b.RawMaterialId == component.RawMaterialId &&
                               b.Status == "APROVADO" &&
                               b.CurrentQuantity > 0 &&
                               b.ExpiryDate > DateTime.UtcNow)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new ViewModels.Manipulation.AvailableBatchItem
                    {
                        BatchId = b.Id,
                        BatchNumber = b.BatchNumber,
                        AvailableQuantity = b.CurrentQuantity,
                        ExpiryDate = b.ExpiryDate,
                        IsExpiringSoon = b.ExpiryDate < DateTime.UtcNow.AddDays(30)
                    })
                    .Take(5)
                    .ToListAsync();

                viewModel.Components.Add(new ViewModels.Manipulation.ComponentWeighingItem
                {
                    ComponentId = component.Id,
                    RawMaterialName = component.RawMaterial?.Name ?? "",
                    DcbCode = component.RawMaterial?.DcbCode ?? "",
                    UnitQuantity = component.Quantity,
                    TotalQuantity = totalQuantity,
                    Unit = component.Unit,
                    IsControlled = component.RawMaterial?.ControlType != "COMUM",
                    AvailableBatches = batches
                });
            }
        }

        return View(viewModel);
    }

    /// <summary>
    /// Kanban de produção
    /// </summary>
    [HttpGet("Kanban")]
    public IActionResult Kanban()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }
}
