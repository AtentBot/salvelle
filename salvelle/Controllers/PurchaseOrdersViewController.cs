using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class PurchaseOrdersViewController : Controller
{
    [HttpGet("/compras")]
    public IActionResult Index()
    {
        return View("~/Views/PurchaseOrders/Index.cshtml");
    }

    [HttpGet("/compras/criar")]
    public IActionResult Create()
    {
        return View("~/Views/PurchaseOrders/Create.cshtml");
    }

    [HttpGet("/compras/{id}")]
    public IActionResult Details(int id)
    {
        ViewBag.OrderId = id;
        return View("~/Views/PurchaseOrders/Details.cshtml");
    }

    [HttpGet("/compras/{id}/receber")]
    public IActionResult Receive(int id)
    {
        ViewBag.OrderId = id;
        return View("~/Views/PurchaseOrders/Receive.cshtml");
    }
}
