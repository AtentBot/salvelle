using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Billing;
using Service;
using Stripe;
using Stripe.Checkout;

namespace Controllers.Api;

[ApiController]
[Route("api/webhooks")]
public class StripeWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<StripeWebhookController> _logger;
    private readonly IEncryptionService _encryption;

    public StripeWebhookController(
        AppDbContext context,
        ILogger<StripeWebhookController> logger,
        IEncryptionService encryption)
    {
        _context = context;
        _logger = logger;
        _encryption = encryption;
    }

    /// <summary>
    /// Endpoint para receber webhooks do Stripe
    /// Configure no Stripe Dashboard: https://seudominio.com/api/webhooks/stripe
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);

            if (stripeConfig == null)
            {
                _logger.LogError("Configuração do Stripe não encontrada");
                return BadRequest("Gateway não configurado");
            }

            var webhookSecret = _encryption.Decrypt(stripeConfig.WebhookSecretEncrypted ?? "");
            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            
            StripeConfiguration.ApiKey = secretKey;
            
            Event stripeEvent;

            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("Webhook secret não configurado - rejeitando webhook por segurança");
                return BadRequest("Webhook secret não configurado");
            }

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning("Assinatura do webhook inválida: {Message}", ex.Message);
                return BadRequest("Assinatura inválida");
            }

            _logger.LogInformation("Webhook recebido: {EventType} - {EventId}", stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutCompleted(stripeEvent);
                    break;
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                case "customer.subscription.trial_will_end":
                    await HandleTrialWillEnd(stripeEvent);
                    break;
                case "invoice.paid":
                    await HandleInvoicePaid(stripeEvent);
                    break;
                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook");
            return StatusCode(500);
        }
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        if (!session.Metadata.TryGetValue("establishment_id", out var estIdStr) ||
            !Guid.TryParse(estIdStr, out var establishmentId))
        {
            _logger.LogWarning("Webhook checkout.session.completed sem metadata establishment_id. SessionId: {SessionId}", session.Id);
            return;
        }

        if (!session.Metadata.TryGetValue("plan_id", out var planIdStr) ||
            !Guid.TryParse(planIdStr, out var planId))
        {
            _logger.LogWarning("Webhook checkout.session.completed sem metadata plan_id. SessionId: {SessionId}", session.Id);
            return;
        }

        Guid? gatewayConfigId = null;
        if (session.Metadata.TryGetValue("gateway_config_id", out var gId) && Guid.TryParse(gId, out var g))
            gatewayConfigId = g;

        // Idempotência: verificar se já processamos este checkout (pelo StripeSubscriptionId)
        if (!string.IsNullOrEmpty(session.SubscriptionId))
        {
            var existingByStripe = await _context.Subscriptions
                .AnyAsync(s => s.StripeSubscriptionId == session.SubscriptionId);
            if (existingByStripe)
            {
                _logger.LogInformation("Checkout já processado (SubscriptionId duplicado): {SubscriptionId}", session.SubscriptionId);
                return;
            }
        }

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);
        var plan = await _context.Set<SubscriptionPlan>().FindAsync(planId);
        var establishment = await _context.Establishments.FindAsync(establishmentId);

        if (plan == null || establishment == null) return;

        var trialEnd = DateTime.UtcNow.AddDays(14);

        if (subscription == null)
        {
            subscription = new Models.Subscription
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                SubscriptionPlanId = planId,
                GatewayConfigId = gatewayConfigId,
                StripeSubscriptionId = session.SubscriptionId,
                StripeCustomerId = session.CustomerId,
                ExternalSubscriptionId = session.SubscriptionId,
                ExternalCustomerId = session.CustomerId,
                Status = "TRIALING",
                BillingCycle = "MONTHLY",
                TrialStart = DateTime.UtcNow,
                TrialEnd = trialEnd,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.SubscriptionPlanId = planId;
            subscription.GatewayConfigId = gatewayConfigId;
            subscription.StripeSubscriptionId = session.SubscriptionId;
            subscription.StripeCustomerId = session.CustomerId;
            subscription.ExternalSubscriptionId = session.SubscriptionId;
            subscription.ExternalCustomerId = session.CustomerId;
            subscription.Status = "TRIALING";
            subscription.TrialStart = DateTime.UtcNow;
            subscription.TrialEnd = trialEnd;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        establishment.SubscriptionStatus = "TRIALING";
        establishment.TrialEndsAt = trialEnd;
        establishment.MaxEmployeesLimit = plan.MaxEmployees;
        establishment.MaxOrdersLimit = plan.MaxMonthlyOrders;
        establishment.FeaturesEnabled = plan.Features;
        establishment.IsActive = true;
        establishment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Checkout completado para establishment: {EstablishmentId}", establishmentId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub == null) return;

        var subscription = await _context.Subscriptions
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (subscription == null) return;

        subscription.Status = MapStatus(stripeSub.Status);
        subscription.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        
        if (stripeSub.CanceledAt.HasValue) 
            subscription.CanceledAt = stripeSub.CanceledAt.Value;
        
        subscription.UpdatedAt = DateTime.UtcNow;

        if (subscription.Establishment != null)
        {
            subscription.Establishment.SubscriptionStatus = subscription.Status;
            if (subscription.Status == "CANCELED") 
                subscription.Establishment.IsActive = false;
            subscription.Establishment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Subscription atualizada: {SubscriptionId} - Status: {Status}", stripeSub.Id, stripeSub.Status);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub == null) return;

        var subscription = await _context.Subscriptions
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (subscription != null)
        {
            subscription.Status = "CANCELED";
            subscription.CanceledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (subscription.Establishment != null)
            {
                subscription.Establishment.SubscriptionStatus = "CANCELED";
                subscription.Establishment.IsActive = false;
                subscription.Establishment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning("Subscription cancelada: {SubscriptionId}", stripeSub.Id);
        }
    }

    private async Task HandleTrialWillEnd(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSub == null) return;

        var subscription = await _context.Subscriptions
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);

        if (subscription?.Establishment != null)
        {
            _logger.LogInformation("Trial ending soon: {EstablishmentId} - {Email}",
                subscription.EstablishmentId, subscription.Establishment.Email?.Length > 5 ? subscription.Establishment.Email[..2] + "***" + subscription.Establishment.Email[subscription.Establishment.Email.IndexOf('@')..] : "***");
            // TODO: Enviar notificação via WhatsApp/Email
        }
    }

    private async Task HandleInvoicePaid(Event stripeEvent)
    {
        // Usar nome completo para evitar ambiguidade com Models.Billing.Invoice
        var stripeInvoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (stripeInvoice == null) return;

        // Stripe.NET 48.x: SubscriptionId foi movido para Parent.SubscriptionDetails.Subscription.Id
        var subscriptionId = stripeInvoice.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var subscription = await _context.Subscriptions
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription == null) return;

        var existing = await _context.SubscriptionInvoices
            .FirstOrDefaultAsync(i => i.StripeInvoiceId == stripeInvoice.Id);
        
        if (existing == null)
        {
            var newInvoice = new SubscriptionInvoice
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                StripeInvoiceId = stripeInvoice.Id,
                Amount = stripeInvoice.AmountPaid / 100m,
                Status = "PAID",
                GatewayType = "Stripe",
                PaymentMethod = "card",
                InvoiceUrl = stripeInvoice.HostedInvoiceUrl,
                InvoicePdfUrl = stripeInvoice.InvoicePdf,
                PaidAt = DateTime.UtcNow,
                DueDate = stripeInvoice.DueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.SubscriptionInvoices.Add(newInvoice);
        }
        else
        {
            existing.Status = "PAID";
            existing.PaidAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        if (subscription.Status != "ACTIVE")
        {
            subscription.Status = "ACTIVE";
            subscription.UpdatedAt = DateTime.UtcNow;
            
            if (subscription.Establishment != null)
            {
                subscription.Establishment.SubscriptionStatus = "ACTIVE";
                subscription.Establishment.IsActive = true;
                subscription.Establishment.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Invoice paga: {InvoiceId} - R$ {Amount}", stripeInvoice.Id, stripeInvoice.AmountPaid / 100m);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var stripeInvoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (stripeInvoice == null) return;

        // Stripe.NET 48.x: SubscriptionId foi movido para Parent.SubscriptionDetails.Subscription.Id
        var subscriptionId = stripeInvoice.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (string.IsNullOrEmpty(subscriptionId)) return;

        var subscription = await _context.Subscriptions
            .Include(s => s.Establishment)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription == null) return;

        subscription.Status = "PAST_DUE";
        subscription.UpdatedAt = DateTime.UtcNow;

        if (subscription.Establishment != null)
        {
            subscription.Establishment.SubscriptionStatus = "PAST_DUE";
            subscription.Establishment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogWarning("Pagamento falhou: {InvoiceId}", stripeInvoice.Id);
    }

    private static string MapStatus(string s) => s switch
    {
        "active" => "ACTIVE",
        "trialing" => "TRIALING",
        "past_due" => "PAST_DUE",
        "canceled" => "CANCELED",
        "unpaid" => "PAST_DUE",
        "incomplete" => "INCOMPLETE",
        "incomplete_expired" => "CANCELED",
        "paused" => "PAUSED",
        _ => "UNKNOWN"
    };
}
