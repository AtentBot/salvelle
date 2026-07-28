using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Support;
using Service.Support;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/support")]
public class AdminSupportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SupportTicketService _tickets;
    private readonly ILogger<AdminSupportController> _logger;

    public AdminSupportController(AppDbContext db, SupportTicketService tickets, ILogger<AdminSupportController> logger)
    {
        _db = db;
        _tickets = tickets;
        _logger = logger;
    }

    // ── WhatsApp status ────────────────────────────────────────────────────────

    [HttpGet("whatsapp-status")]
    public async Task<IActionResult> GetWhatsAppStatus()
    {
        var statuses = await _db.WhatsAppInstanceStatuses.ToListAsync();
        return Ok(statuses.Select(s => new
        {
            s.InstanceName,
            s.Status,
            statusDisplay = s.StatusDisplay,
            isHealthy = s.IsHealthy,
            s.LastCheckedAt,
            s.DisconnectedSince,
            s.LastConnectedAt,
            s.ActiveTicketId
        }));
    }

    // ── Tickets ────────────────────────────────────────────────────────────────

    [HttpGet("tickets")]
    public async Task<IActionResult> ListTickets(
        [FromQuery] string? status,
        [FromQuery] string? origin,
        [FromQuery] string? category,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        pageSize = Math.Min(pageSize, 100);

        var query = _db.SupportTickets
            .Include(t => t.Establishment)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status.ToUpper());
        if (!string.IsNullOrEmpty(origin))
            query = query.Where(t => t.Origin == origin.ToUpper());
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category.ToUpper());
        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority.ToUpper());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.Priority == "CRITICAL")
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Origin,
                t.Category,
                categoryDisplay = t.CategoryDisplay,
                t.Title,
                t.Priority,
                priorityDisplay = t.PriorityDisplay,
                t.Status,
                statusDisplay = t.StatusDisplay,
                t.AssignedTo,
                t.EstablishmentId,
                pharmacyName = t.Establishment != null ? t.Establishment.NomeFantasia : null,
                t.CreatedAt,
                t.UpdatedAt,
                t.ResolvedAt,
                messageCount = t.Messages.Count
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var ticket = await _db.SupportTickets
            .Include(t => t.Establishment)
            .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound(new { message = "Chamado não encontrado" });

        return Ok(new
        {
            ticket.Id,
            ticket.Origin,
            ticket.Category,
            categoryDisplay = ticket.CategoryDisplay,
            ticket.Title,
            ticket.Description,
            ticket.Priority,
            priorityDisplay = ticket.PriorityDisplay,
            ticket.Status,
            statusDisplay = ticket.StatusDisplay,
            ticket.AssignedTo,
            ticket.IsAutoResolvable,
            ticket.EstablishmentId,
            pharmacyName = ticket.Establishment?.NomeFantasia,
            pharmacyCnpj = ticket.Establishment?.Cnpj,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
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

    [HttpPost("tickets/{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { message = "Mensagem não pode ser vazia" });

        var admin = (HttpContext.Items["SaasAdmin"] as SaasAdmin)?.FullName ?? "Admin";

        try
        {
            await _tickets.AddMessageAsync(id, "ADMIN", null, admin, req.Body.Trim());
            return Ok(new { message = "Resposta enviada" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("tickets/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] StatusRequest req)
    {
        var allowed = new[] { "OPEN", "IN_PROGRESS", "RESOLVED", "CLOSED" };
        if (string.IsNullOrEmpty(req.Status) || !allowed.Contains(req.Status.ToUpper()))
            return BadRequest(new { message = "Status inválido" });

        try
        {
            await _tickets.UpdateStatusAsync(id, req.Status.ToUpper());
            return Ok(new { message = "Status atualizado" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("tickets/stats")]
    public async Task<IActionResult> GetStats()
    {
        var all = await _db.SupportTickets.ToListAsync();
        return Ok(new
        {
            total = all.Count,
            open = all.Count(t => t.Status == "OPEN"),
            inProgress = all.Count(t => t.Status == "IN_PROGRESS"),
            resolved = all.Count(t => t.Status == "RESOLVED"),
            critical = all.Count(t => t.Priority == "CRITICAL" && t.Status is "OPEN" or "IN_PROGRESS"),
            system = all.Count(t => t.Origin == "SYSTEM" && t.Status is "OPEN" or "IN_PROGRESS")
        });
    }

    public record ReplyRequest(string Body);
    public record StatusRequest(string Status);
}
