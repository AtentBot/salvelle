using Microsoft.EntityFrameworkCore;
using Data;
using Service.Notifications;

namespace Services.Jobs;

/// <summary>
/// Job que executa periodicamente para verificar trials expirados
/// e atualizar o status dos estabelecimentos
/// </summary>
public class TrialExpirationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrialExpirationJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public TrialExpirationJob(
        IServiceProvider serviceProvider,
        ILogger<TrialExpirationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrialExpirationJob iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredTrials(stoppingToken);
                await CheckTrialsEndingSoon(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no TrialExpirationJob");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("TrialExpirationJob finalizado");
    }

    /// <summary>
    /// Verifica trials que já expiraram e não foram convertidos
    /// </summary>
    private async Task CheckExpiredTrials(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Buscar subscriptions em TRIALING com trial expirado
        var expiredTrials = await context.Subscriptions
            .Include(s => s.Establishment)
            .Where(s => s.Status == "TRIALING" 
                     && s.TrialEnd.HasValue 
                     && s.TrialEnd.Value < now)
            .ToListAsync(ct);

        if (expiredTrials.Count == 0) return;

        _logger.LogInformation("Encontrados {Count} trials expirados", expiredTrials.Count);

        foreach (var subscription in expiredTrials)
        {
            // Se passou mais de 3 dias do fim do trial e ainda está como TRIALING,
            // provavelmente houve um problema - marcar como expirado
            if (subscription.TrialEnd.HasValue && subscription.TrialEnd.Value.AddDays(3) < now)
            {
                subscription.Status = "TRIAL_EXPIRED";
                subscription.UpdatedAt = now;

                if (subscription.Establishment != null)
                {
                    subscription.Establishment.SubscriptionStatus = "TRIAL_EXPIRED";
                    subscription.Establishment.IsActive = false;
                    subscription.Establishment.UpdatedAt = now;

                    _logger.LogWarning("Trial expirado sem conversão: {EstablishmentId} - {NomeFantasia}", 
                        subscription.EstablishmentId, subscription.Establishment.NomeFantasia);
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Verifica trials que vão expirar em 3 dias para notificação
    /// </summary>
    private async Task CheckTrialsEndingSoon(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<WhatsAppService>();

        var now = DateTime.UtcNow;
        var threeDaysFromNow = now.AddDays(3);

        // Buscar trials que vencem nos próximos 3 dias e que ainda não receberam o lembrete
        var endingSoon = await context.Subscriptions
            .Include(s => s.Establishment)
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.Status == "TRIALING"
                     && s.TrialEnd.HasValue
                     && s.TrialEnd.Value > now
                     && s.TrialEnd.Value <= threeDaysFromNow
                     && s.TrialEndingReminderSentAt == null)
            .ToListAsync(ct);

        foreach (var subscription in endingSoon)
        {
            if (subscription.Establishment == null || !subscription.TrialEnd.HasValue) continue;

            var daysLeft = (int)(subscription.TrialEnd.Value - now).TotalDays;

            _logger.LogInformation(
                "Trial ending soon: {EstablishmentId} - {NomeFantasia} - {DaysLeft} dias restantes - Email: {Email}",
                subscription.EstablishmentId,
                subscription.Establishment.NomeFantasia,
                daysLeft,
                subscription.Establishment.Email?.Length > 5 ? subscription.Establishment.Email[..2] + "***" + subscription.Establishment.Email[subscription.Establishment.Email.IndexOf('@')..] : "***");

            // Enviar lembrete via WhatsApp (exatamente uma vez por assinatura)
            var phone = subscription.Establishment.WhatsApp;
            if (string.IsNullOrWhiteSpace(phone))
            {
                _logger.LogWarning(
                    "Trial ending mas sem WhatsApp cadastrado: {EstablishmentId}",
                    subscription.EstablishmentId);
                continue;
            }

            var message = BuildTrialEndingMessage(subscription);

            try
            {
                var (success, resultMessage) = await whatsApp.SendMessageAsync(phone, message);
                if (success)
                {
                    subscription.TrialEndingReminderSentAt = now;
                    await context.SaveChangesAsync(ct);
                    _logger.LogInformation(
                        "Lembrete de fim de trial enviado: {EstablishmentId}",
                        subscription.EstablishmentId);
                }
                else
                {
                    _logger.LogWarning(
                        "Falha ao enviar lembrete de fim de trial: {EstablishmentId} - {Result}",
                        subscription.EstablishmentId, resultMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao enviar lembrete de fim de trial: {EstablishmentId}",
                    subscription.EstablishmentId);
            }
        }
    }

    /// <summary>
    /// Monta a mensagem de lembrete de fim do período de teste.
    /// </summary>
    private static string BuildTrialEndingMessage(Models.Subscription subscription)
    {
        var nome = subscription.Establishment?.NomeFantasia ?? "";
        var trialEnd = subscription.TrialEnd!.Value.ToLocalTime().ToString("dd/MM/yyyy");

        var plan = subscription.SubscriptionPlan;
        var yearly = string.Equals(subscription.BillingCycle, "YEARLY", StringComparison.OrdinalIgnoreCase);
        var valor = plan == null
            ? null
            : (yearly ? plan.PriceYearly : plan.PriceMonthly).ToString("C", new System.Globalization.CultureInfo("pt-BR"));
        var ciclo = yearly ? "ano" : "mês";

        var cobranca = valor == null
            ? $"A primeira cobrança acontecerá em {trialEnd}."
            : $"A primeira cobrança de {valor}/{ciclo} acontecerá em {trialEnd}.";

        return
            $"Olá! Seu período de teste do Salvelle{(string.IsNullOrWhiteSpace(nome) ? "" : $" ({nome})")} termina em {trialEnd}.\n\n" +
            $"{cobranca}\n\n" +
            "Se quiser continuar, não precisa fazer nada — a assinatura segue automaticamente. " +
            "Caso não queira continuar, cancele antes dessa data e você não será cobrado: https://salvelle.com/minha-assinatura";
    }
}

/// <summary>
/// Job para manutenção e estatísticas das assinaturas
/// </summary>
public class SubscriptionMaintenanceJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionMaintenanceJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public SubscriptionMaintenanceJob(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionMaintenanceJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguardar 5 minutos antes de iniciar (deixar a aplicação estabilizar)
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        _logger.LogInformation("SubscriptionMaintenanceJob iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPastDueSubscriptions(stoppingToken);
                await UpdateSubscriptionStats(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no SubscriptionMaintenanceJob");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Processa subscriptions PAST_DUE por mais de 7 dias - desativa estabelecimento
    /// </summary>
    private async Task ProcessPastDueSubscriptions(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);

        // Buscar subscriptions PAST_DUE há mais de 7 dias
        var pastDue = await context.Subscriptions
            .Include(s => s.Establishment)
            .Where(s => s.Status == "PAST_DUE"
                     && s.UpdatedAt < sevenDaysAgo)
            .ToListAsync(ct);

        if (pastDue.Count == 0) return;

        _logger.LogInformation("Encontradas {Count} subscriptions PAST_DUE há mais de 7 dias", pastDue.Count);

        foreach (var subscription in pastDue)
        {
            subscription.Status = "SUSPENDED";
            subscription.UpdatedAt = now;

            if (subscription.Establishment != null)
            {
                subscription.Establishment.SubscriptionStatus = "SUSPENDED";
                subscription.Establishment.IsActive = false;
                subscription.Establishment.UpdatedAt = now;

                _logger.LogWarning(
                    "Estabelecimento suspenso por inadimplencia: {EstablishmentId} - {NomeFantasia}",
                    subscription.EstablishmentId, subscription.Establishment.NomeFantasia);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Atualiza estatísticas de uso das subscriptions
    /// </summary>
    private async Task UpdateSubscriptionStats(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Contar assinaturas por status
        var stats = await context.Subscriptions
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var statsString = string.Join(", ", stats.Select(s => $"{s.Status}: {s.Count}"));
        _logger.LogInformation("Estatísticas de assinaturas: {Stats}", statsString);
    }
}
