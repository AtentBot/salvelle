using Microsoft.AspNetCore.Mvc;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para páginas de Precificação
/// Rota: /Precificacao/*
/// </summary>
[Route("Precificacao")]
public class PrecificacaoController : Controller
{
    private readonly ILogger<PrecificacaoController> _logger;

    public PrecificacaoController(ILogger<PrecificacaoController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Dashboard de Precificação
    /// GET /Precificacao
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["Employee"] = employee;
        return View("PricingDashboard");
    }

    /// <summary>
    /// Configurações de Precificação
    /// GET /Precificacao/Configuracoes
    /// </summary>
    [HttpGet("Configuracoes")]
    public IActionResult Configuracoes()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verificar permissão
        var jobCode = employee.JobPosition?.Code?.ToUpper();
        var canManage = jobCode == "MANAGER" || jobCode == "GERENTE" || 
                        jobCode == "PROPRIETARIO" || jobCode == "ADMIN" ||
                        jobCode == "PHARMACIST_RT" || jobCode == "FARMACEUTICO_RT";

        if (!canManage)
        {
            TempData["Error"] = "Você não tem permissão para acessar esta página.";
            return RedirectToAction("Index");
        }

        ViewData["Employee"] = employee;
        return View("PricingSettings");
    }

    /// <summary>
    /// Simulador de Preços
    /// GET /Precificacao/Simulador
    /// </summary>
    [HttpGet("Simulador")]
    public IActionResult Simulador()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["Employee"] = employee;
        return View("PriceSimulator");
    }
}
