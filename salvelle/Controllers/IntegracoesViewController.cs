using Microsoft.AspNetCore.Mvc;
using Data;

namespace Controllers;

/// <summary>
/// Controller MVC para Views de Integrações
/// </summary>
public class IntegracoesViewController : Controller
{
    private readonly AppDbContext _db;

    public IntegracoesViewController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Página de configuração de integrações
    /// GET /Integracoes
    /// </summary>
    [HttpGet]
    [Route("Integracoes")]
    public IActionResult Index()
    {
        return View("~/Views/Integracoes/Index.cshtml");
    }

    /// <summary>
    /// Página de configuração de balança
    /// GET /Integracoes/Balanca
    /// </summary>
    [HttpGet]
    [Route("Integracoes/Balanca")]
    public IActionResult Balanca()
    {
        return View("~/Views/Integracoes/Balanca.cshtml");
    }

    /// <summary>
    /// Página de configuração de impressoras
    /// GET /Integracoes/Impressoras
    /// </summary>
    [HttpGet]
    [Route("Integracoes/Impressoras")]
    public IActionResult Impressoras()
    {
        return View("~/Views/Integracoes/Impressoras.cshtml");
    }
}
