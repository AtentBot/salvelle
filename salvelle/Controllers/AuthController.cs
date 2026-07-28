using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Auth;
using Service.Auth;
using Validators.Auth;
using Service;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly AuditService _audit;

    public AuthController(AppDbContext context, AuthService authService, ILogger<AuthController> logger, AuditService audit)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
        _audit = audit;
    }

    // ==================== LOGIN ====================
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] EmployeeLoginDto dto)
    {
        try
        {
            var validator = new EmployeeLoginValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.LoginAsync(dto, ipAddress, userAgent);

            if (!result.Success)
            {
                _logger.LogWarning("Login falhou para identificador: {Identifier}", dto.Identifier);
                return Unauthorized(result);
            }

            // Se requer 2FA, criar sessão pendente com token opaco (não expor identifier ao cliente)
            if (result.Requires2FA)
            {
                _logger.LogInformation("Login requer 2FA para: {Identifier}", dto.Identifier);

                var employee2fa = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

                if (employee2fa == null)
                    return Unauthorized(new { message = "Funcionário não encontrado" });

                var tempToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
                var pending = new Models.PendingTwoFactorSession
                {
                    TempToken = tempToken,
                    EmployeeId = employee2fa.Id,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.PendingTwoFactorSessions.Add(pending);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    result.Success,
                    result.Message,
                    result.Requires2FA,
                    tempToken // token opaco — NÃO inclui identifier
                });
            }

            // Login bem-sucedido sem 2FA
            Response.Cookies.Append("SessionId", result.SessionId!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            _logger.LogInformation("Login bem-sucedido: {EmployeeName} (ID: {EmployeeId})",
                result.Employee?.Name, result.Employee?.Id);

            if (result.Employee?.Id != null)
                await _audit.LogAsync(HttpContext, "EMPLOYEE_LOGIN", "Employee", result.Employee.Id.ToString());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar login");
            return StatusCode(500, new { message = "Erro interno ao processar login" });
        }
    }

    // ==================== VERIFICAR 2FA ====================
    [HttpPost("verify-2fa")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
    {
        try
        {
            var validator = new Verify2FAValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            if (string.IsNullOrWhiteSpace(dto.TempToken))
                return BadRequest(new { message = "TempToken não fornecido. Realize o login novamente." });

            var pending = await _context.PendingTwoFactorSessions
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.TempToken == dto.TempToken);

            if (pending == null || pending.IsUsed)
                return Unauthorized(new { message = "Token de 2FA inválido." });

            if (pending.ExpiresAt < DateTime.UtcNow)
                return Unauthorized(new { message = "Token de 2FA expirado. Realize o login novamente." });

            var employee = pending.Employee;
            if (employee == null)
                return Unauthorized(new { message = "Funcionário não encontrado." });

            // Marca o temp token como usado para evitar replay attack
            pending.IsUsed = true;
            await _context.SaveChangesAsync();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var (success, message, sessionId) = await _authService.Verify2FAAsync(
                employee.Id, dto, ipAddress, userAgent);

            if (!success)
            {
                _logger.LogWarning("Verificação 2FA falhou: TempToken={TempToken}", dto.TempToken);
                return BadRequest(new { message });
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                Response.Cookies.Append("SessionId", sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                });

                var employeeData = await _context.Employees
                    .Include(e => e.JobPosition)
                    .Include(e => e.Establishment)
                    .FirstOrDefaultAsync(e => e.Id == employee.Id);

                _logger.LogInformation("2FA verificado com sucesso: {EmployeeName} (ID: {EmployeeId})",
                    employeeData?.FullName, employeeData?.Id);

                return Ok(new LoginResponseDto
                {
                    Success = true,
                    Message = message,
                    SessionId = sessionId,
                    Employee = employeeData != null ? new EmployeeInfoDto
                    {
                        Id = employeeData.Id,
                        Name = employeeData.FullName,
                        Cpf = employeeData.Cpf,
                        WhatsApp = employeeData.WhatsApp,
                        Email = employeeData.Email,
                        JobPositionName = employeeData.JobPosition?.Name ?? "",
                        EstablishmentName = employeeData.Establishment?.NomeFantasia ?? ""
                    } : null
                });
            }

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar 2FA");
            return StatusCode(500, new { message = "Erro interno ao verificar código 2FA" });
        }
    }

    // ==================== SOLICITAR 2FA ====================
    [HttpPost("request-2fa")]
    public async Task<IActionResult> Request2FA([FromBody] Request2FADto dto)
    {
        try
        {
            var employeeId = await GetEmployeeIdAsync();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var (success, message) = await _authService.Create2FACodeAsync(
                employeeId.Value, dto.Purpose, ipAddress, userAgent);

            if (!success)
                return BadRequest(new { message });

            _logger.LogInformation("Código 2FA solicitado: Employee ID {EmployeeId}, Purpose: {Purpose}",
                employeeId.Value, dto.Purpose);

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar 2FA");
            return StatusCode(500, new { message = "Erro interno ao solicitar código 2FA" });
        }
    }

    // ==================== RECUPERAÇÃO DE SENHA - PASSO 1: SOLICITAR CÓDIGO ====================
    [EnableRateLimiting("password-reset")]
    [HttpPost("password/request-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
    {
        try
        {
            var validator = new RequestPasswordResetValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            _logger.LogInformation("Solicitação de recuperação de senha para: {Identifier}", dto.Identifier);

            var (success, message) = await _authService.RequestPasswordResetAsync(dto);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar recuperação de senha");
            return StatusCode(500, new { message = "Erro interno ao processar solicitação" });
        }
    }

    // ==================== RECUPERAÇÃO DE SENHA - PASSO 2: VERIFICAR CÓDIGO (OPCIONAL) ====================
    [EnableRateLimiting("password-reset")]
    [HttpPost("password/verify-code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
    {
        try
        {
            var validator = new VerifyResetCodeValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var (success, message) = await _authService.VerifyResetCodeAsync(dto);

            if (!success)
                return BadRequest(new { message });

            _logger.LogInformation("Código de recuperação verificado para: {Identifier}", dto.Identifier);

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar código de recuperação");
            return StatusCode(500, new { message = "Erro interno ao verificar código" });
        }
    }

    // ==================== RECUPERAÇÃO DE SENHA - PASSO 3: REDEFINIR SENHA ====================
    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            var validator = new ResetPasswordValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            _logger.LogInformation("Redefinindo senha para: {Identifier}", dto.Identifier);

            var (success, message) = await _authService.ResetPasswordAsync(dto);

            if (!success)
                return BadRequest(new { message });

            _logger.LogInformation("Senha redefinida com sucesso para: {Identifier}", dto.Identifier);

            await _audit.LogAsync(HttpContext, "PASSWORD_RESET", "Employee", null, "Password reset completed");

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao redefinir senha");
            return StatusCode(500, new { message = "Erro interno ao redefinir senha" });
        }
    }

    // ==================== TROCAR SENHA (USUÁRIO LOGADO) ====================
    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var validator = new ChangePasswordValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

            var employeeId = await GetEmployeeIdAsync();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            _logger.LogInformation("Troca de senha solicitada: Employee ID {EmployeeId}", employeeId.Value);

            var result = await _authService.ChangePasswordAsync(employeeId.Value, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            // Limpar sessão após troca de senha
            Response.Cookies.Delete("SessionId");

            _logger.LogInformation("Senha alterada com sucesso: Employee ID {EmployeeId}", employeeId.Value);

            return Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao trocar senha");
            return StatusCode(500, new { message = "Erro interno ao trocar senha" });
        }
    }

    // ==================== LOGOUT ====================
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = Request.Cookies["SessionId"] ?? Request.Headers["X-Session-Token"].FirstOrDefault();

            if (!string.IsNullOrEmpty(sessionToken))
            {
                var session = await _context.EmployeeSessions
                    .FirstOrDefaultAsync(s => s.Token == sessionToken);

                if (session != null)
                {
                    session.IsActive = false;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Logout realizado: Session ID {SessionId}, Employee ID {EmployeeId}",
                        session.Id, session.EmployeeId);
                }
            }

            Response.Cookies.Delete("SessionId");
            return Ok(new { message = "Logout realizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer logout");
            return StatusCode(500, new { message = "Erro interno ao fazer logout" });
        }
    }

    // ==================== OBTER DADOS DO USUÁRIO ATUAL ====================
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentEmployee()
    {
        try
        {
            var employeeId = await GetEmployeeIdAsync();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var employee = await _context.Employees
                .Include(e => e.JobPosition)
                .Include(e => e.Establishment)
                .FirstOrDefaultAsync(e => e.Id == employeeId.Value);

            if (employee == null)
                return NotFound(new { message = "Funcionário não encontrado" });

            return Ok(new EmployeeInfoDto
            {
                Id = employee.Id,
                Name = employee.FullName,
                Cpf = employee.Cpf,
                WhatsApp = employee.WhatsApp,
                Email = employee.Email,
                JobPositionName = employee.JobPosition?.Name ?? "",
                EstablishmentName = employee.Establishment?.NomeFantasia ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados do funcionário");
            return StatusCode(500, new { message = "Erro interno ao buscar dados" });
        }
    }

    // ==================== HELPER: OBTER ID DO FUNCIONÁRIO DA SESSÃO (ASYNC) ====================
    private async Task<Guid?> GetEmployeeIdAsync()
    {
        // ✅ MELHORADO: Aceita token de Cookie OU Header
        var sessionToken = Request.Cookies["SessionId"] ?? Request.Headers["X-Session-Token"].FirstOrDefault();

        if (string.IsNullOrEmpty(sessionToken))
            return null;

        // ✅ CORRIGIDO: Query assíncrona
        var session = await _context.EmployeeSessions
            .FirstOrDefaultAsync(s => s.Token == sessionToken &&
                                     s.ExpiresAt > DateTime.UtcNow &&
                                     s.IsActive);

        return session?.EmployeeId;
    }
}