using Microsoft.AspNetCore.Mvc;
using Data;
using Models.Employees;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, ILogger<AccountController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== LOGIN ====================
    [HttpGet]
    public IActionResult Login()
    {
        // Se já estiver logado, redirecionar para dashboard
        if (HttpContext.Items["Employee"] != null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    // ==================== REGISTRO ====================
    [HttpGet]
    public IActionResult Register()
    {
        // Se já estiver logado, redirecionar para dashboard
        if (HttpContext.Items["Employee"] != null)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    // ==================== VALIDAR CÓDIGO WHATSAPP ====================
    [HttpGet]
    public IActionResult ValidateCode()
    {
        // Verificar se há um employeeId na sessão temporária
        var employeeId = HttpContext.Session.GetString("PendingEmployeeId");
        if (string.IsNullOrEmpty(employeeId))
        {
            return RedirectToAction("Login");
        }

        return View();
    }

    // ==================== LOGOUT ====================
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = Request.Cookies["SessionId"];
            if (!string.IsNullOrEmpty(sessionToken))
            {
                // Invalidar sessão no banco
                var session = await _db.EmployeeSessions
                    .FirstOrDefaultAsync(s => s.Token == sessionToken);

                if (session != null)
                {
                    session.ExpiresAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                // Remover cookie
                Response.Cookies.Delete("SessionId");
            }

            // Limpar sessão temporária
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer logout");
            return RedirectToAction("Login");
        }
    }

    // ==================== ESQUECI MINHA SENHA ====================
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // ==================== REDEFINIR SENHA ====================
    [HttpGet]
    public IActionResult ResetPassword(string? identifier = null)
    {
        // Identifier pode vir da URL ou o usuário digita na tela
        ViewBag.Identifier = identifier;
        return View();
    }

    // ==================== MEU PERFIL ====================
    [HttpGet]
    public IActionResult Profile()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null) return RedirectToAction("Login");
        return View();
    }
}
