using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/subscriptions")]
public class AdminSubscriptionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminSubscriptionsController> _logger;

    public AdminSubscriptionsController(AppDbContext context, ILogger<AdminSubscriptionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        try
        {
            var query = _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.Establishment)
                .AsQueryable();

            // Filtrar por status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status.ToUpper());
            }

            // Buscar por nome do estabelecimento
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s =>
                    s.Establishment != null &&
                    s.Establishment.NomeFantasia.ToLower().Contains(searchLower));
            }

            var total = await query.CountAsync();

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip(skip)
                .Take(Math.Min(take, 100))
                .Select(s => new
                {
                    s.Id,
                    EstablishmentId = s.EstablishmentId,
                    EstablishmentName = s.Establishment != null ? s.Establishment.NomeFantasia : "-",
                    PlanName = s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "-",
                    s.Status,
                    s.BillingCycle,
                    Amount = s.BillingCycle == "YEARLY"
                        ? (s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceYearly : 0)
                        : (s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceMonthly : 0),
                    s.CurrentPeriodStart,
                    s.CurrentPeriodEnd,
                    s.TrialEnd,
                    s.CancelAtPeriodEnd,
                    s.CanceledAt,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, subscriptions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar assinaturas");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .Include(s => s.Establishment)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            // Buscar faturas
            var invoices = await _context.SubscriptionInvoices
                .Where(i => i.SubscriptionId == id)
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .Select(i => new
                {
                    i.Id,
                    i.Amount,
                    i.Status,
                    i.DueDate,
                    i.PaidAt,
                    i.InvoiceUrl,
                    i.CreatedAt
                })
                .ToListAsync();

            var result = new
            {
                subscription.Id,
                EstablishmentId = subscription.EstablishmentId,
                EstablishmentName = subscription.Establishment?.NomeFantasia ?? "-",
                PlanId = subscription.SubscriptionPlanId,
                PlanName = subscription.SubscriptionPlan?.Name ?? "-",
                subscription.Status,
                subscription.BillingCycle,
                Amount = subscription.BillingCycle == "YEARLY"
                    ? (subscription.SubscriptionPlan?.PriceYearly ?? 0)
                    : (subscription.SubscriptionPlan?.PriceMonthly ?? 0),
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd,
                subscription.TrialStart,
                subscription.TrialEnd,
                subscription.CancelAtPeriodEnd,
                subscription.CanceledAt,
                subscription.StripeSubscriptionId,
                subscription.StripeCustomerId,
                subscription.CreatedAt,
                Invoices = invoices
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar assinatura {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelSubscriptionRequest? request)
    {
        try
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            if (subscription.Status == "CANCELED")
                return BadRequest(new { message = "Assinatura já está cancelada" });

            subscription.Status = "CANCELED";
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Atualizar status do estabelecimento
            var establishment = await _context.Establishments.FindAsync(subscription.EstablishmentId);
            if (establishment != null)
            {
                establishment.SubscriptionStatus = "CANCELED";
                establishment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Assinatura {Id} cancelada. Motivo: {Reason}", id, request?.Reason ?? "Não informado");

            return Ok(new { message = "Assinatura cancelada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar assinatura {Id}", id);
            return StatusCode(500, new { message = "Erro ao cancelar assinatura" });
        }
    }

    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            if (subscription.Status == "ACTIVE")
                return BadRequest(new { message = "Assinatura já está ativa" });

            subscription.Status = "ACTIVE";
            subscription.CanceledAt = null;
            subscription.CancelAtPeriodEnd = false;
            subscription.CurrentPeriodStart = DateTime.UtcNow;
            subscription.CurrentPeriodEnd = subscription.BillingCycle == "YEARLY"
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1);
            subscription.UpdatedAt = DateTime.UtcNow;

            // Atualizar status do estabelecimento
            var establishment = await _context.Establishments.FindAsync(subscription.EstablishmentId);
            if (establishment != null)
            {
                establishment.SubscriptionStatus = "ACTIVE";
                establishment.IsActive = true;
                establishment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assinatura {Id} reativada", id);

            return Ok(new { message = "Assinatura reativada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reativar assinatura {Id}", id);
            return StatusCode(500, new { message = "Erro ao reativar assinatura" });
        }
    }

    [HttpPost("{id}/change-plan")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangePlanRequest request)
    {
        try
        {
            var subscription = await _context.Subscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound(new { message = "Assinatura não encontrada" });

            var newPlan = await _context.SubscriptionPlans.FindAsync(request.NewPlanId);
            if (newPlan == null)
                return BadRequest(new { message = "Plano não encontrado" });

            subscription.SubscriptionPlanId = newPlan.Id;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assinatura {Id} alterada para plano {PlanId}", id, newPlan.Id);

            return Ok(new { message = "Plano alterado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar plano da assinatura {Id}", id);
            return StatusCode(500, new { message = "Erro ao alterar plano" });
        }
    }
}

public class CancelSubscriptionRequest
{
    public string? Reason { get; set; }
}

public class ChangePlanRequest
{
    public Guid NewPlanId { get; set; }
}