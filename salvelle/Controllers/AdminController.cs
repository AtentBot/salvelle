using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/admin")]
    public IActionResult Index()
    {
        return RedirectToAction("Dashboard");
    }

    [HttpGet("/admin/login")]
    public IActionResult Login()
    {
        if (HttpContext.Items["SaasAdmin"] != null)
        {
            return RedirectToAction("Index", "AdminDashboard");
        }
        return View();
    }

    [HttpGet("/admin/forgot-password")]
    public IActionResult ForgotPassword()
    {
        // Se já estiver logado, redirecionar
        if (HttpContext.Items["SaasAdmin"] != null)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [HttpGet("/admin/reset-password")]
    public IActionResult ResetPassword([FromQuery] string? token)
    {
        // Se já estiver logado, redirecionar
        if (HttpContext.Items["SaasAdmin"] != null)
        {
            return RedirectToAction("Dashboard");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Login");
        }

        ViewBag.Token = token;
        return View();
    }

    [HttpGet("/admin/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/admin/establishments")]
    public IActionResult Establishments()
    {
        return View();
    }

    [HttpGet("/admin/establishments/{id}")]
    public IActionResult EstablishmentDetails(Guid id)
    {
        ViewBag.EstablishmentId = id;
        return View();
    }

    [HttpGet("/admin/subscriptions")]
    public IActionResult Subscriptions()
    {
        return View();
    }

    [HttpGet("/admin/subscriptions/{id}")]
    public IActionResult SubscriptionDetails(Guid id)
    {
        ViewBag.SubscriptionId = id;
        return View();
    }

    [HttpGet("/admin/plans")]
    public IActionResult Plans()
    {
        return View();
    }

    [HttpGet("/admin/invoices")]
    public IActionResult Invoices()
    {
        return View();
    }

    [HttpGet("/admin/reports")]
    public IActionResult Reports()
    {
        return View();
    }

    [HttpGet("/admin/settings")]
    public IActionResult Settings()
    {
        return View();
    }

    [HttpGet("/admin/support")]
    public IActionResult Support()
    {
        return View();
    }
}
