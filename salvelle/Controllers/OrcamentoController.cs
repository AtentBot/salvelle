using Microsoft.AspNetCore.Mvc;
using Service.Prescriptions;

namespace Controllers;

/// <summary>
/// Controller MVC para página pública de orçamento
/// Permite que o cliente visualize e aprove/recuse o orçamento
/// </summary>
[Route("orcamento")]
public class OrcamentoController : Controller
{
    private readonly PrescriptionQuoteService _quoteService;

    public OrcamentoController(PrescriptionQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    /// <summary>
    /// Página pública de visualização do orçamento
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> Index(string token)
    {
        var quote = await _quoteService.GetByPublicTokenAsync(token);
        
        if (quote == null)
            return View("NotFound");

        if (quote.IsExpired && quote.Status == "PENDENTE")
        {
            ViewBag.Message = "Este orçamento expirou.";
            return View("Expired", quote);
        }

        return View(quote);
    }

    /// <summary>
    /// Página de sucesso após aprovação
    /// </summary>
    [HttpGet("{token}/sucesso")]
    public async Task<IActionResult> Success(string token)
    {
        var quote = await _quoteService.GetByPublicTokenAsync(token);
        
        if (quote == null)
            return View("NotFound");

        return View(quote);
    }

    /// <summary>
    /// Página de orçamento não encontrado
    /// </summary>
    [HttpGet("nao-encontrado")]
    public IActionResult NotFound()
    {
        return View();
    }
}
