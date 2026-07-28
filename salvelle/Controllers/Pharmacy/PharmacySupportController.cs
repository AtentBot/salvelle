using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Employees;
using Service.Support;

namespace Controllers.Pharmacy;

[ApiController]
[Route("api/pharmacy/support")]
public class PharmacySupportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SupportTicketService _tickets;

    public PharmacySupportController(AppDbContext db, SupportTicketService tickets)
    {
        _db = db;
        _tickets = tickets;
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> ListMyTickets([FromQuery] string? status, [FromQuery] int page = 1)
    {
        var estId = GetEstablishmentId();
        if (estId == null) return Unauthorized();

        var query = _db.SupportTickets
            .Where(t => t.EstablishmentId == estId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status.ToUpper());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * 20)
            .Take(20)
            .Select(t => new
            {
                t.Id,
                t.Category,
                categoryDisplay = t.CategoryDisplay,
                t.Title,
                t.Priority,
                priorityDisplay = t.PriorityDisplay,
                t.Status,
                statusDisplay = t.StatusDisplay,
                t.CreatedAt,
                t.UpdatedAt,
                hasNewReply = t.Messages.Any(m => m.AuthorType == "ADMIN" && m.CreatedAt > t.UpdatedAt)
            })
            .ToListAsync();

        return Ok(new { total, items });
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var estId = GetEstablishmentId();
        if (estId == null) return Unauthorized();

        var ticket = await _db.SupportTickets
            .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == id && t.EstablishmentId == estId.Value);

        if (ticket == null)
            return NotFound(new { message = "Chamado não encontrado" });

        return Ok(new
        {
            ticket.Id,
            ticket.Category,
            categoryDisplay = ticket.CategoryDisplay,
            ticket.Title,
            ticket.Description,
            ticket.Priority,
            priorityDisplay = ticket.PriorityDisplay,
            ticket.Status,
            statusDisplay = ticket.StatusDisplay,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            messages = ticket.Messages.Select(m => new
            {
                m.Id,
                m.AuthorType,
                m.AuthorName,
                m.Body,
                m.CreatedAt
            })
        });
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> OpenTicket([FromBody] OpenTicketRequest req)
    {
        var estId = GetEstablishmentId();
        if (estId == null) return Unauthorized();

        var employee = HttpContext.Items["Employee"] as Employee;

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Título é obrigatório" });
        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest(new { message = "Descrição é obrigatória" });

        var validCategories = new[] { "WHATSAPP", "PAYMENT", "SIGNUP", "TECHNICAL", "GENERAL" };
        var category = string.IsNullOrEmpty(req.Category) ? "GENERAL" : req.Category.ToUpper();
        if (!validCategories.Contains(category)) category = "GENERAL";

        var validPriorities = new[] { "LOW", "MEDIUM", "HIGH" };
        var priority = string.IsNullOrEmpty(req.Priority) ? "MEDIUM" : req.Priority.ToUpper();
        if (!validPriorities.Contains(priority)) priority = "MEDIUM";

        var authorName = employee?.FullName ?? "Farmácia";

        var ticket = await _tickets.OpenManualTicketAsync(
            estId.Value,
            req.Title.Trim(),
            req.Description.Trim(),
            category,
            priority,
            authorName
        );

        return Ok(new { ticket.Id, message = "Chamado aberto com sucesso" });
    }

    [HttpPost("tickets/{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest req)
    {
        var estId = GetEstablishmentId();
        if (estId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { message = "Mensagem não pode ser vazia" });

        // Verifica que o chamado pertence a esta farmácia
        var owns = await _db.SupportTickets
            .AnyAsync(t => t.Id == id && t.EstablishmentId == estId.Value);
        if (!owns) return NotFound(new { message = "Chamado não encontrado" });

        var employee = HttpContext.Items["Employee"] as Employee;
        var authorName = employee?.FullName ?? "Farmácia";

        await _tickets.AddMessageAsync(id, "PHARMACY", employee?.Id, authorName, req.Body.Trim());
        return Ok(new { message = "Resposta enviada" });
    }

    private Guid? GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var id) && id is Guid estId)
            return estId;
        return null;
    }

    public record OpenTicketRequest(string Title, string Description, string? Category, string? Priority);
    public record ReplyRequest(string Body);
}
