using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/invoices")]
public class AdminInvoicesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminInvoicesController> _logger;
    private readonly AuditService _audit;

    private static string SanitizeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var trimmed = value.Replace(";", ",").Replace("\n", " ").Replace("\r", " ");
        if (trimmed.StartsWith("=") || trimmed.StartsWith("+") || trimmed.StartsWith("-") || trimmed.StartsWith("@") || trimmed.StartsWith("\t"))
            trimmed = "'" + trimmed;
        return trimmed;
    }

    public AdminInvoicesController(AppDbContext context, ILogger<AdminInvoicesController> logger, AuditService audit)
    {
        _context = context;
        _logger = logger;
        _audit = audit;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var paidCount = await _context.SubscriptionInvoices
                .CountAsync(i => i.Status == "PAID");

            var pendingCount = await _context.SubscriptionInvoices
                .CountAsync(i => i.Status == "PENDING" && (i.DueDate == null || i.DueDate >= now));

            var overdueCount = await _context.SubscriptionInvoices
                .CountAsync(i => i.Status == "PENDING" && i.DueDate != null && i.DueDate < now);

            var totalRevenueThisMonth = await _context.SubscriptionInvoices
                .Where(i => i.Status == "PAID" && i.PaidAt != null && i.PaidAt >= startOfMonth)
                .SumAsync(i => i.Amount);

            return Ok(new
            {
                paidCount,
                pendingCount,
                overdueCount,
                totalRevenueThisMonth
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar stats de faturas");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            var query = _context.SubscriptionInvoices
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.Establishment)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.SubscriptionPlan)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "OVERDUE")
                {
                    // Vencidas = PENDING com DueDate < hoje
                    query = query.Where(i => i.Status == "PENDING" && i.DueDate != null && i.DueDate < DateTime.UtcNow);
                }
                else
                {
                    query = query.Where(i => i.Status == status.ToUpper());
                }
            }

            // Filter by month/year
            if (month.HasValue && year.HasValue)
            {
                var startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1);
                query = query.Where(i => i.CreatedAt >= startDate && i.CreatedAt < endDate);
            }
            else if (year.HasValue)
            {
                var startDate = new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddYears(1);
                query = query.Where(i => i.CreatedAt >= startDate && i.CreatedAt < endDate);
            }

            // Search by establishment name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(i =>
                    i.Subscription.Establishment.NomeFantasia.ToLower().Contains(searchLower) ||
                    (i.Subscription.Establishment.Cnpj != null && i.Subscription.Establishment.Cnpj.Contains(search)));
            }

            var total = await query.CountAsync();

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(Math.Min(take, 100))
                .Select(i => new
                {
                    i.Id,
                    i.Amount,
                    i.Status,
                    i.DueDate,
                    i.PaidAt,
                    i.CreatedAt,
                    i.StripeInvoiceId,
                    i.InvoiceUrl,
                    EstablishmentId = i.Subscription.EstablishmentId,
                    EstablishmentName = i.Subscription.Establishment.NomeFantasia,
                    PlanName = i.Subscription.SubscriptionPlan != null ? i.Subscription.SubscriptionPlan.Name : null
                })
                .ToListAsync();

            return Ok(new { total, invoices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar faturas");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            // Usar projection para evitar problemas com colunas faltando
            var invoice = await _context.SubscriptionInvoices
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    i.Id,
                    i.Amount,
                    i.Status,
                    i.DueDate,
                    i.PaidAt,
                    i.CreatedAt,
                    i.StripeInvoiceId,
                    i.InvoiceUrl,
                    i.SubscriptionId,
                    EstablishmentId = i.Subscription.EstablishmentId,
                    EstablishmentName = i.Subscription.Establishment.NomeFantasia,
                    PlanName = i.Subscription.SubscriptionPlan != null ? i.Subscription.SubscriptionPlan.Name : null
                })
                .FirstOrDefaultAsync();

            if (invoice == null)
                return NotFound(new { message = "Fatura não encontrada" });

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fatura {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var invoice = await _context.SubscriptionInvoices.FindAsync(id);
            if (invoice == null)
                return NotFound(new { message = "Fatura não encontrada" });

            if (invoice.Status == "PAID")
                return BadRequest(new { message = "Fatura já está paga" });

            invoice.Status = "PAID";
            invoice.PaidAt = DateTime.UtcNow;

            // Atualizar status da subscription se necessário
            var subscription = await _context.Subscriptions.FindAsync(invoice.SubscriptionId);
            if (subscription != null && subscription.Status == "PAST_DUE")
            {
                subscription.Status = "ACTIVE";
                subscription.UpdatedAt = DateTime.UtcNow;

                // Atualizar estabelecimento
                var establishment = await _context.Establishments.FindAsync(subscription.EstablishmentId);
                if (establishment != null)
                {
                    establishment.SubscriptionStatus = "ACTIVE";
                    establishment.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            await _audit.LogAsync(HttpContext, "INVOICE_MARKED_PAID", "Invoice", id.ToString());

            _logger.LogInformation("Fatura {Id} marcada como paga manualmente", id);

            return Ok(new { message = "Fatura marcada como paga" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar fatura {Id} como paga", id);
            return StatusCode(500, new { message = "Erro ao atualizar fatura" });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? status = null,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        try
        {
            var query = _context.SubscriptionInvoices
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.Establishment)
                .Include(i => i.Subscription)
                    .ThenInclude(s => s.SubscriptionPlan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status.ToUpper());
            }

            if (month.HasValue && year.HasValue)
            {
                var startDate = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                var endDate = startDate.AddMonths(1);
                query = query.Where(i => i.CreatedAt >= startDate && i.CreatedAt < endDate);
            }

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    Estabelecimento = i.Subscription.Establishment.NomeFantasia,
                    CNPJ = i.Subscription.Establishment.Cnpj,
                    Plano = i.Subscription.SubscriptionPlan != null ? i.Subscription.SubscriptionPlan.Name : "",
                    Valor = i.Amount,
                    Status = i.Status,
                    Vencimento = i.DueDate,
                    PagoEm = i.PaidAt,
                    CriadoEm = i.CreatedAt
                })
                .ToListAsync();

            // Gerar CSV
            var csv = "ID;Estabelecimento;CNPJ;Plano;Valor;Status;Vencimento;Pago Em;Criado Em\n";
            foreach (var inv in invoices)
            {
                csv += $"{inv.Id};{SanitizeCsv(inv.Estabelecimento)};{SanitizeCsv(inv.CNPJ)};{SanitizeCsv(inv.Plano)};{inv.Valor:F2};{SanitizeCsv(inv.Status)};{inv.Vencimento:dd/MM/yyyy};{inv.PagoEm:dd/MM/yyyy HH:mm};{inv.CriadoEm:dd/MM/yyyy HH:mm}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileName = $"faturas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar faturas");
            return StatusCode(500, new { message = "Erro ao exportar" });
        }
    }

    private bool IsSuperAdmin()
    {
        var role = HttpContext.Items["SaasAdminRole"] as string;
        return string.Equals(role, "SUPER_ADMIN", StringComparison.Ordinal);
    }
}