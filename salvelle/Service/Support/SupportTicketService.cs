using Data;
using Microsoft.EntityFrameworkCore;
using Models.Support;

namespace Service.Support;

public class SupportTicketService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SupportTicketService> _logger;

    public SupportTicketService(AppDbContext db, ILogger<SupportTicketService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Abre (ou atualiza) um chamado sistêmico com deduplicação por chave.
    /// Se já existe um chamado aberto com a mesma chave, retorna o existente.
    /// </summary>
    public async Task<SupportTicket> OpenSystemTicketAsync(
        string deduplicationKey,
        string title,
        string description,
        string category,
        string priority = "HIGH")
    {
        var existing = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.DeduplicationKey == deduplicationKey
                                   && t.Status != "CLOSED"
                                   && t.Status != "RESOLVED");
        if (existing != null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existing;
        }

        var ticket = new SupportTicket
        {
            Origin = "SYSTEM",
            Category = category,
            Title = title,
            Description = description,
            Priority = priority,
            Status = "OPEN",
            DeduplicationKey = deduplicationKey,
            IsAutoResolvable = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.SupportTickets.Add(ticket);
        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = ticket.Id,
            AuthorType = "SYSTEM",
            AuthorName = "Sistema",
            Body = description,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogWarning("Chamado sistêmico aberto: [{Category}] {Title} (id={Id})", category, title, ticket.Id);
        return ticket;
    }

    /// <summary>
    /// Resolve automaticamente o chamado sistêmico associado à chave, adicionando uma nota de resolução.
    /// </summary>
    public async Task AutoResolveByKeyAsync(string deduplicationKey, string resolutionNote)
    {
        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.DeduplicationKey == deduplicationKey
                                   && t.Status != "CLOSED"
                                   && t.Status != "RESOLVED");
        if (ticket == null) return;

        ticket.Status = "RESOLVED";
        ticket.ResolvedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = ticket.Id,
            AuthorType = "SYSTEM",
            AuthorName = "Sistema",
            Body = resolutionNote,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Chamado {Id} resolvido automaticamente: {Key}", ticket.Id, deduplicationKey);
    }

    /// <summary>
    /// Abre um chamado manual pela farmácia.
    /// </summary>
    public async Task<SupportTicket> OpenManualTicketAsync(
        Guid establishmentId,
        string title,
        string description,
        string category,
        string priority,
        string authorName)
    {
        var ticket = new SupportTicket
        {
            Origin = "MANUAL",
            Category = category,
            EstablishmentId = establishmentId,
            Title = title,
            Description = description,
            Priority = priority,
            Status = "OPEN",
            IsAutoResolvable = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.SupportTickets.Add(ticket);
        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = ticket.Id,
            AuthorType = "PHARMACY",
            AuthorName = authorName,
            Body = description,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Chamado manual aberto por {Author} (establishment={EstId})", authorName, establishmentId);
        return ticket;
    }

    /// <summary>
    /// Adiciona mensagem ao chamado. Se o admin responder, move status OPEN → IN_PROGRESS.
    /// </summary>
    public async Task AddMessageAsync(Guid ticketId, string authorType, Guid? authorId, string authorName, string body)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new InvalidOperationException("Chamado não encontrado");

        _db.SupportTicketMessages.Add(new SupportTicketMessage
        {
            TicketId = ticketId,
            AuthorType = authorType,
            AuthorId = authorId,
            AuthorName = authorName,
            Body = body,
            CreatedAt = DateTime.UtcNow
        });

        ticket.UpdatedAt = DateTime.UtcNow;
        if (authorType == "ADMIN" && ticket.Status == "OPEN")
            ticket.Status = "IN_PROGRESS";

        await _db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid ticketId, string newStatus)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new InvalidOperationException("Chamado não encontrado");

        ticket.Status = newStatus;
        ticket.UpdatedAt = DateTime.UtcNow;
        if (newStatus == "RESOLVED") ticket.ResolvedAt = DateTime.UtcNow;
        if (newStatus == "CLOSED") ticket.ClosedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsOpenTicketForKeyAsync(string deduplicationKey)
    {
        return await _db.SupportTickets
            .AnyAsync(t => t.DeduplicationKey == deduplicationKey
                        && t.Status != "CLOSED"
                        && t.Status != "RESOLVED");
    }
}
