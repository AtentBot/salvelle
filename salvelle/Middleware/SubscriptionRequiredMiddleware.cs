using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Middleware;

/// <summary>
/// Middleware que verifica se o establishment tem uma subscription válida (ACTIVE ou TRIALING).
/// Redireciona para seleção de plano se não tiver subscription ou se o trial expirou.
/// </summary>
public class SubscriptionRequiredMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SubscriptionRequiredMiddleware> _logger;

    public SubscriptionRequiredMiddleware(RequestDelegate next, ILogger<SubscriptionRequiredMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Rotas isentas de verificação
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        // Tentar obter EstablishmentId
        var establishmentId = GetEstablishmentId(context);
        
        if (!establishmentId.HasValue)
        {
            await _next(context);
            return;
        }

        // Verificar subscription
        var subscription = await db.Set<Subscription>()
            .Include(s => s.SubscriptionPlan)
            .FirstOrDefaultAsync(s => s.EstablishmentId == establishmentId.Value);

        // Sem subscription
        if (subscription == null)
        {
            _logger.LogWarning("Establishment {EstablishmentId} sem subscription", establishmentId.Value);
            await HandleNoSubscription(context, establishmentId.Value);
            return;
        }

        // Verificar se trial expirou
        if (subscription.Status == "TRIALING" && 
            subscription.TrialEnd.HasValue && 
            subscription.TrialEnd.Value < DateTime.UtcNow)
        {
            subscription.Status = "TRIAL_EXPIRED";
            subscription.UpdatedAt = DateTime.UtcNow;

            var establishment = await db.Establishments.FindAsync(establishmentId.Value);
            if (establishment != null)
            {
                establishment.SubscriptionStatus = "TRIAL_EXPIRED";
                establishment.IsActive = false;
            }

            await db.SaveChangesAsync();

            _logger.LogInformation("Trial expirado para establishment {EstablishmentId}", establishmentId.Value);
            await HandleTrialExpired(context, establishmentId.Value);
            return;
        }

        // Verificar status válido
        var validStatuses = new[] { "ACTIVE", "TRIALING" };
        if (!validStatuses.Contains(subscription.Status))
        {
            _logger.LogWarning("Establishment {EstablishmentId} com subscription inválida: {Status}", 
                establishmentId.Value, subscription.Status);
            await HandleInvalidSubscription(context, establishmentId.Value, subscription.Status);
            return;
        }

        // Subscription válida - adicionar info ao contexto
        context.Items["Subscription"] = subscription;
        context.Items["SubscriptionStatus"] = subscription.Status;
        context.Items["TrialEndsAt"] = subscription.TrialEnd;
        context.Items["PlanName"] = subscription.SubscriptionPlan?.Name;

        await _next(context);
    }

    private async Task HandleNoSubscription(HttpContext context, Guid establishmentId)
    {
        var redirectUrl = $"/signup/select-plan?establishmentId={establishmentId}";
        
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 402;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Você precisa escolher um plano para continuar.",
                code = "SUBSCRIPTION_REQUIRED",
                redirectTo = redirectUrl
            });
        }
        else
        {
            context.Response.Redirect(redirectUrl);
        }
    }

    private async Task HandleTrialExpired(HttpContext context, Guid establishmentId)
    {
        var redirectUrl = $"/signup/select-plan?establishmentId={establishmentId}";
        
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 402;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Seu período de teste de 14 dias expirou. A cobrança será realizada automaticamente.",
                code = "TRIAL_EXPIRED",
                redirectTo = redirectUrl
            });
        }
        else
        {
            context.Response.Redirect(redirectUrl);
        }
    }

    private async Task HandleInvalidSubscription(HttpContext context, Guid establishmentId, string status)
    {
        var redirectUrl = $"/signup/select-plan?establishmentId={establishmentId}";
        
        var message = status switch
        {
            "CANCELED" => "Sua assinatura foi cancelada. Escolha um plano para reativar o acesso.",
            "PAST_DUE" => "Sua assinatura está com pagamento pendente. Atualize seu método de pagamento.",
            "UNPAID" => "Sua assinatura não foi paga. Atualize seu método de pagamento.",
            _ => "Sua assinatura não está ativa."
        };

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 402;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = message,
                code = "SUBSCRIPTION_INVALID",
                status = status,
                redirectTo = redirectUrl
            });
        }
        else
        {
            context.Response.Redirect(redirectUrl);
        }
    }

    private Guid? GetEstablishmentId(HttpContext context)
    {
        // 1. Items (setado por outros middlewares)
        if (context.Items.TryGetValue("EstablishmentId", out var itemValue) && itemValue is Guid itemId)
            return itemId;

        // 2. Cookie
        if (context.Request.Cookies.TryGetValue("EstablishmentId", out var cookieValue) && 
            Guid.TryParse(cookieValue, out var cookieId))
            return cookieId;

        // 3. Header
        if (context.Request.Headers.TryGetValue("X-Establishment-Id", out var headerValue) &&
            Guid.TryParse(headerValue.FirstOrDefault(), out var headerId))
            return headerId;

        // 4. Claim
        var claim = context.User?.FindFirst("EstablishmentId")?.Value;
        if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var claimId))
            return claimId;

        return null;
    }

    private bool IsExemptPath(string path)
    {
        var exemptPrefixes = new[]
        {
            // Páginas públicas
            "/",
            "/landing",
            "/pricing",
            "/features",
            "/about",
            "/contact",
            "/terms",
            "/privacy",
            
            // Autenticação
            "/login",
            "/account/login",
            "/account/logout",
            "/account/forgotpassword",
            
            // Signup completo
            "/signup",
            "/api/signup",
            
            // Admin (próprio middleware)
            "/admin",
            "/api/admin",
            
            // Cliente/Portal (próprio middleware)
            "/cliente",
            "/api/cliente",
            "/api/customer-portal",
            "/c/",
            "/orcamento",
            
            // Stripe
            "/api/webhooks/stripe",
            "/api/stripe/success",
            "/api/stripe/cancel",
            
            // APIs públicas
            "/api/subscriptionplans",
            "/api/auth/login",
            "/api/auth/2fa",
            "/api/auth/logout",
            
            // Health/Docs
            "/health",
            "/swagger",
            
            // Assets
            "/css",
            "/js",
            "/images",
            "/lib",
            "/favicon",
            "/_framework"
        };

        // Verifica se o path começa com algum prefixo isento
        foreach (var prefix in exemptPrefixes)
        {
            if (path == prefix || path.StartsWith(prefix + "/") || path.StartsWith(prefix + "?"))
                return true;
        }

        // Extensões de arquivos estáticos
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf" };
        if (staticExtensions.Any(ext => path.EndsWith(ext)))
            return true;

        return false;
    }
}

public static class SubscriptionRequiredMiddlewareExtensions
{
    public static IApplicationBuilder UseSubscriptionRequired(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SubscriptionRequiredMiddleware>();
    }
}
