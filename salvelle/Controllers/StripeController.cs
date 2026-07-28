using Microsoft.AspNetCore.Mvc;
using DTOs;
using Service;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly StripeService _stripeService;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeController> _logger;

    public StripeController(
        StripeService stripeService,
        IConfiguration config,
        ILogger<StripeController> logger)
    {
        _stripeService = stripeService;
        _config = config;
        _logger = logger;
    }

    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutSessionDto dto)
    {
        try
        {
            var session = await _stripeService.CreateCheckoutSessionAsync(
                dto.EstablishmentId,
                dto.PlanId,
                dto.BillingCycle,
                dto.SuccessUrl,
                dto.CancelUrl
            );

            var publishableKey = _config["Stripe:PublishableKey"];

            return Ok(new CheckoutSessionResponseDto
            {
                SessionId = session.Id,
                PublishableKey = publishableKey ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar checkout session");
            return BadRequest(new { message = "Erro ao criar sessão de pagamento" });
        }
    }

    [HttpPost("create-portal-session")]
    public async Task<IActionResult> CreatePortalSession([FromBody] CreatePortalSessionDto dto)
    {
        try
        {
            var session = await _stripeService.CreatePortalSessionAsync(
                dto.EstablishmentId,
                dto.ReturnUrl
            );

            return Ok(new PortalSessionResponseDto
            {
                Url = session.Url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar portal session");
            return BadRequest(new { message = "Erro ao criar sessão do portal" });
        }
    }

    // Webhook removido - usar /api/webhooks/stripe (StripeWebhookController) como unico endpoint
    // Configure no Stripe Dashboard: https://seudominio.com/api/webhooks/stripe

    [HttpGet("success")]
    public IActionResult Success([FromQuery] string session_id)
    {
        // Sanitizar session_id para prevenir open redirect/injection
        var safeSessionId = System.Text.RegularExpressions.Regex.IsMatch(session_id ?? "", @"^cs_[a-zA-Z0-9_]+$")
            ? session_id
            : "";
        return Redirect("/signup/complete?session=" + Uri.EscapeDataString(safeSessionId ?? ""));
    }

    [HttpGet("cancel")]
    public IActionResult Cancel()
    {
        return Redirect("/signup/payment?canceled=true");
    }
}
