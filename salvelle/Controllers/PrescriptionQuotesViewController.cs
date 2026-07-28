using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using Models.Employees;

namespace Controllers.Mvc;

/// <summary>
/// Controller MVC para visualização de orçamentos (retorna Views HTML)
/// Rota: /PrescriptionQuotes
/// </summary>
[Route("PrescriptionQuotes")]
public class PrescriptionQuotesViewController : Controller
{
    private readonly AppDbContext _context;

    public PrescriptionQuotesViewController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista de orçamentos
    /// GET /PrescriptionQuotes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var session = await GetCurrentSessionAsync();
        if (session == null)
            return RedirectToAction("Login", "Account");

        var quotes = await _context.PrescriptionQuotes
            .Where(q => q.EstablishmentId == session.Employee.EstablishmentId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(100)
            .ToListAsync();

        ViewBag.Quotes = quotes;
        return View("~/Views/PrescriptionQuotes/Index.cshtml");
    }

    /// <summary>
    /// Detalhes do orçamento
    /// GET /PrescriptionQuotes/Details/{id}
    /// </summary>
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var session = await GetCurrentSessionAsync();
        if (session == null)
            return RedirectToAction("Login", "Account");

        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == session.Employee.EstablishmentId);

        if (quote == null)
            return NotFound();

        // Buscar ordem de manipulação se existir
        ManipulationOrder? manipulationOrder = null;
        if (quote.ManipulationOrderId.HasValue)
        {
            manipulationOrder = await _context.ManipulationOrders
                .FirstOrDefaultAsync(m => m.Id == quote.ManipulationOrderId.Value);
        }

        ViewBag.Quote = quote;
        ViewBag.ManipulationOrder = manipulationOrder;
        return View("~/Views/PrescriptionQuotes/Details.cshtml");
    }

    /// <summary>
    /// Aprovar orçamento (via form POST)
    /// POST /PrescriptionQuotes/Approve/{id}
    /// </summary>
    [HttpPost("Approve/{id}")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var session = await GetCurrentSessionAsync();
        if (session == null)
            return RedirectToAction("Login", "Account");

        try
        {
            var quote = await _context.PrescriptionQuotes
                .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == session.Employee.EstablishmentId);

            if (quote == null)
            {
                TempData["Error"] = "Orçamento não encontrado";
                return RedirectToAction("Index");
            }

            if (quote.Status != "PENDENTE")
            {
                TempData["Error"] = $"Orçamento não pode ser aprovado. Status atual: {quote.Status}";
                return RedirectToAction("Index");
            }

            // Verificar validade
            if (quote.ValidUntil < DateTime.UtcNow)
            {
                TempData["Error"] = "Orçamento expirado";
                return RedirectToAction("Index");
            }

            // Criar Ordem de Manipulação
            var orderCode = await GenerateManipulationOrderCodeAsync(session.Employee.EstablishmentId);
            
            var order = new ManipulationOrder
            {
                Id = Guid.NewGuid(),
                EstablishmentId = session.Employee.EstablishmentId,
                OrderNumber = orderCode,
                Code = orderCode,
                PrescriptionQuoteId = quote.Id,
                PrescriberName = quote.DoctorName,
                PrescriberRegistration = quote.DoctorCrm,
                CustomerName = quote.CustomerName ?? "Cliente",
                CustomerPhone = quote.CustomerPhone,
                QuantityToProduce = quote.TotalQuantityNumeric,
                Unit = quote.TotalQuantityUnit ?? "un",
                SpecialInstructions = quote.Instructions,
                Status = "AGUARDANDO_PRODUCAO",
                Priority = "NORMAL",
                OrderDate = DateTime.UtcNow,
                ExpectedDate = DateTime.UtcNow.AddDays(quote.EstimatedDays),
                RequestedByEmployeeId = session.EmployeeId,
                PassedQualityControl = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationOrders.Add(order);

            // Atualizar orçamento
            quote.Status = "APROVADO";
            quote.ApprovedAt = DateTime.UtcNow;
            quote.ManipulationOrderId = order.Id;
            quote.UpdatedAt = DateTime.UtcNow;
            quote.UpdatedByEmployeeId = session.EmployeeId;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Orçamento aprovado! Ordem de Manipulação criada: {orderCode}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao aprovar: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Rejeitar orçamento (via form POST)
    /// POST /PrescriptionQuotes/Reject/{id}
    /// </summary>
    [HttpPost("Reject/{id}")]
    public async Task<IActionResult> Reject(Guid id, [FromForm] string reason)
    {
        var session = await GetCurrentSessionAsync();
        if (session == null)
            return RedirectToAction("Login", "Account");

        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == session.Employee.EstablishmentId);

        if (quote == null)
        {
            TempData["Error"] = "Orçamento não encontrado";
            return RedirectToAction("Index");
        }

        quote.Status = "RECUSADO"; // vocabulário canônico (serviço e estatísticas usam "RECUSADO")
        quote.RejectionReason = reason;
        quote.RejectedAt = DateTime.UtcNow;
        quote.UpdatedAt = DateTime.UtcNow;
        quote.UpdatedByEmployeeId = session.EmployeeId;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Orçamento rejeitado com sucesso.";

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Cancelar orçamento
    /// POST /PrescriptionQuotes/Cancel/{id}
    /// </summary>
    [HttpPost("Cancel/{id}")]
    public async Task<IActionResult> Cancel(Guid id, [FromForm] string reason)
    {
        var session = await GetCurrentSessionAsync();
        if (session == null)
            return RedirectToAction("Login", "Account");

        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == session.Employee.EstablishmentId);

        if (quote == null)
        {
            TempData["Error"] = "Orçamento não encontrado";
            return RedirectToAction("Index");
        }

        if (quote.Status == "CONVERTIDO")
        {
            TempData["Error"] = "Não é possível cancelar orçamento já convertido em venda";
            return RedirectToAction("Index");
        }

        quote.Status = "CANCELADO";
        quote.RejectionReason = reason;
        quote.UpdatedAt = DateTime.UtcNow;
        quote.UpdatedByEmployeeId = session.EmployeeId;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Orçamento cancelado com sucesso.";

        return RedirectToAction("Index");
    }

    // ===== MÉTODOS AUXILIARES =====

    private async Task<EmployeeSession?> GetCurrentSessionAsync()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = await _context.EmployeeSessions
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Token == sessionToken &&
                                     s.ExpiresAt > DateTime.UtcNow &&
                                     s.IsActive);

        return session;
    }

    private async Task<string> GenerateManipulationOrderCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var dateStr = today.ToString("yyyyMMdd");

        var count = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.CreatedAt.Date == today.Date)
            .CountAsync();

        return $"OM{dateStr}{(count + 1):D4}";
    }
}
