using Microsoft.AspNetCore.Mvc;
using Data;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para páginas de Relatórios (área legada / stubs em português).
/// As rotas /Reports/* canônicas ficam em ReportsViewController; este controller
/// só responde em /Relatorios/* para não criar AmbiguousMatchException.
/// </summary>
[Route("[controller]")]
public class RelatoriosController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<RelatoriosController> _logger;

    public RelatoriosController(AppDbContext context, ILogger<RelatoriosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;

    private bool IsAuthenticated() => GetCurrentEmployee() != null;

    /// <summary>
    /// Dashboard Principal de Relatórios
    /// GET /Relatorios ou /Reports
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatórios";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Dashboard Executivo
    /// GET /Relatorios/Dashboard ou /Reports/Dashboard
    /// </summary>
    [HttpGet("Dashboard")]
    public IActionResult Dashboard()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Dashboard Executivo";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Vendas
    /// GET /Relatorios/Vendas ou /Reports/Vendas
    /// </summary>
    [HttpGet("Vendas")]
    public IActionResult Vendas()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Vendas";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Produção
    /// GET /Relatorios/Producao ou /Reports/Producao
    /// </summary>
    [HttpGet("Producao")]
    public IActionResult Producao()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Produção";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Estoque
    /// GET /Relatorios/Estoque ou /Reports/Estoque
    /// </summary>
    [HttpGet("Estoque")]
    public IActionResult Estoque()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Estoque";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório Financeiro
    /// GET /Relatorios/Financeiro ou /Reports/Financeiro
    /// </summary>
    [HttpGet("Financeiro")]
    public IActionResult Financeiro()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório Financeiro";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório SNGPC / ANVISA
    /// GET /Relatorios/SNGPC ou /Reports/SNGPC
    /// </summary>
    [HttpGet("SNGPC")]
    public IActionResult SNGPC()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "SNGPC / ANVISA";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Ordens de Manipulação
    /// GET /Relatorios/Ordens ou /Reports/Ordens
    /// </summary>
    [HttpGet("Ordens")]
    public IActionResult Ordens()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Ordens";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Rendimento
    /// GET /Relatorios/Rendimento ou /Reports/Rendimento
    /// </summary>
    [HttpGet("Rendimento")]
    public IActionResult Rendimento()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Rendimento";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Perdas
    /// GET /Relatorios/Perdas ou /Reports/Perdas
    /// </summary>
    [HttpGet("Perdas")]
    public IActionResult Perdas()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Perdas";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Custos
    /// GET /Relatorios/Custos ou /Reports/Custos
    /// </summary>
    [HttpGet("Custos")]
    public IActionResult Custos()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Custos";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Produtividade
    /// GET /Relatorios/Produtividade ou /Reports/Produtividade
    /// </summary>
    [HttpGet("Produtividade")]
    public IActionResult Produtividade()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Produtividade";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Clientes
    /// GET /Relatorios/Clientes ou /Reports/Clientes
    /// </summary>
    [HttpGet("Clientes")]
    public IActionResult Clientes()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Clientes";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }

    /// <summary>
    /// Relatório de Fornecedores
    /// GET /Relatorios/Fornecedores ou /Reports/Fornecedores
    /// </summary>
    [HttpGet("Fornecedores")]
    public IActionResult Fornecedores()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewData["Title"] = "Relatório de Fornecedores";
        ViewData["Employee"] = GetCurrentEmployee();
        return View();
    }
}
