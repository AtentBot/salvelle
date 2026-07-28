using Microsoft.AspNetCore.Mvc;
using Models.Employees;

namespace Controllers;

/// <summary>
/// MVC controller para a página de suporte da farmácia.
/// Auth via EmployeeAuthMiddleware → HttpContext.Items["Employee"].
/// </summary>
[Route("Support")]
public class SupportController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Redirect("/Account/Login");

        ViewBag.Establishment = employee.Establishment;
        return View();
    }
}
