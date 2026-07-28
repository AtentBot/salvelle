using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class PaymentManagementController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Configuration()
    {
        return View();
    }

    [HttpGet]
    public IActionResult DailyReport()
    {
        return View();
    }

    [HttpGet]
    public IActionResult PaymentMethods()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GatewaySettings()
    {
        return View();
    }

    [HttpGet]
    public IActionResult PendingPayments()
    {
        return View();
    }
}
