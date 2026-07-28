using Data;
using Models;
using Microsoft.EntityFrameworkCore;
using DTOs;
using System.Text.Json;

namespace Service;

public class SubscriptionService
{
    private readonly AppDbContext _context;
    private readonly StripeService _stripeService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        AppDbContext context, 
        StripeService stripeService,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<(bool success, string message, Subscription? subscription)> CreateTrialSubscriptionAsync(
        Guid establishmentId, 
        Guid planId)
    {
        try
        {
            var establishment = await _context.Establishments.FindAsync(establishmentId);
            if (establishment == null)
                return (false, "Establishment not found", null);

            var plan = await _context.Set<SubscriptionPlan>().FindAsync(planId);
            if (plan == null || !plan.IsActive)
                return (false, "Plan not found or inactive", null);

            // Verificar se já existe subscription
            var existing = await _context.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);
            
            if (existing != null)
                return (false, "Subscription already exists", null);

            var trialEnd = DateTime.UtcNow.AddDays(14);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                SubscriptionPlanId = planId,
                Status = "TRIALING",
                BillingCycle = "MONTHLY",
                TrialEnd = trialEnd,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = trialEnd,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Atualizar establishment
            establishment.SubscriptionStatus = "TRIALING";
            establishment.TrialEndsAt = trialEnd;
            establishment.MaxEmployeesLimit = plan.MaxEmployees;
            establishment.MaxOrdersLimit = plan.MaxMonthlyOrders;
            establishment.FeaturesEnabled = plan.Features;

            _context.Set<Subscription>().Add(subscription);
            await _context.SaveChangesAsync();

            return (true, "Trial subscription created", subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trial subscription for establishment {EstablishmentId}", establishmentId);
            return (false, "Error creating subscription", null);
        }
    }

    public async Task<(bool success, string message)> UpdateSubscriptionStatusAsync(
        Guid subscriptionId,
        string status,
        string? stripeSubscriptionId = null,
        DateTime? currentPeriodStart = null,
        DateTime? currentPeriodEnd = null)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .Include(s => s.Establishment)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                return (false, "Subscription not found");

            subscription.Status = status;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(stripeSubscriptionId))
                subscription.StripeSubscriptionId = stripeSubscriptionId;

            if (currentPeriodStart.HasValue)
                subscription.CurrentPeriodStart = currentPeriodStart.Value;

            if (currentPeriodEnd.HasValue)
                subscription.CurrentPeriodEnd = currentPeriodEnd.Value;

            // Atualizar establishment
            if (subscription.Establishment != null)
            {
                subscription.Establishment.SubscriptionStatus = status;
                
                if (status == "ACTIVE")
                {
                    subscription.Establishment.IsActive = true;
                }
                else if (status == "CANCELED" || status == "PAST_DUE")
                {
                    // Grace period de 7 dias para PAST_DUE
                    if (status == "PAST_DUE" && subscription.CurrentPeriodEnd.HasValue)
                    {
                        var daysPastDue = (DateTime.UtcNow - subscription.CurrentPeriodEnd.Value).Days;
                        if (daysPastDue > 7)
                        {
                            subscription.Establishment.IsActive = false;
                        }
                    }
                    else if (status == "CANCELED")
                    {
                        subscription.Establishment.IsActive = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return (true, "Subscription status updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscriptionId);
            return (false, "Error updating subscription");
        }
    }

    public async Task<(bool success, string message)> ChangePlanAsync(
        Guid establishmentId,
        Guid newPlanId)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .Include(s => s.Establishment)
                .Include(s => s.SubscriptionPlan)
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

            if (subscription == null)
                return (false, "Subscription not found");

            var newPlan = await _context.Set<SubscriptionPlan>().FindAsync(newPlanId);
            if (newPlan == null || !newPlan.IsActive)
                return (false, "Plan not found or inactive");

            // Atualizar no Stripe se houver subscription ativa
            if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            {
                var newPriceId = subscription.BillingCycle == "YEARLY" 
                    ? newPlan.StripePriceIdYearly 
                    : newPlan.StripePriceIdMonthly;

                if (!string.IsNullOrEmpty(newPriceId))
                {
                    await _stripeService.UpdateSubscriptionAsync(
                        subscription.StripeSubscriptionId, 
                        newPriceId);
                }
            }

            subscription.SubscriptionPlanId = newPlanId;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Atualizar limites no establishment
            if (subscription.Establishment != null)
            {
                subscription.Establishment.MaxEmployeesLimit = newPlan.MaxEmployees;
                subscription.Establishment.MaxOrdersLimit = newPlan.MaxMonthlyOrders;
                subscription.Establishment.FeaturesEnabled = newPlan.Features;
            }

            await _context.SaveChangesAsync();
            return (true, "Plan changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing plan for establishment {EstablishmentId}", establishmentId);
            return (false, "Error changing plan");
        }
    }

    public async Task<(bool success, string message)> CancelSubscriptionAsync(
        Guid establishmentId,
        bool cancelImmediately = false,
        string? reason = null)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId);

            if (subscription == null)
                return (false, "Subscription not found");

            if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
            {
                await _stripeService.CancelSubscriptionAsync(
                    subscription.StripeSubscriptionId, 
                    cancelImmediately);
            }

            if (cancelImmediately)
            {
                subscription.Status = "CANCELED";
                subscription.CanceledAt = DateTime.UtcNow;
                
                var establishment = await _context.Establishments.FindAsync(establishmentId);
                if (establishment != null)
                {
                    establishment.IsActive = false;
                    establishment.SubscriptionStatus = "CANCELED";
                }
            }
            else
            {
                subscription.CancelAtPeriodEnd = true;
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Subscription canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription for establishment {EstablishmentId}", establishmentId);
            return (false, "Error canceling subscription");
        }
    }

    public async Task<List<SubscriptionListDto>> GetAllSubscriptionsAsync(string? status = null)
    {
        var query = _context.Set<Subscription>()
            .Include(s => s.Establishment)
            .Include(s => s.SubscriptionPlan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status.ToUpper());

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionListDto
            {
                Id = s.Id,
                EstablishmentName = s.Establishment != null ? s.Establishment.NomeFantasia : "",
                PlanName = s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "",
                Status = s.Status,
                BillingCycle = s.BillingCycle,
                Amount = s.BillingCycle == "YEARLY" 
                    ? (s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceYearly : 0)
                    : (s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceMonthly : 0),
                CurrentPeriodEnd = s.CurrentPeriodEnd
            })
            .ToListAsync();
    }

    public async Task<SubscriptionResponseDto?> GetSubscriptionByEstablishmentAsync(Guid establishmentId)
    {
        return await _context.Set<Subscription>()
            .Include(s => s.Establishment)
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.EstablishmentId == establishmentId)
            .Select(s => new SubscriptionResponseDto
            {
                Id = s.Id,
                EstablishmentId = s.EstablishmentId,
                EstablishmentName = s.Establishment != null ? s.Establishment.NomeFantasia : "",
                SubscriptionPlanId = s.SubscriptionPlanId,
                PlanName = s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : "",
                Status = s.Status,
                BillingCycle = s.BillingCycle,
                Amount = s.BillingCycle == "YEARLY"
                    ? (s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceYearly : 0)
                    : (s.SubscriptionPlan != null ? s.SubscriptionPlan.PriceMonthly : 0),
                CurrentPeriodStart = s.CurrentPeriodStart,
                CurrentPeriodEnd = s.CurrentPeriodEnd,
                TrialEnd = s.TrialEnd,
                CancelAtPeriodEnd = s.CancelAtPeriodEnd,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync();
    }
}
