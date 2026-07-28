using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Controller MVC para servir as Views de Fornecedores
/// A API está em SuppliersController (ApiController)
/// </summary>
public class SuppliersViewController : Controller
{
    private readonly ILogger<SuppliersViewController> _logger;

    public SuppliersViewController(ILogger<SuppliersViewController> logger)
    {
        _logger = logger;
    }

    // GET: /suppliers
    [HttpGet("/suppliers")]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /suppliers/create
    [HttpGet("/suppliers/create")]
    public IActionResult Create()
    {
        return View();
    }

    // GET: /suppliers/edit/{id}
    [HttpGet("/suppliers/edit/{id}")]
    public IActionResult Edit(Guid id)
    {
        ViewData["SupplierId"] = id;
        return View();
    }

    // GET: /suppliers/details/{id}
    [HttpGet("/suppliers/details/{id}")]
    public IActionResult Details(Guid id)
    {
        ViewData["SupplierId"] = id;
        return View();
    }
}
