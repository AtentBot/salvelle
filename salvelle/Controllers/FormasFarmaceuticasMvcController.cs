using Microsoft.AspNetCore.Mvc;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Views de Formas Farmacêuticas e Precificação
/// Rota: /FormasFarmaceuticas
/// </summary>
[Route("FormasFarmaceuticas")]
public class FormasFarmaceuticasController : Controller
{
    private readonly ILogger<FormasFarmaceuticasController> _logger;

    public FormasFarmaceuticasController(ILogger<FormasFarmaceuticasController> logger)
    {
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORMAS FARMACÊUTICAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET: /FormasFarmaceuticas
    /// Lista de formas farmacêuticas
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// GET: /FormasFarmaceuticas/Create
    /// Criar nova forma farmacêutica personalizada
    /// </summary>
    [HttpGet("Create")]
    public IActionResult Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// GET: /FormasFarmaceuticas/Edit/{id}
    /// Editar forma farmacêutica
    /// </summary>
    [HttpGet("Edit/{id}")]
    public IActionResult Edit(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["FormId"] = id;
        return View();
    }

    /// <summary>
    /// GET: /FormasFarmaceuticas/Details/{id}
    /// Detalhes e subtipos da forma farmacêutica
    /// </summary>
    [HttpGet("Details/{id}")]
    public IActionResult Details(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["FormId"] = id;
        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SUBTIPOS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET: /FormasFarmaceuticas/{formId}/Subtipos
    /// Lista subtipos de uma forma
    /// </summary>
    [HttpGet("{formId}/Subtipos")]
    public IActionResult Subtipos(Guid formId)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["FormId"] = formId;
        return View();
    }

    /// <summary>
    /// GET: /FormasFarmaceuticas/Subtipos/Create/{formId}
    /// Criar novo subtipo
    /// </summary>
    [HttpGet("Subtipos/Create/{formId}")]
    public IActionResult CreateSubtipo(Guid formId)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["FormId"] = formId;
        return View();
    }

    /// <summary>
    /// GET: /FormasFarmaceuticas/Subtipos/Edit/{id}
    /// Editar subtipo
    /// </summary>
    [HttpGet("Subtipos/Edit/{id}")]
    public IActionResult EditSubtipo(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["SubtypeId"] = id;
        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONFIGURAÇÃO DE PRECIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET: /FormasFarmaceuticas/Precificacao
    /// Configuração de taxas e impostos
    /// </summary>
    [HttpGet("Precificacao")]
    public IActionResult Precificacao()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TAMANHOS DE CÁPSULAS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET: /FormasFarmaceuticas/Capsulas
    /// Configuração de tamanhos de cápsulas
    /// </summary>
    [HttpGet("Capsulas")]
    public IActionResult Capsulas()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// GET: /FormasFarmaceuticas/Capsulas/Calculadora
    /// Calculadora de cápsulas
    /// </summary>
    [HttpGet("Capsulas/Calculadora")]
    public IActionResult Calculadora()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }
}
