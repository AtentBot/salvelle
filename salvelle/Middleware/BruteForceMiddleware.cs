using Microsoft.Extensions.Caching.Memory;

namespace Middleware;

/// <summary>
/// Bloqueia IPs que acumulam falhas de autenticação em janela deslizante.
/// 10 falhas em 15 minutos → bloqueio de 1 hora.
/// Complementa o lockout de conta (Layer 1) com defesa na camada de rede (Layer 2).
/// </summary>
public class BruteForceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BruteForceMiddleware> _logger;

    private const int MaxFailures = 10;
    private const int FailureWindowMinutes = 15;
    private const int BlockDurationMinutes = 60;

    public BruteForceMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<BruteForceMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var blockKey = $"bf:block:{ip}";

        if (_cache.TryGetValue(blockKey, out _))
        {
            _logger.LogWarning("IP bloqueado por brute-force: {IP} → {Path}", ip, context.Request.Path);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Acesso temporariamente bloqueado. Tente novamente mais tarde."
            });
            return;
        }

        await _next(context);

        // Monitorar falhas em endpoints de autenticação:
        // 401 = credenciais inválidas, 429 = rate limit atingido (sinal claro de ataque)
        if (IsAuthEndpoint(context.Request.Path) && context.Response.StatusCode is 401 or 429)
        {
            var failKey = $"bf:fail:{ip}";
            var failures = _cache.GetOrCreate(failKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(FailureWindowMinutes);
                return 0;
            });

            failures++;
            _cache.Set(failKey, failures, TimeSpan.FromMinutes(FailureWindowMinutes));

            _logger.LogDebug("Falha de auth para {IP}: {Count}/{Max}", ip, failures, MaxFailures);

            if (failures >= MaxFailures)
            {
                _cache.Set(blockKey, true, TimeSpan.FromMinutes(BlockDurationMinutes));
                _cache.Remove(failKey);
                _logger.LogWarning("IP bloqueado por {Minutes} minutos após {Max} falhas: {IP}",
                    BlockDurationMinutes, MaxFailures, ip);
            }
        }
    }

    private static bool IsAuthEndpoint(PathString path)
    {
        var p = path.Value?.ToLowerInvariant() ?? "";
        return p.Contains("/auth/login")
            || p.Contains("/auth/verify")
            || p.Contains("/auth/confirm")
            || p.Contains("/auth/request-reset")
            || p.Contains("/login");
    }
}
