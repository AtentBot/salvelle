using Microsoft.AspNetCore.Mvc;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

/// <summary>
/// MVC Controller para Views de Estoque
/// Serve páginas HTML para gerenciamento visual do estoque
/// </summary>
[Route("[controller]")]
public class EstoqueController : Controller
{
    private readonly AppDbContext _context;

    public EstoqueController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Dashboard principal do estoque
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Listagem de movimentações de estoque
    /// </summary>
    [HttpGet("Movimentacoes")]
    public IActionResult Movimentacoes()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Listagem de lotes
    /// </summary>
    [HttpGet("Lotes")]
    public IActionResult Lotes()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Rastreabilidade de lote específico
    /// </summary>
    [HttpGet("Rastreabilidade/{id}")]
    public async Task<IActionResult> Rastreabilidade(Guid id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var batch = await _context.Batches
            .Include(b => b.RawMaterial)
            .FirstOrDefaultAsync(b => b.Id == id &&
                                      b.RawMaterial != null &&
                                      b.RawMaterial.EstablishmentId == GetEstablishmentId());

        if (batch == null)
            return NotFound();

        return View(batch);
    }

    /// <summary>
    /// Controle de qualidade - Lotes em quarentena
    /// </summary>
    [HttpGet("Qualidade")]
    public IActionResult Qualidade()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Posição de estoque consolidada
    /// </summary>
    [HttpGet("Posicao")]
    public IActionResult Posicao()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Account");

        return View();
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Items["Employee"] != null;
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        return employee?.EstablishmentId ?? Guid.Empty;
    }
}
