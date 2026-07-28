using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Service;
using Stripe.Checkout;

namespace Controllers;

public class SignupMvcController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<SignupMvcController> _logger;
    private readonly IEncryptionService _encryption;

    public SignupMvcController(
        AppDbContext context, 
        ILogger<SignupMvcController> logger,
        IEncryptionService encryption)
    {
        _context = context;
        _logger = logger;
        _encryption = encryption;
    }

    /// <summary>
    /// Passo 1: Formulário de cadastro do estabelecimento
    /// </summary>
    [HttpGet("/signup")]
    public async Task<IActionResult> Index([FromQuery] Guid? planId = null)
    {
        var plans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        ViewBag.Plans = plans;
        ViewBag.SelectedPlanId = planId;

        return View();
    }

    /// <summary>
    /// Passo 2: Verificação do código enviado via WhatsApp
    /// </summary>
    [HttpGet("/signup/verify")]
    public IActionResult Verify([FromQuery] string? whatsapp = null)
    {
        ViewBag.WhatsApp = whatsapp;
        return View();
    }

    /// <summary>
    /// Passo 3: Completar perfil do proprietário (CPF, nome completo)
    /// </summary>
    [HttpGet("/signup/complete-profile")]
    public async Task<IActionResult> CompleteProfile([FromQuery] Guid establishmentId)
    {
        if (establishmentId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Estabelecimento não informado";
            return RedirectToAction("Index");
        }

        var establishment = await _context.Establishments.FindAsync(establishmentId);
        if (establishment == null)
        {
            TempData["ErrorMessage"] = "Estabelecimento não encontrado";
            return RedirectToAction("Index");
        }

        if (!establishment.IsActive)
        {
            TempData["ErrorMessage"] = "Verifique o código primeiro";
            return RedirectToAction("Verify");
        }

        // Verificar se já existe proprietário
        var hasOwner = await _context.Set<Models.Employees.Employee>()
            .AnyAsync(e => e.EstablishmentId == establishmentId);

        if (hasOwner)
        {
            // Verificar se já tem subscription
            var hasSubscription = await _context.Set<Subscription>()
                .AnyAsync(s => s.EstablishmentId == establishmentId);

            if (hasSubscription)
            {
                TempData["SuccessMessage"] = "Cadastro já finalizado. Faça login.";
                return Redirect("/Account/Login");
            }
            else
            {
                // Tem proprietário mas não tem subscription - redirecionar para escolha de plano
                return RedirectToAction("SelectPlan", new { establishmentId });
            }
        }

        ViewBag.EstablishmentId = establishmentId;
        ViewBag.EstablishmentName = establishment.NomeFantasia;
        ViewBag.Email = establishment.Email;

        return View();
    }

    /// <summary>
    /// Passo 4: Seleção de plano - NOVA ACTION
    /// </summary>
    [HttpGet("/signup/select-plan")]
    public async Task<IActionResult> SelectPlan([FromQuery] Guid establishmentId, [FromQuery] bool canceled = false)
    {
        if (establishmentId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Estabelecimento não informado";
            return RedirectToAction("Index");
        }

        var establishment = await _context.Establishments.FindAsync(establishmentId);
        if (establishment == null)
        {
            TempData["ErrorMessage"] = "Estabelecimento não encontrado";
            return RedirectToAction("Index");
        }

        // Verificar se já tem subscription ativa
        var existingSubscription = await _context.Set<Subscription>()
            .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId && 
                                     (s.Status == "ACTIVE" || s.Status == "TRIALING"));

        if (existingSubscription != null)
        {
            TempData["SuccessMessage"] = "Você já possui uma assinatura ativa.";
            return Redirect("/Account/Login");
        }

        var plans = await _context.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();

        ViewBag.Plans = plans;
        ViewBag.EstablishmentId = establishmentId;
        ViewBag.EstablishmentName = establishment.NomeFantasia;

        if (canceled)
        {
            ViewBag.ErrorMessage = "Pagamento cancelado. Escolha um plano para continuar.";
        }

        return View();
    }

    /// <summary>
    /// Passo 5: Página de sucesso após checkout do Stripe - NOVA ACTION
    /// </summary>
    [HttpGet("/signup/success")]
    public async Task<IActionResult> Success([FromQuery] string session_id)
    {
        if (string.IsNullOrEmpty(session_id))
        {
            return Redirect("/signup");
        }

        try
        {
            // ═══════════════════════════════════════════════════════════════════
            // BUSCAR API KEY DO BANCO (PaymentGatewayConfig)
            // ═══════════════════════════════════════════════════════════════════
            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe 
                                       && g.IsActive 
                                       && g.IsDefault);

            if (stripeConfig == null)
            {
                stripeConfig = await _context.Set<PaymentGatewayConfig>()
                    .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);
            }

            if (stripeConfig == null)
            {
                _logger.LogError("Nenhuma configuração do Stripe encontrada no banco");
                TempData["ErrorMessage"] = "Configuração de pagamento não encontrada.";
                return Redirect("/signup");
            }

            // Descriptografar a Secret Key
            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Secret Key do Stripe está vazia");
                TempData["ErrorMessage"] = "Configuração de pagamento inválida.";
                return Redirect("/signup");
            }

            // Configurar Stripe
            Stripe.StripeConfiguration.ApiKey = secretKey;

            // Buscar sessão do Stripe
            var service = new SessionService();
            var session = await service.GetAsync(session_id, new SessionGetOptions
            {
                Expand = new List<string> { "subscription", "customer" }
            });

            // Extrair IDs dos metadados
            var establishmentIdStr = session.Metadata.GetValueOrDefault("establishment_id");
            var planIdStr = session.Metadata.GetValueOrDefault("plan_id");
            var gatewayConfigIdStr = session.Metadata.GetValueOrDefault("gateway_config_id");

            if (!Guid.TryParse(establishmentIdStr, out var establishmentId) ||
                !Guid.TryParse(planIdStr, out var planId))
            {
                _logger.LogWarning("Metadados inválidos na sessão {SessionId}", session_id);
                return Redirect("/signup");
            }

            var establishment = await _context.Establishments.FindAsync(establishmentId);
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(planId);

            if (establishment == null || plan == null)
            {
                return Redirect("/signup");
            }

            // Idempotente: verificar se o webhook já criou a subscription
            var subscription = await _context.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

            var alreadyProcessed = subscription?.StripeSubscriptionId == session.SubscriptionId
                                   && !string.IsNullOrEmpty(subscription?.StripeSubscriptionId);

            if (!alreadyProcessed)
            {
                var trialEnd = DateTime.UtcNow.AddDays(14);

                // Parse do gateway_config_id se existir
                Guid? gatewayConfigId = null;
                if (Guid.TryParse(gatewayConfigIdStr, out var parsedGatewayId))
                {
                    gatewayConfigId = parsedGatewayId;
                }

                if (subscription == null)
                {
                    subscription = new Subscription
                    {
                        Id = Guid.NewGuid(),
                        EstablishmentId = establishmentId,
                        SubscriptionPlanId = planId,
                        StripeSubscriptionId = session.SubscriptionId,
                        StripeCustomerId = session.CustomerId,
                        ExternalSubscriptionId = session.SubscriptionId,
                        ExternalCustomerId = session.CustomerId,
                        GatewayConfigId = gatewayConfigId,
                        Status = "TRIALING",
                        BillingCycle = "MONTHLY",
                        TrialStart = DateTime.UtcNow,
                        TrialEnd = trialEnd,
                        CurrentPeriodStart = DateTime.UtcNow,
                        CurrentPeriodEnd = trialEnd,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Set<Subscription>().Add(subscription);
                }
                else
                {
                    subscription.SubscriptionPlanId = planId;
                    subscription.StripeSubscriptionId = session.SubscriptionId;
                    subscription.StripeCustomerId = session.CustomerId;
                    subscription.ExternalSubscriptionId = session.SubscriptionId;
                    subscription.ExternalCustomerId = session.CustomerId;
                    subscription.GatewayConfigId = gatewayConfigId;
                    subscription.Status = "TRIALING";
                    subscription.TrialStart = DateTime.UtcNow;
                    subscription.TrialEnd = trialEnd;
                    subscription.CurrentPeriodStart = DateTime.UtcNow;
                    subscription.CurrentPeriodEnd = trialEnd;
                    subscription.UpdatedAt = DateTime.UtcNow;
                }

                // Atualizar establishment
                establishment.SubscriptionStatus = "TRIALING";
                establishment.TrialEndsAt = trialEnd;
                establishment.MaxEmployeesLimit = plan.MaxEmployees;
                establishment.MaxOrdersLimit = plan.MaxMonthlyOrders;
                establishment.FeaturesEnabled = plan.Features;
                establishment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Subscription criada com trial para establishment {EstablishmentId}, " +
                                   "Stripe Subscription: {StripeSubscriptionId}", 
                                   establishmentId, session.SubscriptionId);

            ViewBag.SessionId = session_id;
            ViewBag.EstablishmentName = establishment.NomeFantasia;
            ViewBag.PlanName = plan.Name;
            ViewBag.TrialEndDate = (subscription?.TrialEnd ?? DateTime.UtcNow.AddDays(14)).ToString("dd/MM/yyyy");

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar sucesso do checkout para session {SessionId}", session_id);
            TempData["ErrorMessage"] = "Erro ao processar pagamento. Entre em contato com o suporte.";
            return Redirect("/signup");
        }
    }

    /// <summary>
    /// Página antiga de pagamento (redireciona para select-plan)
    /// </summary>
    [HttpGet("/signup/payment")]
    public IActionResult Payment([FromQuery] Guid establishmentId, [FromQuery] Guid? planId)
    {
        // Redirecionar para a nova página de seleção de plano
        return RedirectToAction("SelectPlan", new { establishmentId, canceled = Request.Query.ContainsKey("canceled") });
    }

    /// <summary>
    /// Página de conclusão (legado)
    /// </summary>
    [HttpGet("/signup/complete")]
    public IActionResult Complete([FromQuery] string? session = null)
    {
        // Redirecionar para Success
        if (!string.IsNullOrEmpty(session))
        {
            return RedirectToAction("Success", new { session_id = session });
        }
        return Redirect("/signup");
    }
}
