using Microsoft.AspNetCore.Mvc;
using Data;

namespace Controllers;

/// <summary>
/// MVC Controller para Views de Workflow de Manipulação
/// Rotas: /Workflow, /Workflow/Kanban, /Workflow/Dashboard
/// </summary>
[Route("[controller]")]
public class WorkflowController : Controller
{
    private readonly AppDbContext _context;

    public WorkflowController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Workflow de manipulação - lista com etapas visuais
    /// GET /Workflow
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View("~/Views/Manipulacoes/Workflow.cshtml");
    }

    /// <summary>
    /// Kanban de produção
    /// GET /Workflow/Kanban
    /// </summary>
    [HttpGet("Kanban")]
    public IActionResult Kanban()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View("~/Views/Manipulacoes/Kanban.cshtml");
    }

    /// <summary>
    /// Dashboard de produção
    /// GET /Workflow/Dashboard
    /// </summary>
    [HttpGet("Dashboard")]
    public IActionResult Dashboard()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        // Tentar carregar view Dashboard, se não existir usa Workflow
        try
        {
            return View("~/Views/Manipulacoes/Dashboard.cshtml");
        }
        catch
        {
            return View("~/Views/Manipulacoes/Workflow.cshtml");
        }
    }

    /// <summary>
    /// Criar nova ordem - redireciona para ManipulacoesController
    /// GET /Workflow/Create
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        return RedirectToAction("Create", "Manipulacoes");
    }

    /// <summary>
    /// Detalhes da ordem
    /// GET /Workflow/Details/{id}
    /// </summary>
    [HttpGet("Details/{id}")]
    public IActionResult Details(Guid id)
    {
        return RedirectToAction("Details", "Manipulacoes", new { id });
    }

    /// <summary>
    /// Produção de ordem específica
    /// GET /Workflow/Producao/{id}
    /// </summary>
    [HttpGet("Producao/{id}")]
    public IActionResult Producao(Guid id)
    {
        return RedirectToAction("ProducaoOrdem", "Manipulacoes", new { id });
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }
}
