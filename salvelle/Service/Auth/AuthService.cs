using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Data;
using DTOs.Auth;
using Models.Auth;
using Models.Employees;
using Service.Notifications;
using System.Security.Cryptography;
using Isopoh.Cryptography.Argon2;

namespace Service.Auth;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, WhatsAppService whatsAppService, ILogger<AuthService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(EmployeeLoginDto dto, string ipAddress, string? userAgent)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .Include(e => e.Establishment)
            .FirstOrDefaultAsync(e =>
                (e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier) &&
                e.Status.ToUpper() == "ATIVO");

        var attempt = new LoginAttempt
        {
            Identifier = dto.Identifier,
            EmployeeId = employee?.Id,
            Success = false,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AttemptedAt = DateTime.UtcNow
        };

        if (employee == null)
        {
            attempt.FailureReason = "Funcionário não encontrado ou inativo";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "CPF/WhatsApp ou senha inválidos"
            };
        }

        if (employee.Establishment?.IsActive != true)
        {
            attempt.FailureReason = "Estabelecimento inativo";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "Estabelecimento inativo"
            };
        }

        // Verificar lockout da conta
        if (employee.LockedUntil.HasValue && employee.LockedUntil > DateTime.UtcNow)
        {
            attempt.FailureReason = "Conta bloqueada";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "Conta bloqueada temporariamente por excesso de tentativas. Tente novamente mais tarde."
            };
        }

        if (!VerifyPassword(dto.Password, employee.PasswordHash))
        {
            employee.FailedLoginAttempts++;
            if (employee.FailedLoginAttempts >= 5)
            {
                employee.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                employee.FailedLoginAttempts = 0;
                _logger.LogWarning("Conta de funcionário bloqueada por 30 min: {Identifier}", dto.Identifier);
            }

            attempt.FailureReason = "Senha incorreta";
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = false,
                Message = "CPF/WhatsApp ou senha inválidos"
            };
        }

        // Reset do lockout ao logar com sucesso
        employee.FailedLoginAttempts = 0;
        employee.LockedUntil = null;

        var requires2FA = await Check2FARequired(employee.Id);

        if (requires2FA)
        {
            var (success2FA, _) = await Create2FACodeAsync(employee.Id, "LOGIN", ipAddress, userAgent);

            if (!success2FA)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Erro ao gerar código 2FA"
                };
            }

            attempt.Success = true;
            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = true,
                Message = "Código de verificação enviado via WhatsApp",
                Requires2FA = true
            };
        }

        var sessionToken = await CreateSessionAsync(employee.Id, dto.RememberMe, ipAddress, userAgent);

        attempt.Success = true;
        _context.LoginAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login realizado com sucesso",
            Requires2FA = false,
            SessionId = sessionToken,
            Employee = new EmployeeInfoDto
            {
                Id = employee.Id,
                Name = employee.FullName,
                Cpf = employee.Cpf,
                WhatsApp = employee.WhatsApp,
                Email = employee.Email,
                JobPositionName = employee.JobPosition?.Name ?? "",
                EstablishmentName = employee.Establishment?.NomeFantasia ?? ""
            }
        };
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(RequestPasswordResetDto dto)
    {
        _logger.LogInformation("RequestPasswordResetAsync iniciado para: {Identifier}", dto.Identifier);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

        if (employee == null)
        {
            _logger.LogDebug("Employee não encontrado para identificador informado");
            await Task.Delay(Random.Shared.Next(1000, 3000));
            return (true, "Se o CPF/WhatsApp existir, você receberá o código de recuperação");
        }

        var recentTokens = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       t.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        if (recentTokens >= 3)
        {
            _logger.LogWarning("Rate limit excedido para employee {EmployeeId}", employee.Id);
            return (false, "Limite de tentativas excedido. Aguarde 1 hora");
        }

        var code = GenerateNumericCode(6);
        var token = GenerateSecureToken();

        var resetToken = new PasswordResetToken
        {
            EmployeeId = employee.Id,
            Token = token,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Type = dto.Method.ToUpper(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            if (dto.Method.ToUpper() == "WHATSAPP" && !string.IsNullOrEmpty(employee.WhatsApp))
            {
                var (whatsappSuccess, whatsappMessage) = await _whatsAppService.SendPasswordResetCodeAsync(
                    employee.WhatsApp,
                    code,
                    employee.FullName
                );

                if (!whatsappSuccess)
                {
                    _logger.LogWarning("Falha ao enviar WhatsApp para employee {EmployeeId}", employee.Id);
                    return (false, $"Erro ao enviar código: {whatsappMessage}");
                }

                _logger.LogInformation("Código de reset enviado via WhatsApp para employee {EmployeeId}", employee.Id);
            }

            return (true, "Código de recuperação enviado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar password reset para employee {EmployeeId}", employee.Id);
            return (false, $"Erro ao processar solicitação: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> VerifyResetCodeAsync(VerifyResetCodeDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

        if (employee == null)
            return (false, "Código inválido ou expirado");

        // Verificar tentativas falhas recentes (mesma proteção de ResetPasswordAsync —
        // sem isso, este endpoint permite brute-force do código sem limite)
        var recentFailedAttempts = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .SumAsync(t => t.Attempts);

        if (recentFailedAttempts >= 5)
        {
            _logger.LogWarning("Muitas tentativas de verificação de código para employee {EmployeeId}", employee.Id);
            return (false, "Muitas tentativas incorretas. Solicite um novo código.");
        }

        var token = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       t.Code == dto.Code &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
        {
            var latestToken = await _context.PasswordResetTokens
                .Where(t => t.EmployeeId == employee.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            if (latestToken != null)
            {
                latestToken.Attempts++;
                await _context.SaveChangesAsync();
            }
            return (false, "Código inválido ou expirado");
        }

        return (true, "Código verificado com sucesso");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Cpf == dto.Identifier || e.WhatsApp == dto.Identifier);

        if (employee == null)
            return (false, "Código inválido ou expirado");

        // Verificar tentativas falhas recentes (brute-force protection)
        var recentFailedAttempts = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .SumAsync(t => t.Attempts);

        if (recentFailedAttempts >= 5)
        {
            _logger.LogWarning("Muitas tentativas de reset para employee {EmployeeId}", employee.Id);
            return (false, "Muitas tentativas incorretas. Solicite um novo código.");
        }

        var token = await _context.PasswordResetTokens
            .Where(t => t.EmployeeId == employee.Id &&
                       t.Code == dto.Code &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
        {
            // Incrementar tentativas no token mais recente
            var latestToken = await _context.PasswordResetTokens
                .Where(t => t.EmployeeId == employee.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            if (latestToken != null)
            {
                latestToken.Attempts++;
                await _context.SaveChangesAsync();
            }
            return (false, "Código inválido ou expirado");
        }

        // ✅ CORRIGIDO: Usar Argon2 em vez de BCrypt
        employee.PasswordHash = HashPassword(dto.NewPassword);
        employee.UpdatedAt = DateTime.UtcNow;

        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;

        await _context.EmployeeSessions
            .Where(s => s.EmployeeId == employee.Id && s.IsActive)
            .ForEachAsync(s => s.IsActive = false);

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(employee.WhatsApp))
        {
            await _whatsAppService.SendPasswordChangedNotificationAsync(employee.WhatsApp, employee.FullName);
        }

        return (true, "Senha alterada com sucesso");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid employeeId, ChangePasswordDto dto)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            return (false, "Funcionário não encontrado");

        if (!VerifyPassword(dto.CurrentPassword, employee.PasswordHash))
            return (false, "Senha atual incorreta");

        // ✅ CORRIGIDO: Usar Argon2
        employee.PasswordHash = HashPassword(dto.NewPassword);
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.EmployeeSessions
            .Where(s => s.EmployeeId == employee.Id && s.IsActive)
            .ForEachAsync(s => s.IsActive = false);

        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(employee.WhatsApp))
        {
            await _whatsAppService.SendPasswordChangedNotificationAsync(employee.WhatsApp, employee.FullName);
        }

        return (true, "Senha alterada com sucesso");
    }

    public async Task<(bool Success, string Message)> Create2FACodeAsync(Guid employeeId, string purpose, string? ipAddress, string? userAgent)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || string.IsNullOrEmpty(employee.WhatsApp))
            return (false, "Funcionário não possui WhatsApp cadastrado");

        var code = GenerateNumericCode(6);

        var token = new TwoFactorToken
        {
            EmployeeId = employeeId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.TwoFactorAuths.Add(token);
        await _context.SaveChangesAsync();

        var (success, message) = await _whatsAppService.Send2FACodeAsync(employee.WhatsApp, code, purpose);

        return success
            ? (true, "Código enviado com sucesso")
            : (false, $"Erro ao enviar código: {message}");
    }

    public async Task<(bool Success, string Message, string? SessionId)> Verify2FAAsync(
        Guid employeeId, Verify2FADto dto, string ipAddress, string? userAgent)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .Include(e => e.Establishment)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            return (false, "Funcionário não encontrado", null);

        var token = await _context.TwoFactorAuths
            .Where(t => t.EmployeeId == employeeId &&
                       t.Code == dto.Code &&
                       t.Purpose == dto.Purpose &&
                       !t.IsUsed &&
                       t.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (token == null)
            return (false, "Código inválido ou expirado", null);

        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (dto.Purpose == "LOGIN")
        {
            var sessionToken = await CreateSessionAsync(employee.Id, false, ipAddress, userAgent);
            return (true, "Autenticação 2FA bem-sucedida", sessionToken);
        }

        return (true, "Código 2FA verificado com sucesso", null);
    }

    private async Task<bool> Check2FARequired(Guid employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
            return false;

        var requiresCodes = new[] { "OWNER", "MANAGER", "PHARMACIST_RT", "PHARMACIST" };
        return requiresCodes.Contains(employee.JobPosition?.Code ?? "");
    }

    private async Task<string> CreateSessionAsync(Guid employeeId, bool rememberMe, string? ipAddress, string? userAgent)
    {
        var sessionToken = GenerateSecureToken();
        var expiresAt = rememberMe
            ? DateTime.UtcNow.AddDays(30)
            : DateTime.UtcNow.AddHours(8);

        var session = new EmployeeSession
        {
            EmployeeId = employeeId,
            Token = sessionToken,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeSessions.Add(session);
        await _context.SaveChangesAsync();

        return sessionToken;
    }

    // ✅ CORRIGIDO: Usar Argon2 em vez de BCrypt
    private string HashPassword(string password)
    {
        return Argon2.Hash(password);
    }

    // ✅ CORRIGIDO: Verificar com Argon2 (com fallback para BCrypt para senhas antigas)
    private bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Tentar Argon2 primeiro (padrão atual)
            return Argon2.Verify(hash, password);
        }
        catch
        {
            // Fallback para BCrypt (para senhas antigas que ainda não foram migradas)
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }

    private string GenerateNumericCode(int length)
    {
        var chars = "0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}