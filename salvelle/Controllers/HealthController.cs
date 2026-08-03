using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Helpers;
using System.Diagnostics;
using System.Reflection;

namespace Controllers.Api;

/// <summary>
/// API de Health Check e Diagnóstico do Sistema
/// Fase 9: Monitoramento para Go-Live
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<HealthController> _logger;
    private readonly IWebHostEnvironment _env;

    public HealthController(
        AppDbContext db,
        IConfiguration config,
        ILogger<HealthController> logger,
        IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Health check básico (para load balancers)
    /// GET /api/health
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Health check detalhado
    /// GET /api/health/detailed
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var checks = new List<HealthCheckResult>();

        // 1. Database
        checks.Add(await CheckDatabaseAsync());

        // 2. Disk Space
        checks.Add(CheckDiskSpace());

        // 3. Memory
        checks.Add(CheckMemory());

        // 4. External Services
        checks.Add(await CheckExternalServicesAsync());

        var overallStatus = checks.All(c => c.Status == "healthy") 
            ? "healthy" 
            : checks.Any(c => c.Status == "unhealthy") 
                ? "unhealthy" 
                : "degraded";

        return Ok(new
        {
            status = overallStatus,
            timestamp = DateTime.UtcNow,
            version = GetVersion(),
            environment = _env.EnvironmentName,
            checks
        });
    }

    /// <summary>
    /// Diagnóstico completo do sistema
    /// GET /api/health/diagnostics
    /// </summary>
    [HttpGet("diagnostics")]
    public async Task<IActionResult> GetDiagnostics()
    {
        var sw = Stopwatch.StartNew();

        var diagnostics = new SystemDiagnostics
        {
            Timestamp = DateTime.UtcNow,
            Version = GetVersion(),
            Environment = _env.EnvironmentName,
            
            // Sistema
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            
            // .NET
            DotNetVersion = Environment.Version.ToString(),
            Is64BitProcess = Environment.Is64BitProcess,
            
            // Memória
            WorkingSet = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024, // MB
            GCTotalMemory = GC.GetTotalMemory(false) / 1024 / 1024, // MB
            
            // Uptime
            ProcessUptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()
        };

        // Database stats
        try
        {
            diagnostics.DatabaseStats = await GetDatabaseStatsAsync();
        }
        catch (Exception ex)
        {
            diagnostics.DatabaseStats = new DatabaseStats { Error = ex.Message };
        }

        // API Stats
        diagnostics.ApiStats = await GetApiStatsAsync();

        sw.Stop();
        diagnostics.DiagnosticsTime = sw.ElapsedMilliseconds;

        return Ok(diagnostics);
    }

    /// <summary>
    /// Informações da versão
    /// GET /api/health/version
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = System.IO.File.GetLastWriteTime(assembly.Location);

        return Ok(new
        {
            version = version?.ToString() ?? "1.0.0",
            buildDate,
            environment = _env.EnvironmentName,
            aspNetCoreVersion = Environment.Version.ToString()
        });
    }

    /// <summary>
    /// Readiness check (para Kubernetes)
    /// GET /api/health/ready
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Verificar se consegue conectar ao banco
            await _db.Database.CanConnectAsync();

            // Verificar se as tabelas principais existem
            var hasEmployees = await _db.Employees.AnyAsync();
            var hasEstablishments = await _db.Establishments.AnyAsync();

            return Ok(new { ready = true, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new { ready = false });
        }
    }

    /// <summary>
    /// Liveness check (para Kubernetes)
    /// GET /api/health/live
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new { alive = true, timestamp = DateTime.UtcNow });
    }

    #region Checks

    private async Task<HealthCheckResult> CheckDatabaseAsync()
    {
        var check = new HealthCheckResult { Name = "Database", Component = "PostgreSQL" };
        var sw = Stopwatch.StartNew();

        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            sw.Stop();

            if (canConnect)
            {
                check.Status = "healthy";
                check.Message = "Conexão OK";
                check.ResponseTime = sw.ElapsedMilliseconds;

                // Testar uma query simples
                var count = await _db.Establishments.CountAsync();
                check.Details = new { establishments = count };
            }
            else
            {
                check.Status = "unhealthy";
                check.Message = "Não foi possível conectar";
            }
        }
        catch (Exception ex)
        {
            check.Status = "unhealthy";
            check.Message = ex.Message;
            check.ResponseTime = sw.ElapsedMilliseconds;
        }

        return check;
    }

    private HealthCheckResult CheckDiskSpace()
    {
        var check = new HealthCheckResult { Name = "DiskSpace", Component = "Storage" };

        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/");
            var freeSpaceGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            var totalSpaceGB = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            var usedPercent = (1 - (freeSpaceGB / totalSpaceGB)) * 100;

            check.Status = freeSpaceGB > 1 ? "healthy" : freeSpaceGB > 0.5 ? "degraded" : "unhealthy";
            check.Message = $"{freeSpaceGB:F1} GB livres ({usedPercent:F0}% usado)";
            check.Details = new { freeGB = freeSpaceGB, totalGB = totalSpaceGB, usedPercent };
        }
        catch (Exception ex)
        {
            check.Status = "unhealthy";
            check.Message = ex.Message;
        }

        return check;
    }

    private HealthCheckResult CheckMemory()
    {
        var check = new HealthCheckResult { Name = "Memory", Component = "RAM" };

        try
        {
            var process = Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / 1024.0 / 1024.0;
            var gcMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

            check.Status = workingSetMB < 500 ? "healthy" : workingSetMB < 1000 ? "degraded" : "unhealthy";
            check.Message = $"Working Set: {workingSetMB:F0} MB";
            check.Details = new { workingSetMB, gcMemoryMB, gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2) };
        }
        catch (Exception ex)
        {
            check.Status = "unhealthy";
            check.Message = ex.Message;
        }

        return check;
    }

    private async Task<HealthCheckResult> CheckExternalServicesAsync()
    {
        var check = new HealthCheckResult { Name = "ExternalServices", Component = "APIs" };
        var services = new Dictionary<string, bool>();

        // AtentBot (WhatsApp)
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync("https://api.atentbot.com/");
            services["AtentBot"] = response.IsSuccessStatusCode;
        }
        catch
        {
            services["AtentBot"] = false;
        }

        var allOk = services.Values.All(v => v);
        check.Status = allOk ? "healthy" : "degraded";
        check.Message = allOk ? "Todos os serviços OK" : "Alguns serviços indisponíveis";
        check.Details = services;

        return check;
    }

    private async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        var stats = new DatabaseStats();

        // Contagens
        stats.Establishments = await _db.Establishments.CountAsync();
        stats.Employees = await _db.Employees.CountAsync();
        stats.Customers = await _db.Customers.CountAsync();
        stats.RawMaterials = await _db.RawMaterials.CountAsync();
        stats.Batches = await _db.Batches.CountAsync();
        stats.ManipulationOrders = await _db.ManipulationOrders.CountAsync();
        stats.Sales = await _db.Sales.CountAsync();

        // Tamanho do banco (PostgreSQL)
        try
        {
            var sizeQuery = await _db.Database
                .SqlQueryRaw<long>("SELECT pg_database_size(current_database())")
                .FirstOrDefaultAsync();
            stats.DatabaseSizeMB = sizeQuery / 1024.0 / 1024.0;
        }
        catch
        {
            stats.DatabaseSizeMB = 0;
        }

        return stats;
    }

    private async Task<ApiStats> GetApiStatsAsync()
    {
        var stats = new ApiStats();

        // Vendas do dia
        var hoje = DateTime.UtcNow.Date;
        stats.SalesToday = await _db.Sales.CountAsync(s => s.CreatedAt.Date == hoje);
        stats.SalesTodayValue = await _db.Sales
            .Where(s => s.CreatedAt.Date == hoje)
            .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

        // Ordens em produção (não fechadas: nem finalizadas/entregues nem canceladas)
        stats.OrdersInProduction = await _db.ManipulationOrders
            .CountAsync(o => !ManipulationStatus.Closed.Contains(o.Status));

        // Alertas ativos
        stats.CriticalStock = await _db.RawMaterials
            .CountAsync(r => r.IsActive && r.CurrentStock < r.MinimumStock);

        var limite30Dias = hoje.AddDays(30);
        stats.ExpiringBatches = await _db.Batches
            .CountAsync(b => b.ExpiryDate <= limite30Dias && b.ExpiryDate > hoje && b.CurrentQuantity > 0);

        return stats;
    }

    private string GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    #endregion
}

