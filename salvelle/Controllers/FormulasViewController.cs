using Microsoft.AspNetCore.Mvc;
using Data;

namespace Controllers;

[Route("Formulas")]
public class FormulasViewController : Controller
{
    private readonly AppDbContext _context;

    public FormulasViewController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View("~/Views/Formulas/Index.cshtml");
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View("~/Views/Formulas/Create.cshtml");
    }

    [HttpGet("Edit/{id}")]
    public IActionResult Edit(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewBag.FormulaId = id;
        return View("~/Views/Formulas/Edit.cshtml");
    }

    [HttpGet("Details/{id}")]
    public IActionResult Details(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        ViewBag.FormulaId = id;
        return View("~/Views/Formulas/Details.cshtml");
    }

    private bool IsAuthenticated()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return false;

        var session = _context.EmployeeSessions
            .FirstOrDefault(s => s.Token == sessionToken &&
                                s.ExpiresAt > DateTime.UtcNow &&
                                s.IsActive);

        return session != null;
    }
}
