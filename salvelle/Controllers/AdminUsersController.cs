using Data;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Service;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        AppDbContext db,
        IEmailService email,
        IConfiguration config,
        ILogger<AdminUsersController> logger)
    {
        _db = db;
        _email = email;
        _config = config;
        _logger = logger;
    }

    // ── Listar admins ──────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var requester = GetRequester();
        if (requester == null) return Unauthorized();

        var admins = await _db.SaasAdmins
            .OrderBy(a => a.FullName)
            .Select(a => new
            {
                a.Id,
                a.FullName,
                a.Email,
                a.Role,
                a.IsActive,
                a.LastLoginAt,
                a.CreatedAt,
                activeSessions = a.Sessions.Count(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            })
            .ToListAsync();

        return Ok(admins);
    }

    // ── Convidar novo admin ────────────────────────────────────────────────

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteRequest req)
    {
        var requester = GetRequester();
        if (requester == null) return Unauthorized();

        // Só SUPER_ADMIN pode convidar
        if (requester.Role != "SUPER_ADMIN")
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Nome e email são obrigatórios" });

        var email = req.Email.Trim().ToLowerInvariant();

        if (await _db.SaasAdmins.AnyAsync(a => a.Email == email))
            return BadRequest(new { message = "Email já cadastrado" });

        var validRoles = new[] { "SUPER_ADMIN", "ADMIN", "SUPPORT" };
        var role = string.IsNullOrEmpty(req.Role) ? "ADMIN" : req.Role.ToUpper();
        if (!validRoles.Contains(role)) role = "ADMIN";

        // Cria o admin com senha aleatória — ele definirá via link
        var admin = new SaasAdmin
        {
            Id = Guid.NewGuid(),
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = Argon2.Hash(Guid.NewGuid().ToString()),
            PasswordAlgorithm = "argon2id-v1",
            Role = role,
            IsActive = false, // ativo somente após definir senha
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SaasAdmins.Add(admin);

        // Gera token de definição de senha (mesmo mecanismo do forgot-password)
        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        _db.Set<SaasAdminPasswordReset>().Add(new SaasAdminPasswordReset
        {
            Id = Guid.NewGuid(),
            SaasAdminId = admin.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // Envia convite por email
        var baseUrl = _config["App:BaseUrl"] ?? "https://salvelle.atentbot.com";
        var link = $"{baseUrl}/admin/reset-password?token={token}";
        var inviterName = requester.FullName;

        await _email.SendEmailAsync(
            email,
            req.FullName,
            "Convite — Salvelle Admin Panel",
            $@"<div style='font-family:sans-serif;max-width:520px;margin:0 auto'>
                <h2 style='color:#1a3a3a'>Você foi convidado!</h2>
                <p>Olá <strong>{System.Web.HttpUtility.HtmlEncode(req.FullName)}</strong>,</p>
                <p><strong>{System.Web.HttpUtility.HtmlEncode(inviterName)}</strong> convidou você para acessar o painel admin do <strong>Salvelle</strong> como <strong>{RoleDisplay(role)}</strong>.</p>
                <p>Clique no botão abaixo para definir sua senha e ativar sua conta:</p>
                <div style='text-align:center;margin:32px 0'>
                    <a href='{link}' style='background:#1a3a3a;color:#fff;padding:14px 28px;border-radius:8px;text-decoration:none;font-weight:600'>
                        Definir senha e acessar
                    </a>
                </div>
                <p style='color:#666;font-size:13px'>Este link expira em 3 dias. Se não solicitou, ignore este email.</p>
            </div>"
        );

        _logger.LogInformation("Convite enviado para {Email} por {Inviter}", email, inviterName);
        return Ok(new { message = $"Convite enviado para {email}" });
    }

    // ── Alterar papel ──────────────────────────────────────────────────────

    [HttpPost("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] RoleRequest req)
    {
        var requester = GetRequester();
        if (requester == null) return Unauthorized();
        if (requester.Role != "SUPER_ADMIN") return Forbid();

        var admin = await _db.SaasAdmins.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin não encontrado" });

        // Não pode alterar o próprio papel
        if (admin.Id == requester.Id)
            return BadRequest(new { message = "Não é possível alterar seu próprio papel" });

        var validRoles = new[] { "SUPER_ADMIN", "ADMIN", "SUPPORT" };
        var role = req.Role?.ToUpper() ?? "";
        if (!validRoles.Contains(role))
            return BadRequest(new { message = "Papel inválido" });

        admin.Role = role;
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Papel atualizado" });
    }

    // ── Ativar / desativar ─────────────────────────────────────────────────

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var requester = GetRequester();
        if (requester == null) return Unauthorized();
        if (requester.Role != "SUPER_ADMIN") return Forbid();

        var admin = await _db.SaasAdmins.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin não encontrado" });

        if (admin.Id == requester.Id)
            return BadRequest(new { message = "Não é possível desativar sua própria conta" });

        admin.IsActive = !admin.IsActive;
        admin.UpdatedAt = DateTime.UtcNow;

        // Ao desativar, revoga todas as sessões
        if (!admin.IsActive)
        {
            var sessions = await _db.Set<SaasAdminSession>()
                .Where(s => s.SaasAdminId == id && s.IsActive)
                .ToListAsync();
            foreach (var s in sessions) s.IsActive = false;
        }

        await _db.SaveChangesAsync();

        return Ok(new { isActive = admin.IsActive, message = admin.IsActive ? "Admin ativado" : "Admin desativado e sessões revogadas" });
    }

    // ── Revogar sessões ────────────────────────────────────────────────────

    [HttpPost("{id:guid}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessions(Guid id)
    {
        var requester = GetRequester();
        if (requester == null) return Unauthorized();
        if (requester.Role != "SUPER_ADMIN") return Forbid();

        var sessions = await _db.Set<SaasAdminSession>()
            .Where(s => s.SaasAdminId == id && s.IsActive)
            .ToListAsync();

        foreach (var s in sessions) s.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"{sessions.Count} sessão(ões) revogada(s)" });
    }

    // ── Reenviar convite ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/resend-invite")]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        var requester = GetRequester();
        if (requester == null) return Unauthorized();
        if (requester.Role != "SUPER_ADMIN") return Forbid();

        var admin = await _db.SaasAdmins.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin não encontrado" });
        if (admin.IsActive) return BadRequest(new { message = "Admin já está ativo" });

        // Invalida tokens anteriores
        var oldTokens = await _db.Set<SaasAdminPasswordReset>()
            .Where(r => r.SaasAdminId == id && !r.IsUsed)
            .ToListAsync();
        foreach (var t in oldTokens) t.IsUsed = true;

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        _db.Set<SaasAdminPasswordReset>().Add(new SaasAdminPasswordReset
        {
            Id = Guid.NewGuid(),
            SaasAdminId = admin.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var baseUrl = _config["App:BaseUrl"] ?? "https://salvelle.atentbot.com";
        var link = $"{baseUrl}/admin/reset-password?token={token}";

        await _email.SendEmailAsync(
            admin.Email,
            admin.FullName,
            "Convite reenviado — Salvelle Admin Panel",
            $@"<div style='font-family:sans-serif;max-width:520px;margin:0 auto'>
                <h2 style='color:#1a3a3a'>Novo link de acesso</h2>
                <p>Olá <strong>{System.Web.HttpUtility.HtmlEncode(admin.FullName)}</strong>,</p>
                <p>Um novo link foi gerado para você acessar o painel admin do <strong>Salvelle</strong>.</p>
                <div style='text-align:center;margin:32px 0'>
                    <a href='{link}' style='background:#1a3a3a;color:#fff;padding:14px 28px;border-radius:8px;text-decoration:none;font-weight:600'>
                        Definir senha e acessar
                    </a>
                </div>
                <p style='color:#666;font-size:13px'>Este link expira em 3 dias.</p>
            </div>"
        );

        return Ok(new { message = $"Convite reenviado para {admin.Email}" });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private SaasAdmin? GetRequester()
    {
        return HttpContext.Items["SaasAdmin"] as SaasAdmin;
    }

    private static string RoleDisplay(string role) => role switch
    {
        "SUPER_ADMIN" => "Super Admin",
        "ADMIN" => "Administrador",
        "SUPPORT" => "Suporte",
        _ => role
    };

    public record InviteRequest(string FullName, string Email, string? Role);
    public record RoleRequest(string Role);
}