#region Models

public class HealthCheckResult
{
    public string Name { get; set; } = "";
    public string Component { get; set; } = "";
    public string Status { get; set; } = "unknown";
    public string? Message { get; set; }
    public long? ResponseTime { get; set; }
    public object? Details { get; set; }
}

public class SystemDiagnostics
{
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "";
    public string Environment { get; set; } = "";
    
    // Sistema
    public string MachineName { get; set; } = "";
    public string OSVersion { get; set; } = "";
    public int ProcessorCount { get; set; }
    
    // .NET
    public string DotNetVersion { get; set; } = "";
    public bool Is64BitProcess { get; set; }
    
    // Memória
    public long WorkingSet { get; set; }
    public long GCTotalMemory { get; set; }
    
    // Uptime
    public TimeSpan ProcessUptime { get; set; }
    
    // Stats
    public DatabaseStats? DatabaseStats { get; set; }
    public ApiStats? ApiStats { get; set; }
    
    public long DiagnosticsTime { get; set; }
}

public class DatabaseStats
{
    public int Establishments { get; set; }
    public int Employees { get; set; }
    public int Customers { get; set; }
    public int RawMaterials { get; set; }
    public int Batches { get; set; }
    public int ManipulationOrders { get; set; }
    public int Sales { get; set; }
    public double DatabaseSizeMB { get; set; }
    public string? Error { get; set; }
}

public class ApiStats
{
    public int SalesToday { get; set; }
    public decimal SalesTodayValue { get; set; }
    public int OrdersInProduction { get; set; }
    public int CriticalStock { get; set; }
    public int ExpiringBatches { get; set; }
}

#endregion
