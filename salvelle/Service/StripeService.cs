using Stripe;
using Data;
using Models;
using Microsoft.EntityFrameworkCore;

// Aliases expl�citos para evitar ambiguidades
using StripeCustomer = Stripe.Customer;
using StripeCustomerService = Stripe.CustomerService;
using StripeCustomerCreateOptions = Stripe.CustomerCreateOptions;
using StripeSubscription = Stripe.Subscription;
using StripeSubscriptionService = Stripe.SubscriptionService;
using StripeSubscriptionCreateOptions = Stripe.SubscriptionCreateOptions;
using StripeSubscriptionUpdateOptions = Stripe.SubscriptionUpdateOptions;
using StripeSubscriptionItemOptions = Stripe.SubscriptionItemOptions;
using StripeInvoice = Stripe.Invoice;
using StripeInvoiceService = Stripe.InvoiceService;
using StripeInvoiceListOptions = Stripe.InvoiceListOptions;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionSubscriptionDataOptions = Stripe.Checkout.SessionSubscriptionDataOptions;
using PortalSession = Stripe.BillingPortal.Session;
using PortalSessionService = Stripe.BillingPortal.SessionService;
using PortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;

namespace Service;

public class StripeService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<StripeService> _logger;

    public StripeService(AppDbContext context, IConfiguration config, IEncryptionService encryption, ILogger<StripeService> logger)
    {
        _context = context;
        _config = config;
        _encryption = encryption;
        _logger = logger;
    }

    /// <summary>
    /// Configura a API key do Stripe a partir do banco de dados (PaymentGatewayConfig)
    /// </summary>
    private async Task EnsureStripeConfiguredAsync()
    {
        var stripeConfig = await _context.Set<PaymentGatewayConfig>()
            .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);

        if (stripeConfig != null)
        {
            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            if (!string.IsNullOrEmpty(secretKey))
            {
                StripeConfiguration.ApiKey = secretKey;
                return;
            }
        }

        // Fallback para config (dev/teste)
        var configKey = _config["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(configKey))
        {
            StripeConfiguration.ApiKey = configKey;
            return;
        }

        throw new InvalidOperationException("Stripe API Key não configurada. Configure via PaymentGatewayConfig no banco ou Stripe:SecretKey no appsettings.");
    }

    public async Task<StripeCustomer> CreateCustomerAsync(Establishment establishment)
    {
        try
        {
            await EnsureStripeConfiguredAsync();
            var options = new StripeCustomerCreateOptions
            {
                Email = establishment.Email,
                Name = establishment.NomeFantasia,
                Phone = establishment.WhatsApp,
                Metadata = new Dictionary<string, string>
                {
                    { "establishment_id", establishment.Id.ToString() },
                    { "cnpj", establishment.Cnpj ?? "" }
                }
            };

            var service = new StripeCustomerService();
            return await service.CreateAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar customer no Stripe para establishment {EstablishmentId}", establishment.Id);
            throw;
        }
    }

    public async Task<StripeSubscription> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int trialPeriodDays = 14)
    {
        try
        {
            await EnsureStripeConfiguredAsync();
            var options = new StripeSubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<StripeSubscriptionItemOptions>
                {
                    new StripeSubscriptionItemOptions { Price = priceId }
                },
                TrialPeriodDays = trialPeriodDays,
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                }
            };

            var service = new StripeSubscriptionService();
            return await service.CreateAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar subscription no Stripe");
            throw;
        }
    }

    public async Task<CheckoutSession> CreateCheckoutSessionAsync(
        Guid establishmentId,
        Guid planId,
        string billingCycle,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(establishmentId);
            if (establishment == null)
                throw new ArgumentException("Establishment not found");

            var plan = await _context.Set<SubscriptionPlan>().FindAsync(planId);
            if (plan == null)
                throw new ArgumentException("Plan not found");

            await EnsureStripeConfiguredAsync();

            var priceId = billingCycle == "YEARLY" ? plan.StripePriceIdYearly : plan.StripePriceIdMonthly;
            if (string.IsNullOrEmpty(priceId))
                throw new ArgumentException("Price ID not configured for this plan");

            // Criar ou obter customer
            var subscription = await _context.Set<Models.Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

            string customerId;
            if (!string.IsNullOrEmpty(subscription?.StripeCustomerId))
            {
                customerId = subscription.StripeCustomerId;
            }
            else
            {
                var customer = await CreateCustomerAsync(establishment);
                customerId = customer.Id;
            }

            var options = new CheckoutSessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                // Cartão só é coletado ao final do trial (não no início) — promessa "sem cartão"
                PaymentMethodCollection = "if_required",
                LineItems = new List<CheckoutSessionLineItemOptions>
                {
                    new CheckoutSessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SubscriptionData = new CheckoutSessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 14,
                    Metadata = new Dictionary<string, string>
                    {
                        { "establishment_id", establishmentId.ToString() },
                        { "plan_id", planId.ToString() }
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "establishment_id", establishmentId.ToString() },
                    { "plan_id", planId.ToString() },
                    { "billing_cycle", billingCycle }
                }
            };

            var service = new CheckoutSessionService();
            return await service.CreateAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar checkout session");
            throw;
        }
    }

    public async Task<PortalSession> CreatePortalSessionAsync(
        Guid establishmentId,
        string returnUrl)
    {
        try
        {
            await EnsureStripeConfiguredAsync();
            var subscription = await _context.Set<Models.Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

            if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
                throw new ArgumentException("No active subscription found");

            var options = new PortalSessionCreateOptions
            {
                Customer = subscription.StripeCustomerId,
                ReturnUrl = returnUrl
            };

            var service = new PortalSessionService();
            return await service.CreateAsync(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar portal session");
            throw;
        }
    }

    public async Task<StripeSubscription> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately = false)
    {
        try
        {
            await EnsureStripeConfiguredAsync();
            var service = new StripeSubscriptionService();

            if (cancelImmediately)
            {
                return await service.CancelAsync(subscriptionId);
            }
            else
            {
                var options = new StripeSubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };
                return await service.UpdateAsync(subscriptionId, options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<StripeSubscription> UpdateSubscriptionAsync(string subscriptionId, string newPriceId)
    {
        try
        {
            await EnsureStripeConfiguredAsync();
            var service = new StripeSubscriptionService();
            var subscription = await service.GetAsync(subscriptionId);

            var options = new StripeSubscriptionUpdateOptions
            {
                Items = new List<StripeSubscriptionItemOptions>
                {
                    new StripeSubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = newPriceId
                    }
                },
                ProrationBehavior = "create_prorations"
            };

            return await service.UpdateAsync(subscriptionId, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<List<StripeInvoice>> GetInvoicesAsync(string customerId)
    {
        try
        {
            await EnsureStripeConfiguredAsync();
            var service = new StripeInvoiceService();
            var options = new StripeInvoiceListOptions
            {
                Customer = customerId,
                Limit = 100
            };

            var invoices = await service.ListAsync(options);
            return invoices.Data.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar invoices do customer {CustomerId}", customerId);
            throw;
        }
    }
}