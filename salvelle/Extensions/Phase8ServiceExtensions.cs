using Services.Integration;

namespace Extensions;

/// <summary>
/// Extensões para configuração de serviços da Fase 8/9
/// Adicione ao Program.cs: builder.Services.AddPhase8Integration();
/// </summary>
public static class Phase8ServiceExtensions
{
    /// <summary>
    /// Adiciona serviços de integração (balanças, impressoras, etc.)
    /// </summary>
    public static IServiceCollection AddPhase8Integration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurações de balança
        services.Configure<BalanceSettings>(options =>
        {
            var section = configuration.GetSection("Integration:Balance");
            options.PortName = section["PortName"] ?? "COM1";
            options.BaudRate = int.TryParse(section["BaudRate"], out var baud) ? baud : 9600;
            options.DataBits = int.TryParse(section["DataBits"], out var bits) ? bits : 8;
            options.Parity = section["Parity"] ?? "None";
            options.StopBits = section["StopBits"] ?? "One";
            options.Protocol = section["Protocol"] ?? "TOLEDO";
            options.ReadTimeoutMs = int.TryParse(section["ReadTimeoutMs"], out var readTimeout) ? readTimeout : 3000;
            options.WriteTimeoutMs = int.TryParse(section["WriteTimeoutMs"], out var writeTimeout) ? writeTimeout : 1000;
            options.ReadDelayMs = int.TryParse(section["ReadDelayMs"], out var readDelay) ? readDelay : 200;
        });

        // Serviço de balança (Singleton para manter conexão)
        services.AddSingleton<BalanceIntegrationService>();

        // Configurações de impressora térmica
        var printerSettings = new ThermalPrinterSettings
        {
            CupomPrinterName = configuration["Integration:Printer:Cupom"],
            EtiquetaPrinterName = configuration["Integration:Printer:Etiqueta"],
            CupomWidth = int.TryParse(configuration["Integration:Printer:CupomWidth"], out var cw) ? cw : 80,
            EtiquetaWidth = int.TryParse(configuration["Integration:Printer:EtiquetaWidth"], out var ew) ? ew : 50
        };
        services.AddSingleton(printerSettings);
        services.AddSingleton<ThermalPrinterService>();

        return services;
    }

    /// <summary>
    /// Adiciona health checks customizados
    /// </summary>
    public static IServiceCollection AddPhase9HealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Health check básico sem dependência de pacote extra
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
                tags: new[] { "self" });

        // Para health check de PostgreSQL, instale:
        // dotnet add package AspNetCore.HealthChecks.NpgSql
        // E use: .AddNpgSql(connectionString, name: "postgresql")

        return services;
    }
}

/// <summary>
/// Exemplo de configuração no appsettings.json:
/// 
/// "Integration": {
///   "Balance": {
///     "PortName": "COM1",
///     "BaudRate": 9600,
///     "Protocol": "TOLEDO"
///   },
///   "Printer": {
///     "Cupom": "EPSON TM-T20",
///     "Etiqueta": "Zebra ZD220",
///     "CupomWidth": 80,
///     "EtiquetaWidth": 50
///   }
/// }
/// </summary>

/*
=== INSTRUÇÕES DE USO ===

1. Adicione os arquivos de serviço ao projeto:
   - Services/Integration/BalanceIntegrationService.cs
   - Services/Integration/ThermalPrinterService.cs

2. No Program.cs, adicione:

   // Após builder.Services.AddDbContext...
   builder.Services.AddPhase8Integration(builder.Configuration);
   builder.Services.AddPhase9HealthChecks(builder.Configuration);

3. Configure o appsettings.json conforme exemplo acima

4. Execute o script SQL para criar as tabelas:
   - integration_configs
   - weighing_logs
   - print_logs

5. Copie os controllers:
   - Controllers/Api/BalancaController.cs
   - Controllers/Api/ImpressoraController.cs
   - Controllers/Api/IntegracoesController.cs
   - Controllers/Api/HealthController.cs

6. Copie as views:
   - Views/Integracoes/Index.cshtml

7. Adicione o link no menu lateral:
   <a href="/Integracoes" class="list-group-item">
       <i class="bi bi-plug"></i> Integrações
   </a>

*/
