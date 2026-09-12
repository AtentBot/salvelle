using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Service;
using Stripe;

namespace Controllers;

/// <summary>
/// Controller MVC para o Portal do Cliente gerenciar sua assinatura
/// </summary>
public class SubscriptionPortalController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubscriptionPortalController> _logger;
    private readonly IEncryptionService _encryption;

    public SubscriptionPortalController(
        AppDbContext context,
        ILogger<SubscriptionPortalController> logger,
        IEncryptionService encryption)
    {
        _context = context;
        _logger = logger;
        _encryption = encryption;
    }

    /// <summary>
    /// Página principal do portal de assinatura
    /// </summary>
    [HttpGet("/minha-assinatura")]
    public async Task<IActionResult> Index()
    {
        var establishment = HttpContext.Items["Establishment"] as Establishment;
        if (establishment == null)
            return RedirectToAction("Login", "Account");

        var subscription = await _context.Subscriptions
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.EstablishmentId == establishment.Id);

        var invoices = subscription != null 
            ? await _context.SubscriptionInvoices
                .Where(i => i.SubscriptionId == subscription.Id)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .ToListAsync()
            : new List<Models.Billing.SubscriptionInvoice>();

        var plans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        ViewBag.Establishment = establishment;
        ViewBag.Subscription = subscription;
        ViewBag.Invoices = invoices;
        ViewBag.Plans = plans;

        return View();
    }

    /// <summary>
    /// Redireciona para o Stripe Customer Portal
    /// </summary>
    [ValidateAntiForgeryToken]
    [HttpPost("/minha-assinatura/portal")]
    public async Task<IActionResult> RedirectToStripePortal()
    {
        var establishment = HttpContext.Items["Establishment"] as Establishment;
        if (establishment == null)
            return RedirectToAction("Login", "Account");

        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishment.Id);

            if (subscription?.StripeCustomerId == null)
            {
                TempData["Error"] = "Assinatura não encontrada";
                return RedirectToAction("Index");
            }

            // Buscar API Key do banco
            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);

            if (stripeConfig == null)
            {
                TempData["Error"] = "Gateway de pagamento não configurado";
                return RedirectToAction("Index");
            }

            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            StripeConfiguration.ApiKey = secretKey;

            // Criar sessão do Billing Portal
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = subscription.StripeCustomerId,
                ReturnUrl = $"{Request.Scheme}://{Request.Host}/minha-assinatura"
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar portal session");
            TempData["Error"] = "Erro ao acessar portal de pagamento";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Solicita cancelamento da assinatura
    /// </summary>
    [ValidateAntiForgeryToken]
    [HttpPost("/minha-assinatura/cancelar")]
    public async Task<IActionResult> RequestCancellation([FromForm] List<string>? reasons, [FromForm] string? comment)
    {
        var establishment = HttpContext.Items["Establishment"] as Establishment;
        if (establishment == null)
            return RedirectToAction("Login", "Account");

        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishment.Id);

            if (subscription == null)
            {
                TempData["Error"] = "Assinatura não encontrada";
                return RedirectToAction("Index");
            }

            // Buscar API Key do banco
            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);

            if (stripeConfig == null || string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            {
                TempData["Error"] = "Não foi possível processar o cancelamento";
                return RedirectToAction("Index");
            }

            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            StripeConfiguration.ApiKey = secretKey;

            // Trial abandonado tem tratamento diferente de período pago.
            var isTrial = string.Equals(subscription.Status, "TRIALING", StringComparison.OrdinalIgnoreCase);

            var service = new Stripe.SubscriptionService();
            if (isTrial)
            {
                // Em teste: cancela AGORA no Stripe (nunca houve cobrança) e revoga o acesso.
                await service.CancelAsync(subscription.StripeSubscriptionId);
            }
            else
            {
                // Período pago: sem novas cobranças, mas mantém acesso até o fim do período
                // (cobrança por período fechado, sem reembolso).
                await service.UpdateAsync(subscription.StripeSubscriptionId, new Stripe.SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                });
            }

            subscription.CancelAtPeriodEnd = true;
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (isTrial)
            {
                // Revoga o acesso imediatamente (não há período pago a honrar).
                subscription.Status = "CANCELED";
                establishment.SubscriptionStatus = "CANCELED";
                establishment.IsActive = false;
                establishment.UpdatedAt = DateTime.UtcNow;
            }

            // Histórico do cancelamento (questionário opcional: razões + comentário livre).
            var reasonCodes = (reasons ?? new List<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .ToList();
            _context.SubscriptionCancellations.Add(new SubscriptionCancellation
            {
                EstablishmentId = establishment.Id,
                SubscriptionId = subscription.Id,
                Reasons = reasonCodes.Count > 0 ? string.Join(",", reasonCodes) : null,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CanceledByEmployeeId = HttpContext.Items["EmployeeId"] as Guid?,
                PeriodEnd = subscription.CurrentPeriodEnd,
                CreatedAt = DateTime.UtcNow
            });

            if (isTrial)
            {
                // Desloga todos os usuários do estabelecimento (acesso encerra em minutos).
                var empIds = await _context.Employees
                    .Where(e => e.EstablishmentId == establishment.Id)
                    .Select(e => e.Id)
                    .ToListAsync();
                var activeSessions = await _context.EmployeeSessions
                    .Where(s => empIds.Contains(s.EmployeeId) && s.IsActive)
                    .ToListAsync();
                foreach (var s in activeSessions)
                {
                    s.IsActive = false;
                    s.RevokedAt = DateTime.UtcNow;
                    s.RevocationReason = "Assinatura cancelada durante o período de teste";
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Cancelamento solicitado: {EstablishmentId} - Trial: {IsTrial} - Razões: {Reasons} - Comentário: {HasComment}",
                establishment.Id, isTrial,
                reasonCodes.Count > 0 ? string.Join(",", reasonCodes) : "não informado",
                !string.IsNullOrWhiteSpace(comment));

            if (isTrial)
            {
                TempData["Success"] = "Assinatura cancelada. Como você estava em período de teste, não há cobrança e o acesso foi encerrado.";
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = "Cancelamento agendado. Você terá acesso até o fim do período atual (sem novas cobranças).";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar assinatura");
            TempData["Error"] = "Erro ao processar cancelamento";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Reverte o cancelamento agendado
    /// </summary>
    [ValidateAntiForgeryToken]
    [HttpPost("/minha-assinatura/reativar")]
    public async Task<IActionResult> ReactivateSubscription()
    {
        var establishment = HttpContext.Items["Establishment"] as Establishment;
        if (establishment == null)
            return RedirectToAction("Login", "Account");

        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishment.Id);

            if (subscription?.StripeSubscriptionId == null)
            {
                TempData["Error"] = "Assinatura não encontrada";
                return RedirectToAction("Index");
            }

            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);

            if (stripeConfig == null)
            {
                TempData["Error"] = "Gateway não configurado";
                return RedirectToAction("Index");
            }

            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            StripeConfiguration.ApiKey = secretKey;

            var service = new Stripe.SubscriptionService();
            await service.UpdateAsync(subscription.StripeSubscriptionId, new Stripe.SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            });

            subscription.CancelAtPeriodEnd = false;
            subscription.CanceledAt = null;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelamento revertido: {EstablishmentId}", establishment.Id);

            TempData["Success"] = "Assinatura reativada com sucesso!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reativar assinatura");
            TempData["Error"] = "Erro ao reativar assinatura";
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Troca para outro plano
    /// </summary>
    [ValidateAntiForgeryToken]
    [HttpPost("/minha-assinatura/trocar-plano")]
    public async Task<IActionResult> ChangePlan([FromForm] Guid planId)
    {
        var establishment = HttpContext.Items["Establishment"] as Establishment;
        if (establishment == null)
            return RedirectToAction("Login", "Account");

        try
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishment.Id);

            if (subscription?.StripeSubscriptionId == null)
            {
                TempData["Error"] = "Assinatura não encontrada";
                return RedirectToAction("Index");
            }

            var newPlan = await _context.Set<SubscriptionPlan>().FindAsync(planId);
            if (newPlan == null)
            {
                TempData["Error"] = "Plano não encontrado";
                return RedirectToAction("Index");
            }

            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);

            if (stripeConfig == null)
            {
                TempData["Error"] = "Gateway não configurado";
                return RedirectToAction("Index");
            }

            // Resolver o preço conforme o ambiente do gateway (Sandbox=teste, Production=live)
            var priceId = newPlan.GetStripePriceId(stripeConfig.Environment, subscription.BillingCycle);
            if (string.IsNullOrEmpty(priceId))
            {
                TempData["Error"] = "Plano não configurado para pagamentos neste ambiente";
                return RedirectToAction("Index");
            }

            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            StripeConfiguration.ApiKey = secretKey;

            // Buscar subscription atual no Stripe
            var subService = new Stripe.SubscriptionService();
            var stripeSub = await subService.GetAsync(subscription.StripeSubscriptionId);

            await subService.UpdateAsync(subscription.StripeSubscriptionId, new Stripe.SubscriptionUpdateOptions
            {
                Items = new List<Stripe.SubscriptionItemOptions>
                {
                    new Stripe.SubscriptionItemOptions
                    {
                        Id = stripeSub.Items.Data[0].Id,
                        Price = priceId
                    }
                },
                ProrationBehavior = "create_prorations"
            });

            // Atualizar no banco
            subscription.SubscriptionPlanId = planId;
            subscription.UpdatedAt = DateTime.UtcNow;

            establishment.MaxEmployeesLimit = newPlan.MaxEmployees;
            establishment.MaxOrdersLimit = newPlan.MaxMonthlyOrders;
            establishment.FeaturesEnabled = newPlan.Features;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano alterado: {EstablishmentId} -> {PlanName}", establishment.Id, newPlan.Name);

            TempData["Success"] = $"Plano alterado para {newPlan.Name}!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao trocar plano");
            TempData["Error"] = "Erro ao trocar plano";
            return RedirectToAction("Index");
        }
    }
}
