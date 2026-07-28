using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class StockController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Movements()
    {
        return View();
    }

    public IActionResult LowStock()
    {
        return View();
    }

    public IActionResult Adjust()
    {
        return View();
    }
}
