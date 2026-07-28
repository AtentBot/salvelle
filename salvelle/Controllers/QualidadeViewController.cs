using Microsoft.AspNetCore.Mvc;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para páginas de Gestão da Qualidade
/// Rota: /Qualidade/*
/// Views em: /Views/Qualidade/
/// </summary>
[Route("Qualidade")]
public class QualidadeViewController : Controller
{
    private readonly ILogger<QualidadeViewController> _logger;

    public QualidadeViewController(ILogger<QualidadeViewController> logger)
    {
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;

    /// <summary>
    /// Dashboard de Qualidade
    /// GET /Qualidade
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        ViewData["Title"] = "Gestão da Qualidade";
        return View("~/Views/Qualidade/Index.cshtml");
    }

    /// <summary>
    /// Lista de POPs
    /// GET /Qualidade/POPs
    /// </summary>
    [HttpGet("POPs")]
    public IActionResult POPs()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        ViewData["Title"] = "Procedimentos Operacionais Padrão";
        return View("~/Views/Qualidade/POPs.cshtml");
    }

    /// <summary>
    /// Criar/Editar POP
    /// GET /Qualidade/POPs/Editar/{id?}
    /// </summary>
    [HttpGet("POPs/Editar/{id?}")]
    public IActionResult EditarPOP(Guid? id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        ViewData["Title"] = id.HasValue ? "Editar POP" : "Novo POP";
        ViewData["POPId"] = id;
        return View("~/Views/Qualidade/EditarPOP.cshtml");
    }

    /// <summary>
    /// Lista de CAPAs
    /// GET /Qualidade/CAPAs
    /// </summary>
    [HttpGet("CAPAs")]
    public IActionResult CAPAs()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        ViewData["Title"] = "Ações Corretivas e Preventivas";
        return View("~/Views/Qualidade/CAPAs.cshtml");
    }

    /// <summary>
    /// Criar/Editar CAPA
    /// GET /Qualidade/CAPAs/Editar/{id?}
    /// </summary>
    [HttpGet("CAPAs/Editar/{id?}")]
    public IActionResult EditarCAPA(Guid? id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        ViewData["Title"] = id.HasValue ? "Editar CAPA" : "Nova CAPA";
        ViewData["CAPAId"] = id;
        return View("~/Views/Qualidade/EditarCAPA.cshtml");
    }

    /// <summary>
    /// Indicadores de Qualidade
    /// GET /Qualidade/Indicadores
    /// </summary>
    [HttpGet("Indicadores")]
    public IActionResult Indicadores()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        ViewData["Title"] = "Indicadores de Qualidade";
        return View("~/Views/Qualidade/Indicadores.cshtml");
    }
}
