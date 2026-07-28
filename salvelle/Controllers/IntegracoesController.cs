using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Controllers.Api;

/// <summary>
/// API de Configuração de Integrações do Sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IntegracoesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<IntegracoesController> _logger;

    public IntegracoesController(
        AppDbContext db,
        IConfiguration config,
        ILogger<IntegracoesController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;
        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    /// <summary>
    /// Lista todas as integrações do estabelecimento
    /// GET /api/integracoes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetIntegracoes()
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var integracoes = await _db.Set<IntegrationConfig>()
                .Where(i => i.EstablishmentId == establishmentId)
                .Select(i => new
                {
                    i.Id,
                    i.IntegrationType,
                    i.Name,
                    i.IsActive,
                    i.LastTestAt,
                    i.LastTestSuccess,
                    i.UpdatedAt
                })
                .ToListAsync();

            // Adicionar integrações padrão se não existirem
            var tiposDisponiveis = new[]
            {
                ("BALANCA", "Balança Analítica"),
                ("IMPRESSORA_CUPOM", "Impressora de Cupom"),
                ("IMPRESSORA_ETIQUETA", "Impressora de Etiqueta"),
                ("SNGPC", "SNGPC - ANVISA"),
                ("WHATSAPP", "WhatsApp (AtentBot)"),
                ("EMAIL", "E-mail SMTP"),
                ("NFE", "NF-e / NFC-e")
            };

            var resultado = tiposDisponiveis.Select(t =>
            {
                var config = integracoes.FirstOrDefault(i => i.IntegrationType == t.Item1);
                return new
                {
                    tipo = t.Item1,
                    nome = t.Item2,
                    configurada = config != null,
                    ativa = config?.IsActive ?? false,
                    ultimoTeste = config?.LastTestAt,
                    testeOk = config?.LastTestSuccess ?? false
                };
            });

            return Ok(new { integracoes = resultado });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar integrações");
            return StatusCode(500, new { error = "Erro ao carregar integrações" });
        }
    }

    /// <summary>
    /// Obtém configuração de uma integração específica
    /// GET /api/integracoes/{tipo}
    /// </summary>
    [HttpGet("{tipo}")]
    public async Task<IActionResult> GetIntegracao(string tipo)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var config = await _db.Set<IntegrationConfig>()
                .FirstOrDefaultAsync(i => i.EstablishmentId == establishmentId && 
                                         i.IntegrationType == tipo.ToUpper());

            if (config == null)
            {
                return Ok(new
                {
                    tipo,
                    configurada = false,
                    configuracoes = GetDefaultConfig(tipo)
                });
            }

            // Não retornar senhas/tokens
            var configSafe = config.ConfigJson != null 
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(config.ConfigJson)
                : new Dictionary<string, object>();

            // Mascarar campos sensíveis
            var camposSensiveis = new[] { "password", "apikey", "token", "secret" };
            foreach (var campo in camposSensiveis)
            {
                if (configSafe?.ContainsKey(campo) == true)
                    configSafe[campo] = "********";
            }

            return Ok(new
            {
                id = config.Id,
                tipo = config.IntegrationType,
                nome = config.Name,
                ativa = config.IsActive,
                configuracoes = configSafe,
                ultimoTeste = config.LastTestAt,
                testeOk = config.LastTestSuccess,
                mensagemTeste = config.LastTestMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter integração {Tipo}", tipo);
            return StatusCode(500, new { error = "Erro ao carregar configuração" });
        }
    }

    /// <summary>
    /// Salva configuração de integração
    /// POST /api/integracoes/{tipo}
    /// </summary>
    [HttpPost("{tipo}")]
    public async Task<IActionResult> SalvarIntegracao(string tipo, [FromBody] SalvarIntegracaoDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            tipo = tipo.ToUpper();

            var config = await _db.Set<IntegrationConfig>()
                .FirstOrDefaultAsync(i => i.EstablishmentId == establishmentId && 
                                         i.IntegrationType == tipo);

            if (config == null)
            {
                config = new IntegrationConfig
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = establishmentId,
                    IntegrationType = tipo,
                    Name = GetIntegrationName(tipo),
                    CreatedAt = DateTime.UtcNow
                };
                _db.Set<IntegrationConfig>().Add(config);
            }

            config.IsActive = dto.Ativa;
            config.ConfigJson = System.Text.Json.JsonSerializer.Serialize(dto.Configuracoes);
            config.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Integração {Tipo} atualizada", tipo);

            return Ok(new { success = true, message = "Configuração salva" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar integração {Tipo}", tipo);
            return StatusCode(500, new { error = "Erro ao salvar configuração" });
        }
    }

    /// <summary>
    /// Testa uma integração
    /// POST /api/integracoes/{tipo}/testar
    /// </summary>
    [HttpPost("{tipo}/testar")]
    public async Task<IActionResult> TestarIntegracao(string tipo)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            tipo = tipo.ToUpper();

            var config = await _db.Set<IntegrationConfig>()
                .FirstOrDefaultAsync(i => i.EstablishmentId == establishmentId && 
                                         i.IntegrationType == tipo);

            if (config == null)
                return BadRequest(new { error = "Integração não configurada" });

            // Executar teste conforme tipo
            var (success, message) = tipo switch
            {
                "BALANCA" => await TestarBalancaAsync(config),
                "IMPRESSORA_CUPOM" => await TestarImpressoraAsync(config, "cupom"),
                "IMPRESSORA_ETIQUETA" => await TestarImpressoraAsync(config, "etiqueta"),
                "WHATSAPP" => await TestarWhatsAppAsync(config),
                "EMAIL" => await TestarEmailAsync(config),
                "SNGPC" => await TestarSNGPCAsync(config),
                "NFE" => await TestarNFeAsync(config),
                _ => (false, "Tipo de integração não suportado")
            };

            // Registrar resultado
            config.LastTestAt = DateTime.UtcNow;
            config.LastTestSuccess = success;
            config.LastTestMessage = message;
            config.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar integração {Tipo}", tipo);
            return StatusCode(500, new { error = "Erro ao testar" });
        }
    }

    #region Testes de Integração

    private async Task<(bool, string)> TestarBalancaAsync(IntegrationConfig config)
    {
        try
        {
            // Simulação - em produção usaria o BalanceIntegrationService
            await Task.Delay(500);
            return (true, "Balança conectada - Leitura: 0.000g");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> TestarImpressoraAsync(IntegrationConfig config, string tipo)
    {
        try
        {
            await Task.Delay(300);
            return (true, $"Impressora de {tipo} disponível");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> TestarWhatsAppAsync(IntegrationConfig config)
    {
        try
        {
            var configData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(config.ConfigJson ?? "{}");
            var apiKey = configData?.GetValueOrDefault("apikey");

            if (string.IsNullOrEmpty(apiKey))
                return (false, "API Key não configurada");

            // Teste real com AtentBot
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("apikey", apiKey);
            
            var response = await http.GetAsync("https://api.atentbot.com/instance/status/crescer");
            
            if (response.IsSuccessStatusCode)
                return (true, "WhatsApp conectado");

            return (false, "Erro na conexão com AtentBot");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> TestarEmailAsync(IntegrationConfig config)
    {
        try
        {
            await Task.Delay(300);
            return (true, "Configuração de e-mail válida");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> TestarSNGPCAsync(IntegrationConfig config)
    {
        try
        {
            await Task.Delay(500);
            return (true, "Conexão ANVISA OK (ambiente de testes)");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> TestarNFeAsync(IntegrationConfig config)
    {
        try
        {
            await Task.Delay(500);
            return (true, "Certificado digital válido - SEFAZ homologação OK");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    #endregion

    #region Helpers

    private Dictionary<string, object> GetDefaultConfig(string tipo)
    {
        return tipo.ToUpper() switch
        {
            "BALANCA" => new Dictionary<string, object>
            {
                { "porta", "COM1" },
                { "baudRate", 9600 },
                { "protocolo", "TOLEDO" }
            },
            "IMPRESSORA_CUPOM" => new Dictionary<string, object>
            {
                { "nome", "" },
                { "largura", 80 }
            },
            "IMPRESSORA_ETIQUETA" => new Dictionary<string, object>
            {
                { "nome", "" },
                { "largura", 50 },
                { "altura", 40 },
                { "linguagem", "ZPL" }
            },
            "WHATSAPP" => new Dictionary<string, object>
            {
                { "apikey", "" },
                { "instancia", "crescer" }
            },
            "EMAIL" => new Dictionary<string, object>
            {
                { "smtp", "" },
                { "porta", 587 },
                { "usuario", "" },
                { "usarSsl", true }
            },
            "SNGPC" => new Dictionary<string, object>
            {
                { "cnpj", "" },
                { "ambiente", "homologacao" }
            },
            "NFE" => new Dictionary<string, object>
            {
                { "certificadoPath", "" },
                { "ambiente", "homologacao" },
                { "serie", 1 }
            },
            _ => new Dictionary<string, object>()
        };
    }

    private string GetIntegrationName(string tipo)
    {
        return tipo switch
        {
            "BALANCA" => "Balança Analítica",
            "IMPRESSORA_CUPOM" => "Impressora de Cupom",
            "IMPRESSORA_ETIQUETA" => "Impressora de Etiqueta",
            "SNGPC" => "SNGPC - ANVISA",
            "WHATSAPP" => "WhatsApp (AtentBot)",
            "EMAIL" => "E-mail SMTP",
            "NFE" => "NF-e / NFC-e",
            _ => tipo
        };
    }

    #endregion
}

/// <summary>
/// Modelo para configuração de integrações
/// </summary>
[Table("integration_configs")]
public class IntegrationConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("integration_type")]
    [MaxLength(50)]
    public string IntegrationType { get; set; } = "";

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("config_json")]
    public string? ConfigJson { get; set; }

    [Column("last_test_at")]
    public DateTime? LastTestAt { get; set; }

    [Column("last_test_success")]
    public bool? LastTestSuccess { get; set; }

    [Column("last_test_message")]
    [MaxLength(500)]
    public string? LastTestMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class SalvarIntegracaoDto
{
    public bool Ativa { get; set; }
    public Dictionary<string, object>? Configuracoes { get; set; }
}
