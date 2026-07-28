using Service;

namespace Middleware;

public class CustomerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomerAuthMiddleware> _logger;

    public CustomerAuthMiddleware(RequestDelegate next, ILogger<CustomerAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, CustomerAuthService authService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Rotas públicas do portal do cliente
        var publicRoutes = new[]
        {
            "/cliente/login",
            "/cliente/cadastro",
            "/cliente/verificar",
            "/cliente/esquecisenha",
            "/cliente/redefinirsenha",
            "/cliente/definirsenha",
            "/api/cliente/auth/login",
            "/api/cliente/auth/register",
            "/api/cliente/auth/verify",
            "/api/cliente/auth/resend-code",
            "/api/cliente/auth/request-reset",
            "/api/cliente/auth/reset-password",
            "/c/" // QR Code redirect
        };

        // Se é rota pública, continua sem autenticação
        if (publicRoutes.Any(route => path.StartsWith(route)))
        {
            await _next(context);
            return;
        }

        // Verifica se é rota do portal do cliente
        if (!path.StartsWith("/cliente")
            && !path.StartsWith("/api/cliente")
            && !path.StartsWith("/api/customer-portal"))
        {
            await _next(context);
            return;
        }

        // Buscar token: cookie (web), Bearer header (mobile), ou X-Session-Token
        var sessionToken = context.Request.Cookies["CustomerSessionId"];

        if (string.IsNullOrEmpty(sessionToken))
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                sessionToken = authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        if (string.IsNullOrEmpty(sessionToken))
        {
            sessionToken = context.Request.Headers["X-Session-Token"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(sessionToken))
        {
            _logger.LogWarning("Acesso sem token em: {Path}", path);
            await HandleUnauthorized(context, path);
            return;
        }

        // Validar sessão
        var session = await authService.ValidateSessionAsync(sessionToken);

        if (session == null)
        {
            _logger.LogWarning("Sessão inválida ou expirada: {Path}", path);
            context.Response.Cookies.Delete("CustomerSessionId");
            await HandleUnauthorized(context, path);
            return;
        }

        // Adicionar dados ao contexto
        context.Items["CustomerSession"] = session;
        context.Items["Customer"] = session.Customer;
        context.Items["CustomerId"] = session.CustomerId;
        context.Items["CurrentEstablishmentId"] = session.CurrentEstablishmentId;
        context.Items["CurrentEstablishment"] = session.CurrentEstablishment;

        await _next(context);
    }

    private static async Task HandleUnauthorized(HttpContext context, string path)
    {
        if (path.StartsWith("/api/"))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"Sessão expirada. Faça login novamente.\"}");
        }
        else
        {
            context.Response.Redirect("/Cliente/Login");
        }
    }
}

// Extension method para registrar o middleware
public static class CustomerAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomerAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CustomerAuthMiddleware>();
    }
}