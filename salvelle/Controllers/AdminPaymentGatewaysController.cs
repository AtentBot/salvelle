using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Models;
using Service;
using System.Text.Json;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/payment-gateways")]
public class AdminPaymentGatewaysController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminPaymentGatewaysController> _logger;
    private readonly IEncryptionService _encryption;
    private readonly IPaymentGatewayFactory _gatewayFactory;

    public AdminPaymentGatewaysController(
        AppDbContext context,
        ILogger<AdminPaymentGatewaysController> logger,
        IEncryptionService encryption,
        IPaymentGatewayFactory gatewayFactory)
    {
        _context = context;
        _logger = logger;
        _encryption = encryption;
        _gatewayFactory = gatewayFactory;
    }

    // ==================== LISTAR CONFIGURAÇÕES ====================

    /// <summary>
    /// Lista todas as configurações de gateway
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaymentGatewayType? gatewayType = null,
        [FromQuery] GatewayEnvironment? environment = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Set<PaymentGatewayConfig>().AsQueryable();

            if (gatewayType.HasValue)
                query = query.Where(g => g.GatewayType == gatewayType.Value);

            if (environment.HasValue)
                query = query.Where(g => g.Environment == environment.Value);

            if (isActive.HasValue)
                query = query.Where(g => g.IsActive == isActive.Value);

            var configs = await query
                .OrderBy(g => g.GatewayType)
                .ThenBy(g => g.Environment)
                .ToListAsync();

            var result = configs.Select(MapToDto).ToList();

            return Ok(new
            {
                total = result.Count,
                gateways = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar gateways de pagamento");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    /// <summary>
    /// Busca estatísticas dos gateways
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var configs = await _context.Set<PaymentGatewayConfig>().ToListAsync();

            var defaultConfig = configs.FirstOrDefault(c => c.IsDefault && c.IsActive);

            var stats = new PaymentGatewayStatsDto
            {
                TotalConfigurations = configs.Count,
                ActiveConfigurations = configs.Count(c => c.IsActive),
                ProductionConfigurations = configs.Count(c => c.Environment == GatewayEnvironment.Production),
                SandboxConfigurations = configs.Count(c => c.Environment == GatewayEnvironment.Sandbox),
                DefaultGateway = defaultConfig?.GatewayType,
                ConfigurationsByGateway = configs
                    .GroupBy(c => c.GatewayType.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas de gateways");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    /// <summary>
    /// Busca gateways disponíveis (tipos suportados)
    /// </summary>
    [HttpGet("available")]
    public IActionResult GetAvailableGateways()
    {
        var gateways = Enum.GetValues<PaymentGatewayType>()
            .Select(g => new
            {
                Type = (int)g,
                Name = g.ToString(),
                IsImplemented = _gatewayFactory.IsAvailable(g),
                RequiredFields = GetRequiredFields(g)
            })
            .ToList();

        return Ok(gateways);
    }

    // ==================== BUSCAR POR ID ====================

    /// <summary>
    /// Busca uma configuração específica
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var config = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (config == null)
                return NotFound(new { message = "Configuração não encontrada" });

            return Ok(MapToDto(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar gateway {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    // ==================== CRIAR CONFIGURAÇÃO ====================

    /// <summary>
    /// Cria uma nova configuração de gateway
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentGatewayDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(dto.DisplayName))
                return BadRequest(new { message = "Nome de exibição é obrigatório" });

            // Verifica se já existe uma config para o mesmo gateway/ambiente
            var exists = await _context.Set<PaymentGatewayConfig>()
                .AnyAsync(g => g.GatewayType == dto.GatewayType && 
                              g.Environment == dto.Environment);

            if (exists)
                return BadRequest(new { 
                    message = $"Já existe uma configuração para {dto.GatewayType} em {dto.Environment}" 
                });

            // Se vai ser default, remove default dos outros
            if (dto.SetAsDefault)
            {
                await RemoveDefaultFlagAsync(dto.Environment);
            }

            // Obtém admin logado
            var adminId = GetCurrentAdminId();

            // Criar configuração
            var config = new PaymentGatewayConfig
            {
                Id = Guid.NewGuid(),
                GatewayType = dto.GatewayType,
                Environment = dto.Environment,
                DisplayName = dto.DisplayName,
                PublicKeyEncrypted = EncryptIfNotEmpty(dto.PublicKey),
                SecretKeyEncrypted = EncryptIfNotEmpty(dto.SecretKey),
                WebhookSecretEncrypted = EncryptIfNotEmpty(dto.WebhookSecret),
                AccessTokenEncrypted = EncryptIfNotEmpty(dto.AccessToken),
                ClientIdEncrypted = EncryptIfNotEmpty(dto.ClientId),
                ClientSecretEncrypted = EncryptIfNotEmpty(dto.ClientSecret),
                ApiBaseUrl = dto.ApiBaseUrl,
                SupportedCurrencies = dto.SupportedCurrencies ?? "BRL",
                IsActive = dto.IsActive,
                IsDefault = dto.SetAsDefault,
                AdditionalSettings = dto.AdditionalSettings != null 
                    ? JsonSerializer.Serialize(dto.AdditionalSettings) 
                    : null,
                CreatedByAdminId = adminId,
                UpdatedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Gera URL do webhook
            config.WebhookUrl = GenerateWebhookUrl(dto.GatewayType);

            _context.Set<PaymentGatewayConfig>().Add(config);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Gateway {GatewayType} ({Environment}) criado por admin {AdminId}",
                dto.GatewayType, dto.Environment, adminId);

            return Ok(new
            {
                message = "Configuração criada com sucesso",
                id = config.Id,
                webhookUrl = config.WebhookUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar gateway");
            return StatusCode(500, new { message = "Erro ao criar configuração" });
        }
    }

    // ==================== ATUALIZAR CONFIGURAÇÃO ====================

    /// <summary>
    /// Atualiza uma configuração existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentGatewayDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var config = await _context.Set<PaymentGatewayConfig>().FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Configuração não encontrada" });

            // Atualiza campos básicos
            if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                config.DisplayName = dto.DisplayName;

            if (dto.ApiBaseUrl != null)
                config.ApiBaseUrl = dto.ApiBaseUrl;

            if (dto.SupportedCurrencies != null)
                config.SupportedCurrencies = dto.SupportedCurrencies;

            if (dto.IsActive.HasValue)
                config.IsActive = dto.IsActive.Value;

            // Atualiza credenciais apenas se fornecidas (não-nulas)
            if (dto.PublicKey != null)
                config.PublicKeyEncrypted = EncryptIfNotEmpty(dto.PublicKey);

            if (dto.SecretKey != null)
                config.SecretKeyEncrypted = EncryptIfNotEmpty(dto.SecretKey);

            if (dto.WebhookSecret != null)
                config.WebhookSecretEncrypted = EncryptIfNotEmpty(dto.WebhookSecret);

            if (dto.AccessToken != null)
                config.AccessTokenEncrypted = EncryptIfNotEmpty(dto.AccessToken);

            if (dto.ClientId != null)
                config.ClientIdEncrypted = EncryptIfNotEmpty(dto.ClientId);

            if (dto.ClientSecret != null)
                config.ClientSecretEncrypted = EncryptIfNotEmpty(dto.ClientSecret);

            if (dto.AdditionalSettings != null)
                config.AdditionalSettings = JsonSerializer.Serialize(dto.AdditionalSettings);

            config.UpdatedByAdminId = GetCurrentAdminId();
            config.UpdatedAt = DateTime.UtcNow;

            // Limpa resultado do teste anterior já que credenciais podem ter mudado
            if (dto.SecretKey != null || dto.PublicKey != null || dto.AccessToken != null)
            {
                config.LastTestedAt = null;
                config.LastTestStatus = null;
                config.LastTestMessage = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Gateway {Id} atualizado", id);

            return Ok(new { message = "Configuração atualizada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar gateway {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar configuração" });
        }
    }

    // ==================== DELETAR CONFIGURAÇÃO ====================

    /// <summary>
    /// Remove uma configuração
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var config = await _context.Set<PaymentGatewayConfig>().FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Configuração não encontrada" });

            // Verifica se há assinaturas usando este gateway
            // TODO: Implementar verificação quando tiver campo gateway_config_id na Subscription
            
            _context.Set<PaymentGatewayConfig>().Remove(config);
            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "Gateway {GatewayType} ({Environment}) removido. ID: {Id}",
                config.GatewayType, config.Environment, id);

            return Ok(new { message = "Configuração removida com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover gateway {Id}", id);
            return StatusCode(500, new { message = "Erro ao remover configuração" });
        }
    }

    // ==================== TESTAR CONEXÃO ====================

    /// <summary>
    /// Testa a conexão com o gateway
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var config = await _context.Set<PaymentGatewayConfig>().FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Configuração não encontrada" });

            // Obtém o serviço do gateway
            IPaymentGatewayService gatewayService;
            try
            {
                gatewayService = _gatewayFactory.GetService(config.GatewayType);
            }
            catch (NotSupportedException)
            {
                return BadRequest(new { message = $"Gateway {config.GatewayType} não está implementado ainda" });
            }

            // Executa teste
            var result = await gatewayService.TestConnectionAsync(config);

            // Atualiza status no banco
            config.LastTestedAt = DateTime.UtcNow;
            config.LastTestStatus = result.Success 
                ? ConnectionTestStatus.Success 
                : ConnectionTestStatus.Failed;
            config.LastTestMessage = result.Message;
            config.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Teste de conexão {GatewayType}: {Success} - {Message}",
                config.GatewayType, result.Success, result.Message);

            return Ok(new ConnectionTestResultDto
            {
                Success = result.Success,
                Message = result.Message,
                TestedAt = config.LastTestedAt.Value,
                ResponseTimeMs = result.ResponseTimeMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar conexão do gateway {Id}", id);
            return StatusCode(500, new { message = "Erro ao testar conexão" });
        }
    }

    // ==================== DEFINIR COMO PADRÃO ====================

    /// <summary>
    /// Define um gateway como padrão para seu ambiente
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<IActionResult> SetAsDefault(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var config = await _context.Set<PaymentGatewayConfig>().FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Configuração não encontrada" });

            if (!config.IsActive)
                return BadRequest(new { message = "Não é possível definir um gateway inativo como padrão" });

            // Remove flag de default dos outros do mesmo ambiente
            await RemoveDefaultFlagAsync(config.Environment);

            // Define este como default
            config.IsDefault = true;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedByAdminId = GetCurrentAdminId();

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Gateway {Id} ({GatewayType}) definido como padrão para {Environment}",
                id, config.GatewayType, config.Environment);

            return Ok(new { message = "Gateway definido como padrão" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao definir gateway {Id} como padrão", id);
            return StatusCode(500, new { message = "Erro ao atualizar configuração" });
        }
    }

    // ==================== TOGGLE ATIVO ====================

    /// <summary>
    /// Ativa ou desativa um gateway
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var config = await _context.Set<PaymentGatewayConfig>().FindAsync(id);
            if (config == null)
                return NotFound(new { message = "Configuração não encontrada" });

            config.IsActive = !config.IsActive;
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedByAdminId = GetCurrentAdminId();

            // Se desativou e era default, remove o flag
            if (!config.IsActive && config.IsDefault)
            {
                config.IsDefault = false;
            }

            await _context.SaveChangesAsync();

            var status = config.IsActive ? "ativado" : "desativado";
            _logger.LogInformation("Gateway {Id} {Status}", id, status);

            return Ok(new { 
                message = $"Gateway {status} com sucesso", 
                isActive = config.IsActive 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar status do gateway {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar configuração" });
        }
    }

    // ==================== LOGS DE WEBHOOK ====================

    /// <summary>
    /// Lista logs de webhooks
    /// </summary>
    [HttpGet("webhook-logs")]
    public async Task<IActionResult> GetWebhookLogs([FromQuery] WebhookLogFilterDto filter)
    {
        try
        {
            var query = _context.Set<PaymentWebhookLog>().AsQueryable();

            if (filter.GatewayType.HasValue)
                query = query.Where(l => l.GatewayType == filter.GatewayType.Value);

            if (filter.Status.HasValue)
                query = query.Where(l => l.Status == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.EventType))
                query = query.Where(l => l.EventType.Contains(filter.EventType));

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip(filter.Skip)
                .Take(Math.Min(filter.Take, 100))
                .Select(l => new WebhookLogDto
                {
                    Id = l.Id,
                    GatewayType = l.GatewayType,
                    EventType = l.EventType,
                    ExternalEventId = l.ExternalEventId,
                    Status = l.Status,
                    SignatureValid = l.SignatureValid,
                    ErrorMessage = l.ErrorMessage,
                    SubscriptionId = l.SubscriptionId,
                    InvoiceId = l.InvoiceId,
                    CreatedAt = l.CreatedAt,
                    ProcessedAt = l.ProcessedAt,
                    ProcessingTimeMs = l.ProcessingTimeMs
                })
                .ToListAsync();

            return Ok(new { total, logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar logs de webhook");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    // ==================== MÉTODOS AUXILIARES ====================

    private PaymentGatewayConfigDto MapToDto(PaymentGatewayConfig config)
    {
        return new PaymentGatewayConfigDto
        {
            Id = config.Id,
            GatewayType = config.GatewayType,
            Environment = config.Environment,
            DisplayName = config.DisplayName,
            PublicKeyMasked = MaskDecrypted(config.PublicKeyEncrypted),
            SecretKeyMasked = MaskDecrypted(config.SecretKeyEncrypted),
            WebhookSecretMasked = MaskDecrypted(config.WebhookSecretEncrypted),
            AccessTokenMasked = MaskDecrypted(config.AccessTokenEncrypted),
            HasPublicKey = !string.IsNullOrEmpty(config.PublicKeyEncrypted),
            HasSecretKey = !string.IsNullOrEmpty(config.SecretKeyEncrypted),
            HasWebhookSecret = !string.IsNullOrEmpty(config.WebhookSecretEncrypted),
            WebhookUrl = config.WebhookUrl,
            IsActive = config.IsActive,
            IsDefault = config.IsDefault,
            LastTestedAt = config.LastTestedAt,
            LastTestStatus = config.LastTestStatus,
            LastTestMessage = config.LastTestMessage,
            SupportedCurrencies = config.SupportedCurrencies,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    private string? MaskDecrypted(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
            return null;

        try
        {
            var decrypted = _encryption.Decrypt(encrypted);
            return _encryption.MaskKey(decrypted);
        }
        catch
        {
            return "***erro***";
        }
    }

    private string? EncryptIfNotEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return _encryption.Encrypt(value.Trim());
    }

    private async Task RemoveDefaultFlagAsync(GatewayEnvironment environment)
    {
        var defaults = await _context.Set<PaymentGatewayConfig>()
            .Where(g => g.Environment == environment && g.IsDefault)
            .ToListAsync();

        foreach (var config in defaults)
        {
            config.IsDefault = false;
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateWebhookUrl(PaymentGatewayType gatewayType)
    {
        var baseUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;
        var path = gatewayType switch
        {
            PaymentGatewayType.Stripe => "/api/webhooks/stripe",
            PaymentGatewayType.MercadoPago => "/api/webhooks/mercadopago",
            PaymentGatewayType.Abacatepay => "/api/webhooks/abacatepay",
            _ => "/api/webhooks/unknown"
        };
        return baseUrl + path;
    }

    private Guid? GetCurrentAdminId()
    {
        if (HttpContext.Items["SaasAdmin"] is SaasAdmin admin)
            return admin.Id;
        return null;
    }

    private bool IsSuperAdmin()
    {
        var role = HttpContext.Items["SaasAdminRole"] as string;
        return string.Equals(role, "SUPER_ADMIN", StringComparison.Ordinal);
    }

    private static object GetRequiredFields(PaymentGatewayType gatewayType)
    {
        return gatewayType switch
        {
            PaymentGatewayType.Stripe => new
            {
                publicKey = new { label = "Public Key", placeholder = "pk_test_... ou pk_live_...", required = true },
                secretKey = new { label = "Secret Key", placeholder = "sk_test_... ou sk_live_...", required = true },
                webhookSecret = new { label = "Webhook Secret", placeholder = "whsec_...", required = false }
            },
            PaymentGatewayType.MercadoPago => new
            {
                publicKey = new { label = "Public Key", placeholder = "APP_USR-...", required = true },
                accessToken = new { label = "Access Token", placeholder = "APP_USR-...-...", required = true },
                clientId = new { label = "Client ID", placeholder = "Número do App", required = false },
                clientSecret = new { label = "Client Secret", placeholder = "Secret do App", required = false },
                webhookSecret = new { label = "Webhook Secret", placeholder = "Assinatura para webhooks", required = false }
            },
            PaymentGatewayType.Abacatepay => new
            {
                publicKey = new { label = "API Key", placeholder = "Sua API Key", required = true },
                secretKey = new { label = "Secret Key", placeholder = "Sua Secret Key", required = true },
                webhookSecret = new { label = "Webhook Secret", placeholder = "Secret para validação", required = false }
            },
            _ => new { }
        };
    }
}
