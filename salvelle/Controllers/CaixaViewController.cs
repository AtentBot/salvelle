using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;

namespace Controllers;

[Route("Caixa")]
public class CaixaViewController : Controller
{
    private readonly AppDbContext _context;
    private readonly CashRegisterService _cashService;

    public CaixaViewController(AppDbContext context)
    {
        _context = context;
        _cashService = new CashRegisterService(context);
    }

    private Guid GetEstablishmentId()
    {
        var claim = User.FindFirst("EstablishmentId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    /// <summary>
    /// Lista de caixas / Dashboard
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var establishmentId = GetEstablishmentId();
        
        // Verificar caixa aberto
        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        ViewBag.OpenCashRegister = openCashRegister;
        
        // Histórico recente
        var history = await _cashService.GetCashRegisterHistoryAsync(establishmentId, limit: 10);
        
        return View("~/Views/Caixa/Index.cshtml", history);
    }

    /// <summary>
    /// Tela para abrir novo caixa
    /// </summary>
    [HttpGet("Abrir")]
    public async Task<IActionResult> Abrir()
    {
        var establishmentId = GetEstablishmentId();
        
        // Verificar se já tem caixa aberto
        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister != null)
        {
            TempData["Warning"] = $"Já existe um caixa aberto: {openCashRegister.Code}. Feche-o antes de abrir outro.";
            return RedirectToAction("Index");
        }
        
        return View("~/Views/Caixa/Abrir.cshtml");
    }

    /// <summary>
    /// Processar abertura do caixa
    /// </summary>
    [HttpPost("Abrir")]
    public async Task<IActionResult> AbrirPost(decimal saldoInicial, string? observacoes)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var result = await _cashService.OpenCashRegisterAsync(
            establishmentId,
            employeeId,
            saldoInicial,
            observacoes);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction("Index");
        }
        else
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Abrir");
        }
    }

    /// <summary>
    /// Tela para fechar caixa
    /// </summary>
    [HttpGet("Fechar")]
    public async Task<IActionResult> Fechar()
    {
        var establishmentId = GetEstablishmentId();
        
        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister == null)
        {
            TempData["Warning"] = "Nenhum caixa aberto para fechar.";
            return RedirectToAction("Index");
        }
        
        // Buscar movimentos do caixa
        var movements = await _cashService.GetCashMovementsAsync(openCashRegister.Id);
        ViewBag.Movements = movements;
        
        return View("~/Views/Caixa/Fechar.cshtml", openCashRegister);
    }

    /// <summary>
    /// Processar fechamento do caixa
    /// </summary>
    [HttpPost("Fechar")]
    public async Task<IActionResult> FecharPost(Guid cashRegisterId, decimal saldoFinal, string? observacoes)
    {
        var employeeId = GetEmployeeId();

        var result = await _cashService.CloseCashRegisterAsync(
            cashRegisterId,
            employeeId,
            saldoFinal,
            observacoes);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Detalhes de um caixa
    /// </summary>
    [HttpGet("Detalhes/{id}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cashRegister == null)
            return NotFound();

        var movements = await _cashService.GetCashMovementsAsync(id);
        ViewBag.Movements = movements;

        return View("~/Views/Caixa/Detalhes.cshtml", cashRegister);
    }

    /// <summary>
    /// Tela de sangria
    /// </summary>
    [HttpGet("Sangria")]
    public async Task<IActionResult> Sangria()
    {
        var establishmentId = GetEstablishmentId();
        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        
        if (openCashRegister == null)
        {
            TempData["Warning"] = "Nenhum caixa aberto.";
            return RedirectToAction("Index");
        }

        return View("~/Views/Caixa/Sangria.cshtml", openCashRegister);
    }

    /// <summary>
    /// Processar sangria
    /// </summary>
    [HttpPost("Sangria")]
    public async Task<IActionResult> SangriaPost(Guid cashRegisterId, decimal valor, string motivo)
    {
        var employeeId = GetEmployeeId();

        var result = await _cashService.WithdrawCashAsync(
            cashRegisterId,
            valor,
            motivo,
            employeeId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Tela de suprimento
    /// </summary>
    [HttpGet("Suprimento")]
    public async Task<IActionResult> Suprimento()
    {
        var establishmentId = GetEstablishmentId();
        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        
        if (openCashRegister == null)
        {
            TempData["Warning"] = "Nenhum caixa aberto.";
            return RedirectToAction("Index");
        }

        return View("~/Views/Caixa/Suprimento.cshtml", openCashRegister);
    }

    /// <summary>
    /// Processar suprimento
    /// </summary>
    [HttpPost("Suprimento")]
    public async Task<IActionResult> SuprimentoPost(Guid cashRegisterId, decimal valor, string motivo)
    {
        var employeeId = GetEmployeeId();

        var result = await _cashService.SupplyCashAsync(
            cashRegisterId,
            valor,
            motivo,
            employeeId);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction("Index");
    }
}
