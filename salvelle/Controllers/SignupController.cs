using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Service;
using Models;
using Stripe;
using Stripe.Checkout;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SignupController : ControllerBase
{
    private readonly SignupService _signupService;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<SignupController> _logger;
    private readonly IEncryptionService _encryption;

    private readonly AuditService _audit;

    public SignupController(
        SignupService signupService,
        AppDbContext context,
        IConfiguration config,
        ILogger<SignupController> logger,
        IEncryptionService encryption,
        AuditService audit)
    {
        _signupService = signupService;
        _context = context;
        _config = config;
        _logger = logger;
        _encryption = encryption;
        _audit = audit;
    }

    /// <summary>
    /// Passo 1: Registra o estabelecimento e envia código de verificação via WhatsApp
    /// </summary>
    [EnableRateLimiting("signup")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SignupRequestDto dto)
    {
        if (!dto.AcceptTerms)
            return BadRequest(new { message = "Você deve aceitar os termos de uso" });

        var (success, message, establishmentId) = await _signupService.RegisterAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new SignupResponseDto
        {
            EstablishmentId = establishmentId ?? Guid.Empty,
            Message = message,
            RequiresVerification = true
        });
    }

    /// <summary>
    /// Passo 2: Verifica o código enviado via WhatsApp
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifySignupCodeDto dto)
    {
        var (success, message, establishmentId) = await _signupService.VerifyCodeAsync(dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new VerifyCodeResponseDto
        { 
            EstablishmentId = establishmentId ?? Guid.Empty,
            Message = message, 
            RedirectTo = $"/signup/complete-profile?establishmentId={establishmentId}"
        });
    }

    /// <summary>
    /// Passo 3: Completa o perfil do proprietário (CPF, nome completo)
    /// </summary>
    [HttpPost("complete-profile")]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteOwnerProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Nome completo é obrigatório" });

        if (string.IsNullOrWhiteSpace(dto.Cpf))
            return BadRequest(new { message = "CPF é obrigatório" });

        var (success, message, employeeId) = await _signupService.CompleteOwnerProfileAsync(dto);

        if (!success)
            return BadRequest(new { message });

        // IMPORTANTE: Redirecionar para seleção de plano, não para login
        return Ok(new CompleteOwnerProfileResponseDto
        {
            EmployeeId = employeeId ?? Guid.Empty,
            Message = message,
            RedirectTo = $"/signup/select-plan?establishmentId={dto.EstablishmentId}"
        });
    }

    /// <summary>
    /// Passo 4: Cria sessão do Stripe Checkout com trial de 14 dias
    /// O cartão é coletado mas não cobrado até o fim do trial
    /// </summary>
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateTrialCheckoutDto dto)
    {
        try
        {
            // Validar establishment
            var establishment = await _context.Establishments.FindAsync(dto.EstablishmentId);
            if (establishment == null)
                return BadRequest(new { message = "Estabelecimento não encontrado" });

            // Validar plano
            var plan = await _context.Set<SubscriptionPlan>()
                .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.IsActive);
            
            if (plan == null)
                return BadRequest(new { message = "Plano não encontrado ou inativo" });

            if (string.IsNullOrEmpty(plan.StripePriceIdMonthly))
                return BadRequest(new { message = "Plano não configurado para pagamentos. Configure o StripePriceId no painel admin." });

            // ═══════════════════════════════════════════════════════════════════
            // BUSCAR API KEY DO BANCO (PaymentGatewayConfig)
            // ═══════════════════════════════════════════════════════════════════
            var stripeConfig = await _context.Set<PaymentGatewayConfig>()
                .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe 
                                       && g.IsActive 
                                       && g.IsDefault);

            if (stripeConfig == null)
            {
                // Fallback: buscar qualquer config ativa do Stripe
                stripeConfig = await _context.Set<PaymentGatewayConfig>()
                    .FirstOrDefaultAsync(g => g.GatewayType == PaymentGatewayType.Stripe && g.IsActive);
            }

            if (stripeConfig == null)
            {
                _logger.LogError("Nenhuma configuração do Stripe encontrada no banco");
                return BadRequest(new { message = "Gateway de pagamento não configurado. Configure no painel admin." });
            }

            // Descriptografar a Secret Key
            var secretKey = _encryption.Decrypt(stripeConfig.SecretKeyEncrypted ?? "");
            
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Secret Key do Stripe está vazia ou não foi possível descriptografar");
                return BadRequest(new { message = "Configuração de pagamento inválida." });
            }

            // Configurar Stripe com a chave do banco
            StripeConfiguration.ApiKey = secretKey;

            // Obter URL base
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Criar sessão do Stripe Checkout
            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = establishment.Email,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = plan.StripePriceIdMonthly,
                        Quantity = 1
                    }
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    // 14 dias de trial - o cartão é coletado mas não cobrado
                    TrialPeriodDays = 14,
                    Metadata = new Dictionary<string, string>
                    {
                        { "establishment_id", dto.EstablishmentId.ToString() },
                        { "plan_id", dto.PlanId.ToString() },
                        { "gateway_config_id", stripeConfig.Id.ToString() }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "establishment_id", dto.EstablishmentId.ToString() },
                    { "plan_id", dto.PlanId.ToString() },
                    { "gateway_config_id", stripeConfig.Id.ToString() }
                },
                SuccessUrl = $"{baseUrl}/signup/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{baseUrl}/signup/select-plan?establishmentId={dto.EstablishmentId}&canceled=true",
                // Configurações adicionais
                AllowPromotionCodes = true,
                BillingAddressCollection = "auto",
                // Mensagem customizada
                CustomText = new SessionCustomTextOptions
                {
                    Submit = new SessionCustomTextSubmitOptions
                    {
                        Message = "Seu cartão será salvo mas você só será cobrado após os 14 dias de teste gratuito. Cancele a qualquer momento sem custo."
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Stripe Checkout Session criada: {SessionId} para establishment {EstablishmentId}", 
                session.Id, dto.EstablishmentId);

            return Ok(new 
            { 
                checkoutUrl = session.Url,
                sessionId = session.Id
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro Stripe ao criar checkout session");
            return BadRequest(new { message = $"Erro no pagamento: {ex.Message}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar checkout session");
            return BadRequest(new { message = "Erro ao processar. Tente novamente." });
        }
    }

    /// <summary>
    /// Passo 5 (opcional): Finaliza após pagamento do Stripe
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteSignupDto dto)
    {
        _logger.LogInformation("Signup completo para establishment {EstablishmentId}", dto.EstablishmentId);

        await _audit.LogAsync(HttpContext, "SIGNUP_COMPLETED", "Establishment", dto.EstablishmentId.ToString());

        return Ok(new { message = "Cadastro finalizado com sucesso" });
    }

    /// <summary>
    /// Reenvia o código de verificação
    /// </summary>
    [EnableRateLimiting("resend-code")]
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto dto)
    {
        var (success, message) = await _signupService.ResendCodeAsync(dto.WhatsApp);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}

public class ResendCodeDto
{
    public string WhatsApp { get; set; } = string.Empty;
}

public class CreateTrialCheckoutDto
{
    public Guid EstablishmentId { get; set; }
    public Guid PlanId { get; set; }
}
