using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs;
using Service;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminAuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly AuditService _audit;

    public AdminAuthController(
        AppDbContext context,
        ILogger<AdminAuthController> logger,
        IConfiguration configuration,
        IEmailService emailService,
        AuditService audit)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _emailService = emailService;
        _audit = audit;
    }

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
    {
        try
        {
            var admin = await _context.Set<SaasAdmin>()
                .FirstOrDefaultAsync(a => a.Email == dto.Email && a.IsActive);

            if (admin == null)
                return Unauthorized(new { message = "Credenciais inválidas" });

            if (!Argon2.Verify(admin.PasswordHash, dto.Password))
                return Unauthorized(new { message = "Credenciais inválidas" });

            // Criar sessão
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var session = new SaasAdminSession
            {
                Id = Guid.NewGuid(),
                SaasAdminId = admin.Id,
                Token = token,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(12),
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<SaasAdminSession>().Add(session);

            admin.LastLoginAt = DateTime.UtcNow;
            admin.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(HttpContext, "ADMIN_LOGIN", "Admin", admin.Id.ToString());

            // Definir cookie
            Response.Cookies.Append("AdminSessionId", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = session.ExpiresAt
            });

            return Ok(new AdminLoginResponseDto
            {
                Token = token,
                ExpiresAt = session.ExpiresAt,
                Admin = new AdminDto
                {
                    Id = admin.Id,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    Role = admin.Role
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer login de admin");
            return StatusCode(500, new { message = "Erro ao processar login" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = Request.Cookies["AdminSessionId"];
            if (!string.IsNullOrEmpty(sessionToken))
            {
                var session = await _context.Set<SaasAdminSession>()
                    .FirstOrDefaultAsync(s => s.Token == sessionToken);

                if (session != null)
                {
                    session.IsActive = false;
                    session.ExpiresAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                Response.Cookies.Delete("AdminSessionId");
            }

            return Ok(new { message = "Logout realizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer logout de admin");
            return StatusCode(500, new { message = "Erro ao processar logout" });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        try
        {
            var sessionToken = Request.Cookies["AdminSessionId"];
            if (string.IsNullOrEmpty(sessionToken))
                return Unauthorized(new { message = "Não autenticado" });

            var session = await _context.Set<SaasAdminSession>()
                .Include(s => s.SaasAdmin)
                .FirstOrDefaultAsync(s => s.Token == sessionToken && 
                                         s.IsActive && 
                                         s.ExpiresAt > DateTime.UtcNow);

            if (session?.SaasAdmin == null)
                return Unauthorized(new { message = "Sessão inválida ou expirada" });

            // Atualizar última atividade
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new AdminDto
            {
                Id = session.SaasAdmin.Id,
                FullName = session.SaasAdmin.FullName,
                Email = session.SaasAdmin.Email,
                Role = session.SaasAdmin.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar admin logado");
            return StatusCode(500, new { message = "Erro ao processar requisição" });
        }
    }

    // ================== RECUPERAÇÃO DE SENHA ==================

    /// <summary>
    /// Solicita recuperação de senha - envia token por email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] AdminForgotPasswordDto dto)
    {
        try
        {
            // Sempre retornar sucesso para não revelar se email existe
            var successMessage = "Se o e-mail estiver cadastrado, você receberá as instruções de recuperação.";

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Ok(new { message = successMessage });

            var admin = await _context.Set<SaasAdmin>()
                .FirstOrDefaultAsync(a => a.Email == dto.Email.Trim().ToLower() && a.IsActive);

            if (admin == null)
            {
                _logger.LogWarning("Tentativa de recuperação de senha para email não encontrado: {Email}", dto.Email?.Length > 5 ? dto.Email[..2] + "***" + dto.Email[dto.Email.IndexOf('@')..] : "***");
                // Delay para evitar timing attack
                await Task.Delay(Random.Shared.Next(500, 1500));
                return Ok(new { message = successMessage });
            }

            // Invalidar tokens anteriores não usados
            var oldTokens = await _context.Set<SaasAdminPasswordReset>()
                .Where(r => r.SaasAdminId == admin.Id && !r.IsUsed && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
            {
                oldToken.IsUsed = true;
                oldToken.UsedAt = DateTime.UtcNow;
            }

            // Gerar novo token seguro
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var passwordReset = new SaasAdminPasswordReset
            {
                Id = Guid.NewGuid(),
                SaasAdminId = admin.Id,
                Token = token,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(1), // Token válido por 1 hora
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<SaasAdminPasswordReset>().Add(passwordReset);
            await _context.SaveChangesAsync();

            // Construir URL de reset
            var baseUrl = _configuration["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var resetUrl = $"{baseUrl}/admin/reset-password?token={token}";

            // Enviar email de recuperação
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                admin.Email, 
                admin.FullName, 
                resetUrl
            );

            if (emailSent)
            {
                _logger.LogInformation(
                    "Email de recuperação enviado para admin {AdminId} ({Email})", 
                    admin.Id, 
                    admin.Email);
            }
            else
            {
                _logger.LogWarning(
                    "Falha ao enviar email de recuperação para admin {AdminId} ({Email})", 
                    admin.Id, 
                    admin.Email);
            }

            return Ok(new { 
                message = successMessage,
                emailSent = emailSent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar recuperação de senha");
            return StatusCode(500, new { message = "Erro ao processar solicitação" });
        }
    }

    /// <summary>
    /// Valida se um token de reset é válido
    /// </summary>
    [HttpGet("validate-reset-token")]
    public async Task<IActionResult> ValidateResetToken([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { valid = false, message = "Token não informado" });

            var resetToken = await _context.Set<SaasAdminPasswordReset>()
                .Include(r => r.SaasAdmin)
                .FirstOrDefaultAsync(r => r.Token == token && 
                                         !r.IsUsed && 
                                         r.ExpiresAt > DateTime.UtcNow);

            if (resetToken?.SaasAdmin == null)
                return Ok(new { valid = false, message = "Token inválido ou expirado" });

            return Ok(new { 
                valid = true, 
                email = MaskEmail(resetToken.SaasAdmin.Email),
                expiresAt = resetToken.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token de reset");
            return StatusCode(500, new { valid = false, message = "Erro ao validar token" });
        }
    }

    /// <summary>
    /// Redefine a senha usando o token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest(new { message = "Token não informado" });

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Nova senha não informada" });

            if (dto.NewPassword.Length < 8)
                return BadRequest(new { message = "A senha deve ter no mínimo 8 caracteres" });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "As senhas não conferem" });

            // Validar complexidade da senha
            if (!IsPasswordStrong(dto.NewPassword))
                return BadRequest(new { message = "A senha deve conter letras maiúsculas, minúsculas, números e caracteres especiais" });

            var resetToken = await _context.Set<SaasAdminPasswordReset>()
                .Include(r => r.SaasAdmin)
                .FirstOrDefaultAsync(r => r.Token == dto.Token && 
                                         !r.IsUsed && 
                                         r.ExpiresAt > DateTime.UtcNow);

            if (resetToken?.SaasAdmin == null)
                return BadRequest(new { message = "Token inválido ou expirado" });

            // Atualizar senha e ativar conta (caso seja primeiro acesso via convite)
            var admin = resetToken.SaasAdmin;
            admin.PasswordHash = Argon2.Hash(dto.NewPassword);
            admin.IsActive = true;
            admin.UpdatedAt = DateTime.UtcNow;

            // Marcar token como usado
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;

            // Invalidar todas as sessões ativas do admin (forçar novo login)
            var activeSessions = await _context.Set<SaasAdminSession>()
                .Where(s => s.SaasAdminId == admin.Id && s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.ExpiresAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Senha do admin {AdminId} redefinida com sucesso", admin.Id);

            // Enviar notificação de alteração de senha
            _ = _emailService.SendPasswordChangedNotificationAsync(admin.Email, admin.FullName);

            return Ok(new { message = "Senha redefinida com sucesso! Faça login com sua nova senha." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao redefinir senha");
            return StatusCode(500, new { message = "Erro ao redefinir senha" });
        }
    }

    // ================== MÉTODOS AUXILIARES ==================

    /// <summary>
    /// Mascara o email para exibição (ex: d***@gmail.com)
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        var maskedName = name.Length <= 2 
            ? name[0] + "***" 
            : name[0] + new string('*', Math.Min(name.Length - 2, 5)) + name[^1];

        return $"{maskedName}@{domain}";
    }

    /// <summary>
    /// Valida se a senha atende aos requisitos de complexidade
    /// </summary>
    private static bool IsPasswordStrong(string password)
    {
        if (password.Length < 8) return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}

// ================== DTOs (com prefixo Admin para evitar conflito) ==================

public class AdminForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

public class AdminResetPasswordDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
