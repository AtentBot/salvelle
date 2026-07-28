using Microsoft.AspNetCore.Mvc;

namespace Controllers;

/// <summary>
/// Controller MVC para Views de Matérias-Primas
/// Rotas: /materias-primas/*
/// </summary>
public class RawMaterialsViewController : Controller
{
    /// <summary>
    /// Lista de matérias-primas
    /// GET /materias-primas
    /// </summary>
    [HttpGet("/materias-primas")]
    public IActionResult Index()
    {
        return View("~/Views/RawMaterials/Index.cshtml");
    }

    /// <summary>
    /// Formulário de criação
    /// GET /materias-primas/criar
    /// </summary>
    [HttpGet("/materias-primas/criar")]
    public IActionResult Create()
    {
        return View("~/Views/RawMaterials/Create.cshtml");
    }

    /// <summary>
    /// Detalhes de uma matéria-prima
    /// GET /materias-primas/{id}
    /// </summary>
    [HttpGet("/materias-primas/{id:guid}")]
    public IActionResult Details(Guid id)
    {
        ViewBag.RawMaterialId = id;
        return View("~/Views/RawMaterials/Details.cshtml");
    }

    /// <summary>
    /// Formulário de edição
    /// GET /materias-primas/{id}/editar
    /// </summary>
    [HttpGet("/materias-primas/{id:guid}/editar")]
    public IActionResult Edit(Guid id)
    {
        ViewBag.RawMaterialId = id;
        return View("~/Views/RawMaterials/Edit.cshtml");
    }

    /// <summary>
    /// Gestão de lotes
    /// GET /materias-primas/{id}/lotes
    /// </summary>
    [HttpGet("/materias-primas/{id:guid}/lotes")]
    public IActionResult Batches(Guid id)
    {
        ViewBag.RawMaterialId = id;
        return View("~/Views/RawMaterials/Batches.cshtml");
    }

    /// <summary>
    /// Dashboard de Preços
    /// GET /materias-primas/precos
    /// </summary>
    [HttpGet("/materias-primas/precos")]
    public IActionResult Precos()
    {
        return View("~/Views/RawMaterials/Precos.cshtml");
    }
}