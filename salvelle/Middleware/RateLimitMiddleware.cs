using System.Collections.Concurrent;

namespace Middleware;

/// <summary>
/// Rate limiting simples baseado em IP para APIs mobile.
/// Limita requests por IP/minuto para prevenir abuso.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // IP → lista de timestamps de requests
    private static readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();

    private const int MaxRequestsPerMinute = 120;
    private const int AuthMaxRequestsPerMinute = 10; // Login/register mais restrito
    // Limita o tamanho do dicionário para prevenir consumo ilimitado de memória
    private const int MaxTrackedKeys = 50_000;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Aplicar apenas em APIs mobile
        if (!path.StartsWith("/api/mobile/"))
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{ip}:{(IsAuthEndpoint(path) ? "auth" : "general")}";
        var limit = IsAuthEndpoint(path) ? AuthMaxRequestsPerMinute : MaxRequestsPerMinute;

        // Previne crescimento ilimitado do dicionário (ex: IPs únicos de ataque DDoS)
        if (_counters.Count >= MaxTrackedKeys)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { error = "Muitas requisições. Tente novamente em 1 minuto." });
            return;
        }

        var counter = _counters.GetOrAdd(key, _ => new SlidingWindowCounter());
        counter.CleanupOldEntries();

        // Remove chaves vazias periodicamente para liberar memória
        if (counter.Count == 0 && _counters.Count > 1000)
            _counters.TryRemove(key, out _);

        if (counter.Count >= limit)
        {
            _logger.LogWarning("Rate limit exceeded for {Key} ({Count}/{Limit})", key, counter.Count, limit);
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Muitas requisições. Tente novamente em 1 minuto."
            });
            return;
        }

        counter.Increment();
        await _next(context);
    }

    private static bool IsAuthEndpoint(string path)
    {
        return path.Contains("/auth/login") || path.Contains("/auth/register");
    }

    /// <summary>
    /// Contador de janela deslizante (1 minuto)
    /// </summary>
    private class SlidingWindowCounter
    {
        private readonly ConcurrentQueue<DateTime> _timestamps = new();

        public int Count => _timestamps.Count;

        public void Increment()
        {
            _timestamps.Enqueue(DateTime.UtcNow);
        }

        public void CleanupOldEntries()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            while (_timestamps.TryPeek(out var ts) && ts < cutoff)
                _timestamps.TryDequeue(out _);
        }
    }
}
