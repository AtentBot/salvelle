using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Data;
using DTOs;
using DTOs.Mobile;
using Models;
using Service;
using Service.Marketplace;
using Isopoh.Cryptography.Argon2;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly IEmailService _email;
    private readonly ILogger<MobileAuthController> _logger;

    public MobileAuthController(AppDbContext db, JwtTokenService jwt, IEmailService email, ILogger<MobileAuthController> logger)
    {
        _db = db;
        _jwt = jwt;
        _email = email;
        _logger = logger;
    }

    /// <summary>
    /// Registro de novo cliente via app mobile
    /// </summary>
    [HttpPost("register"), EnableRateLimiting("signup")]
    public async Task<ActionResult<MobileAuthResponse>> Register([FromBody] MobileRegisterRequest request)
    {
        // Verificar email duplicado
        var existingAuth = await _db.CustomerAuths
            .AnyAsync(a => a.Phone == request.Email || a.Email == request.Email);

        var existingCustomer = await _db.Customers
            .AnyAsync(c => c.Email == request.Email);

        if (existingAuth || existingCustomer)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Este email já está cadastrado"
            });
        }

        // Criar customer (sem EstablishmentId — cliente marketplace é global)
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Cpf = request.Cpf,
            LoginProvider = "EMAIL",
            Status = "ATIVO",
            ConsentDataProcessing = true,
            ConsentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Gerar OTP de 6 dígitos
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        // Criar auth com OTP pendente
        var passwordHash = Argon2.Hash(request.Password);
        var auth = new CustomerAuth
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Phone = request.Phone ?? request.Email,
            Email = request.Email,
            Cpf = request.Cpf,
            PasswordHash = passwordHash,
            PasswordAlgorithm = "argon2id-v1",
            IsVerified = false,
            VerificationCode = HashOtp(otpCode, request.Email),
            VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15),
            VerificationAttempts = 0,
            LastVerificationSentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        _db.CustomerAuths.Add(auth);
        await _db.SaveChangesAsync();

        // Enviar email com OTP (fire-and-forget — não bloqueia o registro)
        _ = _email.SendEmailAsync(
            request.Email,
            request.FullName,
            "Confirme seu cadastro no Salvelle",
            $"""
            <h2>Bem-vindo ao Salvelle, {System.Net.WebUtility.HtmlEncode(request.FullName)}!</h2>
            <p>Seu código de verificação é:</p>
            <h1 style="letter-spacing:8px;font-size:40px;color:#4F46E5">{otpCode}</h1>
            <p>O código expira em <strong>15 minutos</strong>.</p>
            <p>Se você não criou esta conta, ignore este email.</p>
            """
        );

        _logger.LogInformation("Novo cliente marketplace registrado: {Email}", request.Email);

        return Ok(new MobileAuthResponse
        {
            Success = true,
            Message = "Conta criada! Verifique seu email para confirmar o cadastro.",
            RequiresEmailVerification = true,
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                LoginProvider = "EMAIL"
            }
        });
    }

    /// <summary>
    /// Confirmar email com código OTP enviado no registro
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult<MobileAuthResponse>> VerifyEmail([FromBody] MobileVerifyEmailRequest request)
    {
        var auth = await _db.CustomerAuths
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Email == request.Email);

        if (auth == null)
            return Ok(new MobileAuthResponse { Success = false, Message = "Email não encontrado." });

        if (auth.IsVerified)
            return Ok(new MobileAuthResponse { Success = false, Message = "Email já verificado. Faça login." });

        // Incrementar tentativas antes de validar (evita timing attack)
        auth.VerificationAttempts += 1;

        if (auth.VerificationAttempts > 5)
        {
            await _db.SaveChangesAsync();
            return Ok(new MobileAuthResponse { Success = false, Message = "Muitas tentativas. Solicite um novo código." });
        }

        if (string.IsNullOrEmpty(auth.VerificationCode) || auth.VerificationCode != HashOtp(request.Code, request.Email))
        {
            await _db.SaveChangesAsync();
            return Ok(new MobileAuthResponse { Success = false, Message = "Código inválido." });
        }

        if (auth.VerificationCodeExpiresAt < DateTime.UtcNow)
        {
            await _db.SaveChangesAsync();
            return Ok(new MobileAuthResponse { Success = false, Message = "Código expirado. Solicite um novo." });
        }

        // Verificação bem-sucedida
        auth.IsVerified = true;
        auth.VerificationCode = null;
        auth.VerificationCodeExpiresAt = null;
        auth.VerificationAttempts = 0;
        auth.UpdatedAt = DateTime.UtcNow;

        var customer = auth.Customer!;
        var accessToken = _jwt.GenerateAccessToken(customer.Id, customer.Email ?? "", customer.FullName);
        var refreshToken = _jwt.GenerateRefreshToken();

        var session = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = auth.Id,
            CustomerId = customer.Id,
            SessionToken = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Email verificado com sucesso: {Email}", request.Email);

        return Ok(new MobileAuthResponse
        {
            Success = true,
            Message = "Email verificado! Bem-vindo ao Salvelle.",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                LoginProvider = customer.LoginProvider
            }
        });
    }

    /// <summary>
    /// Reenviar código OTP de verificação de email
    /// </summary>
    [HttpPost("resend-verification"), EnableRateLimiting("resend-code")]
    public async Task<ActionResult> ResendVerification([FromBody] MobileResendVerificationRequest request)
    {
        var auth = await _db.CustomerAuths
            .FirstOrDefaultAsync(a => a.Email == request.Email);

        // Resposta genérica — não revelar se o email existe
        if (auth == null || auth.IsVerified)
            return Ok(new { success = true, message = "Se o email existir e não estiver verificado, um novo código foi enviado." });

        // Anti-flood: mínimo 60s entre reenvios
        if (auth.LastVerificationSentAt.HasValue &&
            (DateTime.UtcNow - auth.LastVerificationSentAt.Value).TotalSeconds < 60)
            return Ok(new { success = false, message = "Aguarde antes de solicitar um novo código." });

        var newCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        auth.VerificationCode = HashOtp(newCode, request.Email);
        auth.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
        auth.VerificationAttempts = 0;
        auth.LastVerificationSentAt = DateTime.UtcNow;
        auth.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var customer = await _db.Customers.FindAsync(auth.CustomerId);
        _ = _email.SendEmailAsync(
            request.Email,
            customer?.FullName ?? "Cliente",
            "Novo código de verificação — Salvelle",
            $"""
            <p>Seu novo código de verificação é:</p>
            <h1 style="letter-spacing:8px;font-size:40px;color:#4F46E5">{newCode}</h1>
            <p>Expira em <strong>15 minutos</strong>.</p>
            """
        );

        _logger.LogInformation("Código de verificação reenviado para: {Email}", request.Email);
        return Ok(new { success = true, message = "Se o email existir e não estiver verificado, um novo código foi enviado." });
    }

    /// <summary>
    /// Login de cliente via email/senha
    /// </summary>
    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<ActionResult<MobileAuthResponse>> Login([FromBody] MobileLoginRequest request)
    {
        // Rejeitar strings com bytes de controle (null bytes, etc.) que causariam erro no PostgreSQL
        if (request.Email.Any(c => c < 32))
            return Ok(new MobileAuthResponse { Success = false, Message = "Email ou senha incorretos" });

        var auth = await _db.CustomerAuths
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Email == request.Email || a.Phone == request.Email);

        if (auth == null)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Email ou senha incorretos"
            });
        }

        if (auth.LockoutEnd.HasValue && auth.LockoutEnd > DateTime.UtcNow)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Conta temporariamente bloqueada. Tente novamente mais tarde."
            });
        }

        if (!Argon2.Verify(auth.PasswordHash, request.Password))
        {
            auth.FailedLoginAttempts += 1;
            if (auth.FailedLoginAttempts >= 5)
            {
                auth.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                auth.FailedLoginAttempts = 0;
            }
            await _db.SaveChangesAsync();

            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Email ou senha incorretos"
            });
        }

        // Reset failed attempts
        auth.FailedLoginAttempts = 0;
        auth.LockoutEnd = null;
        auth.LastLoginAt = DateTime.UtcNow;

        var customer = auth.Customer!;

        // Email não verificado — não emite tokens, informa o cliente
        if (!auth.IsVerified)
        {
            // Reenviar OTP se o anterior expirou
            if (auth.VerificationCodeExpiresAt == null || auth.VerificationCodeExpiresAt < DateTime.UtcNow)
            {
                var newOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                auth.VerificationCode = HashOtp(newOtp, customer.Email ?? "");
                auth.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
                auth.VerificationAttempts = 0;
                auth.LastVerificationSentAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _ = _email.SendEmailAsync(customer.Email ?? "", customer.FullName,
                    "Confirme seu cadastro no Salvelle",
                    $"""
                    <p>Seu código de verificação é:</p>
                    <h1 style="letter-spacing:8px;font-size:40px;color:#4F46E5">{newOtp}</h1>
                    <p>Expira em <strong>15 minutos</strong>.</p>
                    """
                );
            }
            else
            {
                await _db.SaveChangesAsync();
            }

            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Email não verificado. Verifique seu email para o código de confirmação.",
                RequiresEmailVerification = true,
                Customer = new MobileCustomerProfile { Id = customer.Id, Email = customer.Email, FullName = customer.FullName }
            });
        }

        var accessToken = _jwt.GenerateAccessToken(customer.Id, request.Email, customer.FullName);
        var refreshToken = _jwt.GenerateRefreshToken();

        var session = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = auth.Id,
            CustomerId = customer.Id,
            SessionToken = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(new MobileAuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                ProfileImageUrl = customer.ProfileImageUrl,
                LoginProvider = customer.LoginProvider
            }
        });
    }

    /// <summary>
    /// Renovar access token usando refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<MobileAuthResponse>> RefreshToken([FromBody] MobileRefreshTokenRequest request)
    {
        var session = await _db.CustomerSessions
            .Include(s => s.CustomerAuth)
                .ThenInclude(a => a!.Customer)
            .FirstOrDefaultAsync(s => s.SessionToken == request.RefreshToken
                                     && s.IsActive
                                     && s.ExpiresAt > DateTime.UtcNow);

        if (session?.CustomerAuth?.Customer == null)
        {
            return Ok(new MobileAuthResponse
            {
                Success = false,
                Message = "Token inválido ou expirado"
            });
        }

        var customer = session.CustomerAuth.Customer;
        var accessToken = _jwt.GenerateAccessToken(customer.Id, customer.Email ?? "", customer.FullName);

        // Rotacionar refresh token
        session.IsActive = false;
        var newRefreshToken = _jwt.GenerateRefreshToken();

        var newSession = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = session.CustomerAuthId,
            SessionToken = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _db.CustomerSessions.Add(newSession);
        await _db.SaveChangesAsync();

        return Ok(new MobileAuthResponse
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Customer = new MobileCustomerProfile
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                ProfileImageUrl = customer.ProfileImageUrl,
                LoginProvider = customer.LoginProvider
            }
        });
    }

    /// <summary>
    /// Logout — revoga o JWT atual e invalida refresh tokens
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        var customerId = GetCustomerId();
        var jti = HttpContext.Items["MobileJti"] as string;
        var principal = HttpContext.Items["MobileCustomerPrincipal"] as System.Security.Claims.ClaimsPrincipal;

        if (customerId != null && !string.IsNullOrEmpty(jti))
        {
            // Calcular expiração do JWT a partir do claim 'exp'
            var expClaim = principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;
            var expiresAt = expClaim != null && long.TryParse(expClaim, out var exp)
                ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
                : DateTime.UtcNow.AddMinutes(15);

            // Revogar JWT atual
            _db.RevokedJwts.Add(new RevokedJwt
            {
                JwtId = jti,
                ExpiresAt = expiresAt,
                CustomerId = customerId.Value
            });

            // Invalidar todos os refresh tokens ativos
            var activeSessions = await _db.CustomerSessions
                .Where(s => s.CustomerId == customerId.Value && s.IsActive)
                .ToListAsync();
            foreach (var s in activeSessions)
                s.IsActive = false;

            await _db.SaveChangesAsync();
        }

        return Ok(ApiResponse.SuccessResponse("Logout realizado com sucesso."));
    }

    /// <summary>
    /// Perfil do cliente autenticado
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<MobileCustomerProfile>>> GetProfile()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var customer = await _db.Customers.FindAsync(customerId.Value);
        if (customer == null)
            return NotFound(ApiResponse.ErrorResponse("Cliente não encontrado"));

        return Ok(ApiResponse<MobileCustomerProfile>.SuccessResponse(new MobileCustomerProfile
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            ProfileImageUrl = customer.ProfileImageUrl,
            LoginProvider = customer.LoginProvider
        }));
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }

    // SHA-256 hash do código OTP com o email como sal — impede rainbow tables e exposição direta em caso de dump do banco.
    private static string HashOtp(string code, string email)
    {
        var input = System.Text.Encoding.UTF8.GetBytes($"{email.ToLowerInvariant()}:{code}");
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }
}
