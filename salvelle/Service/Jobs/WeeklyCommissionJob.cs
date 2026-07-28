using Data;
using Microsoft.EntityFrameworkCore;
using Service.Marketplace;

namespace Services.Jobs;

/// <summary>
/// Job semanal para calcular comissões de todas as farmácias do marketplace.
/// Executa toda segunda-feira às 03:00 UTC calculando a semana anterior.
/// </summary>
public class WeeklyCommissionJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyCommissionJob> _logger;

    public WeeklyCommissionJob(IServiceProvider serviceProvider, ILogger<WeeklyCommissionJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WeeklyCommissionJob iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextMonday = GetNextMonday(now).AddHours(3); // Segunda 03:00 UTC

                if (nextMonday <= now)
                    nextMonday = nextMonday.AddDays(7);

                var delay = nextMonday - now;
                _logger.LogInformation("Próximo cálculo de comissões em {Delay} ({NextRun:u})", delay, nextMonday);

                await Task.Delay(delay, stoppingToken);

                await CalculateCommissions(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no WeeklyCommissionJob");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task CalculateCommissions(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var commissionService = scope.ServiceProvider.GetRequiredService<CommissionService>();

        _logger.LogInformation("Calculando comissões semanais...");

        // Calcular para a semana que acabou (ontem era domingo)
        var referenceDate = DateTime.UtcNow.AddDays(-1);
        var commissions = await commissionService.CalculateWeeklyCommissionsAsync(referenceDate);

        _logger.LogInformation("Comissões calculadas para {Count} farmácias. Total: R$ {Total:F2}",
            commissions.Count,
            commissions.Sum(c => c.TotalCommissionAmount));
    }

    private static DateTime GetNextMonday(DateTime date)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        return date.Date.AddDays(daysUntilMonday);
    }
}

/// <summary>
/// Job para limpar carrinhos abandonados (mais de 48h sem atividade).
/// Executa a cada 6 horas.
/// </summary>
public class AbandonedCartCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AbandonedCartCleanupJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public AbandonedCartCleanupJob(IServiceProvider serviceProvider, ILogger<AbandonedCartCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AbandonedCartCleanupJob iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await CleanupCarts();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no AbandonedCartCleanupJob");
            }
        }
    }

    private async Task CleanupCarts()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow.AddHours(-48);
        var abandonedCarts = await db.CustomerCarts
            .Where(c => c.Status == "ACTIVE" && (c.UpdatedAt ?? c.CreatedAt) < cutoff)
            .ToListAsync();

        foreach (var cart in abandonedCarts)
            cart.Status = "ABANDONED";

        if (abandonedCarts.Count > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("{Count} carrinhos abandonados marcados", abandonedCarts.Count);
        }
    }
}
