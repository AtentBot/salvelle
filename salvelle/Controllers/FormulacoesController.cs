using Microsoft.AspNetCore.Mvc;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Calculadora de Formulações
/// (Soluções, Xaropes, Sachês, etc.)
/// Rota: /Formulacoes/*
/// </summary>
[Route("Formulacoes")]
public class FormulacoesController : Controller
{
    private readonly ILogger<FormulacoesController> _logger;

    public FormulacoesController(ILogger<FormulacoesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculadora Unificada
    /// GET /Formulacoes ou /Formulacoes/Calculadora
    /// </summary>
    [HttpGet("")]
    [HttpGet("Calculadora")]
    public IActionResult Calculadora()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewData["Employee"] = employee;
        return View();
    }

    /// <summary>
    /// Calculadora de Soluções
    /// GET /Formulacoes/Solucoes
    /// </summary>
    [HttpGet("Solucoes")]
    public IActionResult Solucoes()
    {
        ViewData["FormType"] = "liquid";
        return Calculadora();
    }

    /// <summary>
    /// Calculadora de Xaropes
    /// GET /Formulacoes/Xaropes
    /// </summary>
    [HttpGet("Xaropes")]
    public IActionResult Xaropes()
    {
        ViewData["FormType"] = "syrup";
        return Calculadora();
    }

    /// <summary>
    /// Calculadora de Gotas
    /// GET /Formulacoes/Gotas
    /// </summary>
    [HttpGet("Gotas")]
    public IActionResult Gotas()
    {
        ViewData["FormType"] = "drops";
        return Calculadora();
    }

    /// <summary>
    /// Calculadora de Sachês
    /// GET /Formulacoes/Saches
    /// </summary>
    [HttpGet("Saches")]
    public IActionResult Saches()
    {
        ViewData["FormType"] = "powder";
        return Calculadora();
    }

    /// <summary>
    /// Calculadora de Suspensões
    /// GET /Formulacoes/Suspensoes
    /// </summary>
    [HttpGet("Suspensoes")]
    public IActionResult Suspensoes()
    {
        ViewData["FormType"] = "suspension";
        return Calculadora();
    }
}
