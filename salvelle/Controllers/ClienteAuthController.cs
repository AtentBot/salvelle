using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;

namespace Controllers;

/// <summary>
/// Controller MVC para páginas de autenticação do Portal do Cliente
/// Rota: /ClienteAuth/*
/// </summary>
[Route("ClienteAuth")]
public class ClienteAuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClienteAuthController> _logger;

    public ClienteAuthController(AppDbContext context, ILogger<ClienteAuthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Página de Login
    /// GET /ClienteAuth/Login
    /// </summary>
    [HttpGet("Login")]
    public IActionResult Login([FromQuery] string? returnUrl = null, [FromQuery] Guid? estabelecimento = null)
    {
        // Se já está logado, redireciona
        var session = HttpContext.Items["CustomerSession"] as Models.CustomerSession;
        if (session != null)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Inicio", "Cliente");
        }

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.EstabelecimentoId = estabelecimento;
        
        // Se veio de QR Code, buscar nome da farmácia
        if (estabelecimento.HasValue)
        {
            var est = _context.Establishments
                .FirstOrDefault(e => e.Id == estabelecimento.Value && e.IsActive);
            ViewBag.EstabelecimentoNome = est?.NomeFantasia;
        }

        return View("Login");
    }

    /// <summary>
    /// Página de Cadastro
    /// GET /ClienteAuth/Cadastro
    /// </summary>
    [HttpGet("Cadastro")]
    public IActionResult Cadastro([FromQuery] string? returnUrl = null, [FromQuery] Guid? estabelecimento = null)
    {
        // Se já está logado, redireciona
        var session = HttpContext.Items["CustomerSession"] as Models.CustomerSession;
        if (session != null)
        {
            return RedirectToAction("Inicio", "Cliente");
        }

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.EstabelecimentoId = estabelecimento;

        return View("Cadastro");
    }

    /// <summary>
    /// Página de Verificação de Código
    /// GET /ClienteAuth/Verificar
    /// </summary>
    [HttpGet("Verificar")]
    public IActionResult Verificar([FromQuery] string? phone = null, [FromQuery] string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return RedirectToAction("Login");
        }

        ViewBag.Phone = phone;
        ViewBag.ReturnUrl = returnUrl;
        return View("Verificar");
    }

    /// <summary>
    /// Página de Recuperação de Senha
    /// GET /ClienteAuth/RecuperarSenha
    /// </summary>
    [HttpGet("RecuperarSenha")]
    public IActionResult RecuperarSenha()
    {
        return View("RecuperarSenha");
    }

    /// <summary>
    /// Página de Redefinição de Senha
    /// GET /ClienteAuth/RedefinirSenha
    /// </summary>
    [HttpGet("RedefinirSenha")]
    public IActionResult RedefinirSenha([FromQuery] string? phone = null)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return RedirectToAction("RecuperarSenha");
        }

        ViewBag.Phone = phone;
        return View("RedefinirSenha");
    }

    /// <summary>
    /// Logout e redirecionamento
    /// GET /ClienteAuth/Logout
    /// </summary>
    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        // Limpar cookie de sessão
        Response.Cookies.Delete("CustomerSessionId");
        Response.Cookies.Delete("salvelle_customer_session");
        
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Redireciona /ClienteAuth para Login
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction("Login");
    }
}
