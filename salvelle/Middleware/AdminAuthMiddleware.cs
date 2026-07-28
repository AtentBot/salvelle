using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Middleware;

public class AdminAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminAuthMiddleware> _logger;

    // Rotas admin que NÃO precisam de autenticação
    private static readonly string[] PublicAdminPaths = new[]
    {
        "/admin/login",
        "/admin/forgot-password",
        "/admin/reset-password",
        "/api/admin/auth/login",
        "/api/admin/auth/forgot-password",
        "/api/admin/auth/reset-password",
        "/api/admin/auth/validate-reset-token"
    };

    public AdminAuthMiddleware(RequestDelegate next, ILogger<AdminAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Rotas públicas gerais (não precisam de autenticação)
        if (IsPublicRoute(path))
        {
            await _next(context);
            return;
        }

        // Rotas admin
        if (path.StartsWith("/admin") || path.StartsWith("/api/admin"))
        {
            // Verificar se é rota pública do admin (login, forgot-password, etc)
            if (IsPublicAdminPath(path))
            {
                await _next(context);
                return;
            }

            var sessionToken = context.Request.Cookies["AdminSessionId"];
            
            if (string.IsNullOrEmpty(sessionToken))
            {
                if (path.StartsWith("/api/admin"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Não autenticado" });
                    return;
                }
                
                context.Response.Redirect("/admin/login");
                return;
            }

            var session = await db.Set<SaasAdminSession>()
                .Include(s => s.SaasAdmin)
                .FirstOrDefaultAsync(s => s.Token == sessionToken &&
                                         s.IsActive &&
                                         s.ExpiresAt > DateTime.UtcNow);

            if (session?.SaasAdmin == null || !session.SaasAdmin.IsActive)
            {
                context.Response.Cookies.Delete("AdminSessionId");
                
                if (path.StartsWith("/api/admin"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Sessão inválida ou expirada" });
                    return;
                }
                
                context.Response.Redirect("/admin/login");
                return;
            }

            // Atualizar última atividade
            session.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Adicionar informações ao contexto
            context.Items["SaasAdmin"] = session.SaasAdmin;
            context.Items["SaasAdminId"] = session.SaasAdminId;
            context.Items["SaasAdminRole"] = session.SaasAdmin.Role;
        }

        await _next(context);
    }

    private static bool IsPublicAdminPath(string path)
    {
        return PublicAdminPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                                         path.StartsWith(p + "?", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPublicRoute(string path)
    {
        var publicRoutes = new[]
        {
            "/",
            "/landing",
            "/pricing",
            "/features",
            "/about",
            "/contact",
            "/terms",
            "/privacy",
            "/signup",
            "/api/signup",
            "/api/subscriptionplans",
            "/api/webhooks/stripe",
            "/api/stripe/success",
            "/api/stripe/cancel"
        };

        return publicRoutes.Any(route => path == route || path.StartsWith(route + "/"));
    }
}

public static class AdminAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdminAuthMiddleware>();
    }
}
