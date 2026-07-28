using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class CustomersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Details(Guid id)
    {
        return View();
    }

    public IActionResult Edit(Guid id)
    {
        return View();
    }
}
