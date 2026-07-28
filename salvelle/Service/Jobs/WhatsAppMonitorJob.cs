using System.Security.Cryptography;
using System.Text.Json;
using Data;
using Microsoft.EntityFrameworkCore;
using Models.Support;
using Service.Notifications;
using Service.Support;

namespace Services.Jobs;

/// <summary>
/// Monitora o status da instância WhatsApp (Evolution API) a cada 60s.
/// Abre chamado sistêmico CRITICAL quando desconecta e resolve automaticamente ao reconectar.
/// Ao reconectar, reenviar códigos de verificação para onboardings pendentes das últimas 24h.
/// </summary>
public class WhatsAppMonitorJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WhatsAppMonitorJob> _logger;
    private readonly IConfiguration _config;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

    private const string InstanceName = "pharm";
    private const string DedupKey = "whatsapp_disconnected_pharm";

    public WhatsAppMonitorJob(
        IServiceProvider services,
        ILogger<WhatsAppMonitorJob> logger,
        IConfiguration config)
    {
        _services = services;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("WhatsAppMonitorJob iniciado — verificando instância '{Instance}' a cada {Interval}s",
            InstanceName, _interval.TotalSeconds);

        // Aguarda inicialização da app antes do primeiro check
        await Task.Delay(TimeSpan.FromSeconds(15), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckInstanceAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no WhatsAppMonitorJob");
            }

            await Task.Delay(_interval, ct);
        }

        _logger.LogInformation("WhatsAppMonitorJob finalizado");
    }

    private async Task CheckInstanceAsync(CancellationToken ct)
    {
        var apiKey = _config["AtentBot:ApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "__SET_VIA_ENV_VAR__") return;

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticketService = scope.ServiceProvider.GetRequiredService<SupportTicketService>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var mappedStatus = await FetchInstanceStatusAsync(httpFactory, apiKey, ct);

        var record = await db.WhatsAppInstanceStatuses
            .FirstOrDefaultAsync(s => s.InstanceName == InstanceName, ct);

        if (record == null)
        {
            record = new WhatsAppInstanceStatus { InstanceName = InstanceName };
            db.WhatsAppInstanceStatuses.Add(record);
        }

        var previousStatus = record.Status;
        var wasHealthy = previousStatus == "OPEN";
        var isNowHealthy = mappedStatus == "OPEN";
        var isNowUnhealthy = mappedStatus is "DISCONNECTED" or "CONNECTING";

        record.Status = mappedStatus;
        record.LastCheckedAt = DateTime.UtcNow;

        // Transição: conectado → desconectado
        if (wasHealthy && isNowUnhealthy)
        {
            record.DisconnectedSince = DateTime.UtcNow;
            _logger.LogError("WhatsApp '{Instance}' desconectado! Status: {Status}", InstanceName, mappedStatus);

            var ticket = await ticketService.OpenSystemTicketAsync(
                DedupKey,
                $"WhatsApp desconectado — instância {InstanceName}",
                $"A instância WhatsApp '{InstanceName}' está com status '{record.StatusDisplay}' desde " +
                $"{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC.\n\n" +
                "Impacto: clientes não estão recebendo códigos de verificação no cadastro.\n\n" +
                "Ação: reconectar em https://api.atentbot.com/manager/",
                "WHATSAPP",
                "CRITICAL"
            );
            record.ActiveTicketId = ticket.Id;
        }
        // Transição: desconectado → conectado
        else if (!wasHealthy && previousStatus != "UNKNOWN" && isNowHealthy)
        {
            record.LastConnectedAt = DateTime.UtcNow;
            record.DisconnectedSince = null;
            record.ActiveTicketId = null;

            _logger.LogInformation("WhatsApp '{Instance}' reconectado!", InstanceName);

            await ticketService.AutoResolveByKeyAsync(
                DedupKey,
                $"Instância WhatsApp reconectada em {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC. " +
                "Códigos de verificação pendentes foram reenviados automaticamente."
            );

            await ResendPendingCodesAsync(db, scope, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<string> FetchInstanceStatusAsync(IHttpClientFactory factory, string apiKey, CancellationToken ct)
    {
        try
        {
            using var http = factory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.Add("apikey", apiKey);

            var url = $"https://api.atentbot.com/instance/connectionState/{InstanceName}";
            var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return "UNKNOWN";

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            // Evolution API v2: { "instance": { "instanceName": "pharm", "state": "open" } }
            if (doc.RootElement.TryGetProperty("instance", out var inst) &&
                inst.TryGetProperty("state", out var stateEl))
            {
                return stateEl.GetString() switch
                {
                    "open" => "OPEN",
                    "connecting" => "CONNECTING",
                    "close" => "DISCONNECTED",
                    _ => "UNKNOWN"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar status da instância '{Instance}'", InstanceName);
        }

        return "UNKNOWN";
    }

    /// <summary>
    /// Ao reconectar, gera novo código e reenviar via WhatsApp para onboardings pendentes das últimas 24h.
    /// </summary>
    private async Task ResendPendingCodesAsync(AppDbContext db, IServiceScope scope, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var pending = await db.ClientOnboardings
            .Where(o => !o.OnboardingCompleted && !o.IsUsed && o.CreatedAt >= cutoff)
            .ToListAsync(ct);

        if (!pending.Any())
        {
            _logger.LogInformation("Nenhum onboarding pendente para reenvio pós-reconexão");
            return;
        }

        var whatsAppService = scope.ServiceProvider.GetRequiredService<WhatsAppService>();
        var resent = 0;

        foreach (var onb in pending)
        {
            var newCode = RandomNumberGenerator.GetInt32(100000, 1000000);
            onb.Numero = newCode;
            onb.CreatedAt = DateTime.UtcNow;
            onb.UpdatedAt = DateTime.UtcNow;
            onb.ExpiresAt = DateTime.UtcNow.AddMinutes(10);

            var message = $"🔐 Salvelle — Código de Verificação\n\n" +
                          $"Seu novo código é: *{newCode}*\n\n" +
                          $"Válido por 10 minutos.\n" +
                          $"Se não solicitou, ignore esta mensagem.";

            var (ok, _) = await whatsAppService.SendMessageAsync(onb.WhatsApp, message);
            if (ok) resent++;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Reenviados {Resent}/{Total} códigos pós-reconexão WhatsApp", resent, pending.Count);
    }
}
