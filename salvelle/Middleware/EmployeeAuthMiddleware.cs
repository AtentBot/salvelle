using Data;
using Microsoft.EntityFrameworkCore;
using Models.Employees;
using System.Security.Claims;

namespace Middleware;

public class EmployeeAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EmployeeAuthMiddleware> _logger;

    public EmployeeAuthMiddleware(RequestDelegate next, ILogger<EmployeeAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // 1. ROTAS PÚBLICAS (sem autenticação)
        if (IsPublicPath(path))
        {
            _logger.LogDebug("Rota pública: {Path}", path);
            await _next(context);
            return;
        }

        // 2. ROTAS COM API KEY (sem necessidade de sessão)
        if (IsApiKeyOnlyPath(path))
        {
            _logger.LogDebug("Rota API Key: {Path}", path);
            await _next(context);
            return;
        }

        // 3. ROTAS PROTEGIDAS (requerem X-SESSION-TOKEN)
        var token = GetSessionToken(context);

        if (string.IsNullOrEmpty(token))
        {
            await HandleMissingToken(context, path);
            return;
        }

        var validationResult = await ValidateSession(db, token, path);

        if (!validationResult.IsValid)
        {
            await HandleInvalidSession(context, path, validationResult.ErrorMessage);
            return;
        }

        AddSessionToContext(context, validationResult.Session!);
        AddClaimsToUser(context, validationResult.Session!);

        _logger.LogDebug("Auth OK: {EmployeeName} ({EmployeeId}) -> {Path}",
            validationResult.Session!.Employee.FullName,
            validationResult.Session.EmployeeId,
            path);

        await _next(context);
    }

    /// <summary>
    /// Verifica se o path é de uma rota pública
    /// </summary>
    private static bool IsPublicPath(string path)
    {
        // ===== PREFIXOS PÚBLICOS =====
        var publicPrefixes = new[]
        {
            "/admin",                   // AdminAuthMiddleware cuida
            "/api/admin",               // AdminAuthMiddleware cuida
            "/cliente",                 // CustomerAuthMiddleware cuida
            "/api/cliente",             // CustomerAuthMiddleware cuida
            "/api/customer-portal/",    // APIs Portal Cliente
            "/api/mobile/",             // JwtAuthMiddleware cuida
            "/c/",                      // QR Code redirect (Portal Cliente)
            "/signup",                  // Cadastro (todas as rotas)
            "/orcamento/",              // Orçamentos públicos
            "/swagger",                 // Documentação API
            "/account/login",
            "/account/register",
            "/account/forgotpassword",
            "/account/resetpassword",
            "/account/validatecode"
        };

        if (publicPrefixes.Any(prefix => path.StartsWith(prefix)))
            return true;

        // ===== ROTAS EXATAS =====
        var exactPaths = new[]
        {
            "/",
            "/landing",
            "/health",
            "/home",
            "/home/index",
            "/home/error",
            "/home/privacy",
            "/pricing",
            "/features",
            "/about",
            "/contact",
            "/terms",
            "/privacy",
            "/login"
        };

        if (exactPaths.Contains(path))
            return true;

        // ===== APIs PÚBLICAS =====
        var publicApis = new[]
        {
            "/api/auth/login",
            "/api/auth/logout",
            "/api/auth/register",
            "/api/employees/login",
            "/api/employees/generate-hash",
            "/api/establishment/login",
            "/api/signup",
            "/api/subscriptionplans",
            "/api/webhooks/stripe",
            "/api/prescriptionquotes/public",
            "/api/ingredients",                     // ✅ Autocomplete de ingredientes
            "/api/pricing/ingredient/search",       // ✅ Busca preço por nome (Portal Cliente)
            "/api/pricing/formula",                 // ✅ Cálculo de fórmula (Portal Cliente)
            "/api/wa/diag"                          // ✅ Diagnóstico WhatsApp/AtentBot
        };

        if (publicApis.Any(api => path.StartsWith(api)))
            return true;

        // ===== ARQUIVOS ESTÁTICOS =====
        var staticExtensions = new[]
        {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico",
            ".svg", ".woff", ".woff2", ".ttf", ".eot", ".map"
        };

        if (staticExtensions.Any(ext => path.EndsWith(ext)))
            return true;

        return false;
    }

    /// <summary>
    /// Rotas protegidas apenas com API Key (sem sessão)
    /// </summary>
    private static bool IsApiKeyOnlyPath(string path)
    {
        var apiKeyPaths = new[]
        {
            "/api/auth/password/request-reset",
            "/api/auth/password/verify-code",
            "/api/auth/password/reset"
        };

        return apiKeyPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Obtém o token de sessão do Cookie ou Header
    /// </summary>
    private static string? GetSessionToken(HttpContext context)
    {
        // Header (APIs, mobile)
        var headerToken = context.Request.Headers["X-Session-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerToken))
            return headerToken;

        // Cookie principal
        var cookieToken = context.Request.Cookies["X-SESSION-TOKEN"];
        if (!string.IsNullOrEmpty(cookieToken))
            return cookieToken;

        // Fallback (compatibilidade)
        return context.Request.Cookies["SessionId"];
    }

    /// <summary>
    /// Valida a sessão no banco de dados
    /// </summary>
    private async Task<SessionValidationResult> ValidateSession(AppDbContext db, string token, string path)
    {
        try
        {
            var session = await db.EmployeeSessions
                .Include(s => s.Employee)
                    .ThenInclude(e => e.JobPosition)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.Establishment)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

            if (session == null)
            {
                _logger.LogWarning("Token inválido: {TokenPrefix}...", token[..Math.Min(10, token.Length)]);
                return SessionValidationResult.Fail("Token inválido ou sessão não encontrada");
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Sessão expirada: {EmployeeId}", session.EmployeeId);
                session.IsActive = false;
                await db.SaveChangesAsync();
                return SessionValidationResult.Fail("Sessão expirada. Faça login novamente.");
            }

            if (session.Employee == null)
            {
                _logger.LogWarning("Sessão sem Employee: {SessionId}", session.Id);
                return SessionValidationResult.Fail("Funcionário não encontrado");
            }

            if (session.Employee.Status.ToUpper() != "ATIVO")
            {
                _logger.LogWarning("Employee inativo: {EmployeeId}", session.EmployeeId);
                return SessionValidationResult.Fail("Funcionário inativo");
            }

            if (session.Employee.Establishment == null || !session.Employee.Establishment.IsActive)
            {
                _logger.LogWarning("Establishment inativo: {EstablishmentId}", session.Employee.EstablishmentId);
                return SessionValidationResult.Fail("Estabelecimento inativo");
            }

            session.LastActivityAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return SessionValidationResult.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar sessão: {Path}", path);
            return SessionValidationResult.Fail("Erro ao validar autenticação");
        }
    }

    private static void AddSessionToContext(HttpContext context, EmployeeSession session)
    {
        context.Items["EmployeeId"] = session.EmployeeId;
        context.Items["Employee"] = session.Employee;
        context.Items["EmployeeName"] = session.Employee.FullName;
        context.Items["EmployeeJobPositionCode"] = session.Employee.JobPosition?.Code;
        context.Items["EstablishmentId"] = session.Employee.EstablishmentId;
        context.Items["SessionId"] = session.Id;
    }

    private static void AddClaimsToUser(HttpContext context, EmployeeSession session)
    {
        var claims = new List<Claim>
        {
            new("EmployeeId", session.EmployeeId.ToString()),
            new("EstablishmentId", session.Employee.EstablishmentId.ToString()),
            new("EmployeeName", session.Employee.FullName),
            new(ClaimTypes.Name, session.Employee.FullName),
            new("SessionId", session.Id.ToString())
        };

        if (session.Employee.JobPosition != null)
            claims.Add(new Claim("JobPositionCode", session.Employee.JobPosition.Code));

        if (!string.IsNullOrEmpty(session.Employee.Email))
            claims.Add(new Claim(ClaimTypes.Email, session.Employee.Email));

        var identity = new ClaimsIdentity(claims, "Cookies");
        context.User = new ClaimsPrincipal(identity);
    }

    private async Task HandleMissingToken(HttpContext context, string path)
    {
        _logger.LogWarning("Acesso sem token: {Path}", path);

        if (path.StartsWith("/api/"))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Token de autenticação não fornecido",
                message = "Envie o token no header X-Session-Token ou no cookie X-SESSION-TOKEN"
            });
        }
        else
        {
            context.Response.Redirect("/Account/Login");
        }
    }

    private async Task HandleInvalidSession(HttpContext context, string path, string errorMessage)
    {
        if (path.StartsWith("/api/"))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = errorMessage });
        }
        else
        {
            context.Response.Redirect("/Account/Login");
        }
    }
}

internal class SessionValidationResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public EmployeeSession? Session { get; private set; }

    private SessionValidationResult() { }

    public static SessionValidationResult Success(EmployeeSession session) => new()
    {
        IsValid = true,
        Session = session
    };

    public static SessionValidationResult Fail(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

public static class EmployeeAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseEmployeeAuth(this IApplicationBuilder builder)
        => builder.UseMiddleware<EmployeeAuthMiddleware>();
}