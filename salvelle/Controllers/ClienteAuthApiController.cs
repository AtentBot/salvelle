using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Service;
using DTOs.Cliente;

namespace Controllers.Api;

[ApiController]
[Route("api/cliente/auth")]
[AllowAnonymous]  // Permite acesso sem autenticação do ASP.NET Core
public class ClienteAuthApiController : ControllerBase
{
    private readonly CustomerAuthService _authService;
    private readonly ILogger<ClienteAuthApiController> _logger;

    public ClienteAuthApiController(
        CustomerAuthService authService,
        ILogger<ClienteAuthApiController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] CustomerLoginDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(dto, ipAddress, userAgent);

        if (result.Success && !string.IsNullOrEmpty(result.SessionToken))
        {
            SetSessionCookie(result.SessionToken);
        }

        return Ok(result);
    }

    [HttpPost("register"), EnableRateLimiting("signup")]
    public async Task<IActionResult> Register([FromBody] CustomerRegisterDto dto)
    {
        if (!dto.ConsentDataProcessing)
        {
            return BadRequest(new { success = false, message = "É necessário aceitar os termos de uso." });
        }

        var result = await _authService.RegisterAsync(dto);
        return Ok(result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyCode([FromBody] CustomerVerifyCodeDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.VerifyCodeAsync(dto, ipAddress, userAgent);

        if (result.Success && !string.IsNullOrEmpty(result.SessionToken))
        {
            SetSessionCookie(result.SessionToken);
        }

        return Ok(result);
    }

    [HttpPost("resend-code"), EnableRateLimiting("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] CustomerResendCodeDto dto)
    {
        var (success, message) = await _authService.ResendCodeAsync(dto.Phone);
        return Ok(new { success, message });
    }

    [HttpPost("request-reset"), EnableRateLimiting("password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] CustomerResetPasswordDto dto)
    {
        var (success, message) = await _authService.RequestPasswordResetAsync(dto.Phone);
        return Ok(new { success, message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] CustomerResetPasswordConfirmDto dto)
    {
        var (success, message) = await _authService.ResetPasswordAsync(dto);
        return Ok(new { success, message });
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] CustomerSetPasswordDto dto)
    {
        var sessionToken = Request.Cookies["CustomerSessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var (success, message) = await _authService.SetPasswordAsync(sessionToken, dto);
        return Ok(new { success, message });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionToken = Request.Cookies["CustomerSessionId"];
        if (!string.IsNullOrEmpty(sessionToken))
        {
            await _authService.LogoutAsync(sessionToken);
            Response.Cookies.Delete("CustomerSessionId");
        }
        return Ok(new { success = true, message = "Logout realizado." });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentCustomer()
    {
        var session = HttpContext.Items["CustomerSession"] as Models.CustomerSession;
        var customer = HttpContext.Items["Customer"] as Models.Customer;

        if (session == null || customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        return Ok(new CustomerSessionInfoDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            Phone = customer.Phone ?? "",
            CurrentEstablishmentId = session.CurrentEstablishmentId,
            CurrentEstablishmentName = session.CurrentEstablishment?.NomeFantasia,
            IsVerified = true
        });
    }

    [HttpPost("select-establishment")]
    public async Task<IActionResult> SelectEstablishment([FromBody] SelectEstablishmentDto dto)
    {
        var sessionToken = Request.Cookies["CustomerSessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var success = await _authService.SetCurrentEstablishmentAsync(sessionToken, dto.EstablishmentId);

        if (!success)
            return BadRequest(new { success = false, message = "Estabelecimento não encontrado ou inativo." });

        return Ok(new { success = true, message = "Estabelecimento selecionado." });
    }

    private void SetSessionCookie(string sessionToken)
    {
        Response.Cookies.Append("CustomerSessionId", sessionToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/"
        });
    }
}