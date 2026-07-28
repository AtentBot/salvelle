using Microsoft.AspNetCore.Mvc;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Formulações Tópicas (Cremes, Géis, Pomadas)
/// Rota: /Topicos/*
/// </summary>
[Route("Topicos")]
public class TopicosController : Controller
{
    private readonly ILogger<TopicosController> _logger;

    public TopicosController(ILogger<TopicosController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculadora de Formulações Tópicas
    /// GET /Topicos/Calculadora
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
        return View("Calculadora");
    }

    /// <summary>
    /// Alias para Calculadora de Cremes
    /// GET /Topicos/Cremes
    /// </summary>
    [HttpGet("Cremes")]
    public IActionResult Cremes()
    {
        return RedirectToAction("Calculadora");
    }

    /// <summary>
    /// Alias para Calculadora de Géis
    /// GET /Topicos/Geis
    /// </summary>
    [HttpGet("Geis")]
    public IActionResult Geis()
    {
        return RedirectToAction("Calculadora");
    }

    /// <summary>
    /// Alias para Calculadora de Pomadas
    /// GET /Topicos/Pomadas
    /// </summary>
    [HttpGet("Pomadas")]
    public IActionResult Pomadas()
    {
        return RedirectToAction("Calculadora");
    }
}
