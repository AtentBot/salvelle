using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs.Cliente;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using Service.Notifications;

namespace Service;

public class CustomerAuthService
{
    private readonly AppDbContext _context;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<CustomerAuthService> _logger;
    private const int SESSION_DURATION_DAYS = 30;
    private const int VERIFICATION_CODE_EXPIRY_MINUTES = 10;
    private const int MAX_VERIFICATION_ATTEMPTS = 5;
    private const int MAX_LOGIN_ATTEMPTS = 5;
    private const int LOCKOUT_MINUTES = 30;
    private const int MAX_CODE_GENERATION_RETRIES = 5;

    public CustomerAuthService(
        AppDbContext context,
        WhatsAppService whatsAppService,
        ILogger<CustomerAuthService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    // ==================== LOGIN ====================

    public async Task<CustomerLoginResponseDto> LoginAsync(CustomerLoginDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var phone = NormalizePhone(dto.Phone);

            if (string.IsNullOrEmpty(phone) || phone.Length < 10)
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Telefone inválido"
                };
            }

            if (string.IsNullOrEmpty(dto.Password))
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Senha é obrigatória"
                };
            }

            var auth = await _context.CustomerAuths
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Phone == phone);

            if (auth == null)
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Telefone não cadastrado. Crie sua conta primeiro."
                };
            }

            if (auth.LockoutEnd.HasValue && auth.LockoutEnd > DateTime.UtcNow)
            {
                var minutes = (int)(auth.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = $"Conta bloqueada. Tente novamente em {minutes} minutos."
                };
            }

            if (!auth.IsVerified)
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Conta não verificada. Complete seu cadastro."
                };
            }

            if (string.IsNullOrEmpty(auth.PasswordHash))
            {
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Senha não definida. Use 'Esqueci minha senha' para criar uma."
                };
            }

            if (!VerifyPassword(dto.Password, auth.PasswordHash))
            {
                auth.FailedLoginAttempts++;
                if (auth.FailedLoginAttempts >= MAX_LOGIN_ATTEMPTS)
                {
                    auth.LockoutEnd = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES);
                    auth.FailedLoginAttempts = 0;
                }
                await _context.SaveChangesAsync();

                _logger.LogWarning("Tentativa de login com senha inválida para telefone {Phone}", phone?.Length > 6 ? phone[..4] + "****" + phone[^2..] : "***");
                return new CustomerLoginResponseDto
                {
                    Success = false,
                    Message = "Senha incorreta"
                };
            }

            var session = await CreateSessionAsync(auth, ipAddress, userAgent);

            auth.FailedLoginAttempts = 0;
            auth.LastLoginAt = DateTime.UtcNow;
            auth.LastLoginIp = ipAddress;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login bem-sucedido para cliente {CustomerId}", auth.CustomerId);

            return new CustomerLoginResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso!",
                SessionToken = session.SessionToken,
                Customer = MapToCustomerInfo(auth)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no login do cliente");
            return new CustomerLoginResponseDto
            {
                Success = false,
                Message = "Erro ao processar login. Tente novamente."
            };
        }
    }

    // ==================== REGISTRO ====================

    public async Task<CustomerRegisterResponseDto> RegisterAsync(CustomerRegisterDto dto)
    {
        try
        {
            var phone = NormalizePhone(dto.Phone);
            var cpf = NormalizeCpf(dto.Cpf);

            if (!ValidateCpf(cpf))
            {
                return new CustomerRegisterResponseDto
                {
                    Success = false,
                    Message = "CPF inválido."
                };
            }

            var existingAuth = await _context.CustomerAuths
                .FirstOrDefaultAsync(a => a.Phone == phone || a.Cpf == cpf);

            if (existingAuth != null)
            {
                if (existingAuth.Phone == phone)
                    return new CustomerRegisterResponseDto { Success = false, Message = "Telefone já cadastrado." };
                else
                    return new CustomerRegisterResponseDto { Success = false, Message = "CPF já cadastrado." };
            }

            var defaultEstablishment = await _context.Establishments
                .Where(e => e.IsActive == true)
                .FirstOrDefaultAsync();

            if (defaultEstablishment == null)
            {
                return new CustomerRegisterResponseDto
                {
                    Success = false,
                    Message = "Nenhum estabelecimento disponível."
                };
            }

            // Loop de retry para lidar com race conditions
            for (int attempt = 1; attempt <= MAX_CODE_GENERATION_RETRIES; attempt++)
            {
                try
                {
                    // Gerar código no formato CLI{ano}{sequencial} - mesmo formato do CustomerService
                    var nextCode = await GenerateCustomerCodeAsync(defaultEstablishment.Id);
                    
                    _logger.LogInformation("Tentativa {Attempt}: Gerando código {Code} para establishment {EstId}", 
                        attempt, nextCode, defaultEstablishment.Id);

                    var customerId = Guid.NewGuid();

                    var customer = new Customer
                    {
                        Id = customerId,
                        EstablishmentId = defaultEstablishment.Id,
                        Code = nextCode,
                        FullName = dto.FullName.Trim(),
                        Cpf = cpf,
                        Phone = phone,
                        WhatsApp = phone,
                        Email = dto.Email?.Trim(),
                        BirthDate = !string.IsNullOrEmpty(dto.BirthDate) ? DateTime.Parse(dto.BirthDate) : null,
                        Gender = dto.Gender,
                        ZipCode = dto.ZipCode,
                        Street = dto.Street,
                        Number = dto.Number,
                        Complement = dto.Complement,
                        Neighborhood = dto.Neighborhood,
                        City = dto.City,
                        State = dto.State,
                        ConsentDataProcessing = dto.ConsentDataProcessing,
                        ConsentDate = dto.ConsentDataProcessing ? DateTime.UtcNow : null,
                        Status = "ATIVO",
                        CreatedAt = DateTime.UtcNow,
                        CreatedByEmployeeId = Guid.Empty,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Customers.Add(customer);

                    var auth = new CustomerAuth
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        Phone = phone,
                        Cpf = cpf,
                        IsVerified = false,
                        PasswordAlgorithm = "Argon2id",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CustomerAuths.Add(auth);
                    await _context.SaveChangesAsync();

                    var (codeSent, codeSendMessage) = await SendVerificationCodeAsync(auth);

                    _logger.LogInformation("Cliente cadastrado com sucesso: {CustomerId}, Code: {Code}",
                        customerId, customer.Code);

                    return new CustomerRegisterResponseDto
                    {
                        Success = true,
                        Message = codeSent
                            ? "Cadastro iniciado! Verifique o código enviado por WhatsApp."
                            : "Cadastro criado, mas não conseguimos enviar o código por WhatsApp. Toque em 'Reenviar código' na próxima tela.",
                        CustomerId = customerId,
                        RequiresVerification = true
                    };
                }
                catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
                {
                    _context.ChangeTracker.Clear();

                    if (attempt == MAX_CODE_GENERATION_RETRIES)
                    {
                        _logger.LogError(ex, "Falha após {Attempts} tentativas de gerar código único", MAX_CODE_GENERATION_RETRIES);
                        return new CustomerRegisterResponseDto
                        {
                            Success = false,
                            Message = "Erro ao gerar código do cliente. Por favor, tente novamente."
                        };
                    }

                    _logger.LogWarning("Conflito de código na tentativa {Attempt}, tentando novamente...", attempt);
                    await Task.Delay(100 * attempt + Random.Shared.Next(0, 50));
                }
            }

            return new CustomerRegisterResponseDto
            {
                Success = false,
                Message = "Erro ao processar cadastro. Tente novamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no registro do cliente");
            return new CustomerRegisterResponseDto
            {
                Success = false,
                Message = "Erro ao processar cadastro. Tente novamente."
            };
        }
    }

    /// <summary>
    /// Gera código no formato CLI{ano}{sequencial:5 dígitos}
    /// Exemplo: CLI202400019
    /// Mesmo formato usado pelo CustomerService
    /// </summary>
    private async Task<string> GenerateCustomerCodeAsync(Guid establishmentId)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CLI{year}";

        // Buscar o maior código que começa com o prefixo do ano atual
        var lastCode = await _context.Customers
            .Where(c => c.EstablishmentId == establishmentId && c.Code.StartsWith(prefix))
            .OrderByDescending(c => c.Code)
            .Select(c => c.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastCode) && lastCode.Length > prefix.Length)
        {
            var numberPart = lastCode.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D5}";
    }

    // ==================== VERIFICAÇÃO ====================

    public async Task<CustomerVerifyResponseDto> VerifyCodeAsync(CustomerVerifyCodeDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var phone = NormalizePhone(dto.Phone);

            var auth = await _context.CustomerAuths
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Phone == phone);

            if (auth == null)
            {
                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Telefone não encontrado."
                };
            }

            if (auth.VerificationAttempts >= MAX_VERIFICATION_ATTEMPTS)
            {
                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Muitas tentativas. Solicite um novo código."
                };
            }

            if (auth.VerificationCodeExpiresAt < DateTime.UtcNow)
            {
                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Código expirado. Solicite um novo."
                };
            }

            if (string.IsNullOrEmpty(dto.Code) || auth.VerificationCode != HashOtp(dto.Code, auth.Phone ?? ""))
            {
                auth.VerificationAttempts++;
                await _context.SaveChangesAsync();

                return new CustomerVerifyResponseDto
                {
                    Success = false,
                    Message = "Código incorreto."
                };
            }

            auth.IsVerified = true;
            auth.VerificationCode = null;
            auth.VerificationCodeExpiresAt = null;
            auth.VerificationAttempts = 0;

            var session = await CreateSessionAsync(auth, ipAddress, userAgent);

            auth.LastLoginAt = DateTime.UtcNow;
            auth.LastLoginIp = ipAddress;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cliente {CustomerId} verificado com sucesso", auth.CustomerId);

            return new CustomerVerifyResponseDto
            {
                Success = true,
                Message = "Conta verificada com sucesso!",
                SessionToken = session.SessionToken,
                Customer = MapToCustomerInfo(auth)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na verificação do código");
            return new CustomerVerifyResponseDto
            {
                Success = false,
                Message = "Erro ao verificar código. Tente novamente."
            };
        }
    }

    public async Task<(bool Success, string Message)> ResendCodeAsync(string phone)
    {
        try
        {
            phone = NormalizePhone(phone);

            var auth = await _context.CustomerAuths
                .FirstOrDefaultAsync(a => a.Phone == phone);

            if (auth == null)
                return (false, "Telefone não encontrado.");

            if (auth.LastVerificationSentAt.HasValue &&
                auth.LastVerificationSentAt.Value.AddMinutes(1) > DateTime.UtcNow)
            {
                return (false, "Aguarde 1 minuto para solicitar novo código.");
            }

            var (sent, sendMessage) = await SendVerificationCodeAsync(auth);
            if (!sent) return (false, sendMessage);
            return (true, "Novo código enviado para seu WhatsApp.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reenviar código");
            return (false, "Erro ao enviar código.");
        }
    }

    // ==================== SESSÃO ====================

    public async Task<CustomerSession?> ValidateSessionAsync(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = await _context.CustomerSessions
            .Include(s => s.Customer)
            .Include(s => s.CurrentEstablishment)
            .FirstOrDefaultAsync(s =>
                s.SessionToken == sessionToken &&
                s.IsActive &&
                s.ExpiresAt > DateTime.UtcNow);

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return session;
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        var session = await _context.CustomerSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session == null)
            return false;

        session.IsActive = false;
        session.LogoutAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetCurrentEstablishmentAsync(string sessionToken, Guid establishmentId)
    {
        var session = await _context.CustomerSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session == null)
            return false;

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == establishmentId && e.IsActive == true);

        if (establishment == null)
            return false;

        session.CurrentEstablishmentId = establishmentId;
        await _context.SaveChangesAsync();

        return true;
    }

    // ==================== SENHA ====================

    public async Task<(bool Success, string Message)> SetPasswordAsync(string sessionToken, CustomerSetPasswordDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return (false, "Senhas não conferem.");

        if (dto.Password.Length < 6)
            return (false, "Senha deve ter no mínimo 6 caracteres.");

        var session = await _context.CustomerSessions
            .Include(s => s.CustomerAuth)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session?.CustomerAuth == null)
            return (false, "Sessão inválida.");

        session.CustomerAuth.PasswordHash = HashPassword(dto.Password);
        session.CustomerAuth.PasswordCreatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (true, "Senha definida com sucesso!");
    }

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(string phone)
    {
        phone = NormalizePhone(phone);

        var auth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.Phone == phone);

        if (auth == null)
            return (false, "Telefone não encontrado.");

        // Throttle de envio: impede spam de reset (flood no WhatsApp da vítima e
        // zeragem repetida do contador de tentativas). Igual a ResendCodeAsync.
        if (auth.LastVerificationSentAt.HasValue &&
            auth.LastVerificationSentAt.Value.AddMinutes(1) > DateTime.UtcNow)
        {
            return (false, "Aguarde 1 minuto para solicitar novo código.");
        }

        var (sent, sendMessage) = await SendVerificationCodeAsync(auth);
        if (!sent) return (false, sendMessage);
        return (true, "Código de recuperação enviado para seu WhatsApp.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(CustomerResetPasswordConfirmDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return (false, "Senhas não conferem.");

        if (dto.NewPassword.Length < 6)
            return (false, "Senha deve ter no mínimo 6 caracteres.");

        var phone = NormalizePhone(dto.Phone);

        var auth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.Phone == phone);

        if (auth == null)
            return (false, "Telefone não encontrado.");

        // Rate-limit do OTP: sem o contador abaixo, o código de 6 dígitos (1M
        // combinações) podia ser forçado por bruta neste endpoint sem qualquer
        // bloqueio — takeover de conta via reset de senha. Espelha VerifyCodeAsync.
        if (auth.VerificationAttempts >= MAX_VERIFICATION_ATTEMPTS)
            return (false, "Muitas tentativas. Solicite um novo código.");

        if (auth.VerificationCode == null || auth.VerificationCodeExpiresAt < DateTime.UtcNow)
            return (false, "Código inválido ou expirado.");

        if (string.IsNullOrEmpty(dto.Code) || auth.VerificationCode != HashOtp(dto.Code, auth.Phone ?? ""))
        {
            auth.VerificationAttempts++;
            await _context.SaveChangesAsync();
            return (false, "Código inválido ou expirado.");
        }

        auth.PasswordHash = HashPassword(dto.NewPassword);
        auth.PasswordCreatedAt = DateTime.UtcNow;
        auth.VerificationCode = null;
        auth.VerificationCodeExpiresAt = null;
        auth.VerificationAttempts = 0;
        auth.IsVerified = true;
        await _context.SaveChangesAsync();

        return (true, "Senha redefinida com sucesso!");
    }

    // ==================== HELPERS ====================

    private async Task<(bool Success, string Message)> SendVerificationCodeAsync(CustomerAuth auth)
    {
        var code = GenerateVerificationCode();

        auth.VerificationCode = HashOtp(code, auth.Phone ?? "");
        auth.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(VERIFICATION_CODE_EXPIRY_MINUTES);
        auth.VerificationAttempts = 0;
        auth.LastVerificationSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var maskedPhone = auth.Phone?.Length > 6 ? auth.Phone[..4] + "****" + auth.Phone[^2..] : "***";
        var message = $"🔐 *Salvelle*\n\nSeu código de verificação é: *{code}*\n\nVálido por {VERIFICATION_CODE_EXPIRY_MINUTES} minutos.";
        var (sent, sendMessage) = await _whatsAppService.SendMessageAsync(auth.Phone, message);

        if (!sent)
        {
            _logger.LogError("Falha ao enviar código WhatsApp para {Phone}: {Error}", maskedPhone, sendMessage);
            return (false, "Não foi possível enviar o código pelo WhatsApp. Tente novamente em instantes.");
        }

        _logger.LogInformation("Código de verificação enviado para {Phone}", maskedPhone);
        return (true, "Código enviado.");
    }

    private async Task<CustomerSession> CreateSessionAsync(CustomerAuth auth, string ipAddress, string userAgent)
    {
        // Define establishment padrão da sessão: usa o do cliente cadastrado se houver,
        // ou cai no primeiro estabelecimento ativo do sistema (single-pharmacy setup).
        var customer = auth.Customer ?? await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == auth.CustomerId);

        Guid? defaultEstablishmentId = customer?.EstablishmentId;
        if (defaultEstablishmentId == null || defaultEstablishmentId == Guid.Empty)
        {
            defaultEstablishmentId = await _context.Establishments
                .Where(e => e.IsActive)
                .OrderBy(e => e.CreatedAt)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync();
        }

        var session = new CustomerSession
        {
            Id = Guid.NewGuid(),
            CustomerAuthId = auth.Id,
            CustomerId = auth.CustomerId,
            SessionToken = GenerateSessionToken(),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = DetectDeviceType(userAgent),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(SESSION_DURATION_DAYS),
            IsActive = true,
            CurrentEstablishmentId = defaultEstablishmentId,
        };

        _context.CustomerSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException is Npgsql.PostgresException pgEx && 
               pgEx.SqlState == "23505";
    }

    private static string GenerateVerificationCode() => RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    private static string GenerateSessionToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashPassword(string password) => Argon2.Hash(password);

    /// <summary>
    /// Deriva o hash do código OTP (SHA-256 com salt pelo telefone) para não
    /// armazenar o código em texto puro. O usuário recebe o código em claro;
    /// o banco guarda apenas o hash.
    /// </summary>
    private static string HashOtp(string code, string phone) =>
        Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{phone}:{code}"))
        ).ToLowerInvariant();

    private static bool VerifyPassword(string password, string hash) => Argon2.Verify(hash, password);

    private static string NormalizePhone(string phone) => new string(phone.Where(char.IsDigit).ToArray());

    private static string NormalizeCpf(string cpf) => new string(cpf.Where(char.IsDigit).ToArray());

    private static bool ValidateCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        var digits = cpf.Select(c => int.Parse(c.ToString())).ToArray();
        var sum = 0;
        for (int i = 0; i < 9; i++) sum += digits[i] * (10 - i);
        var d1 = sum % 11 < 2 ? 0 : 11 - sum % 11;
        if (digits[9] != d1) return false;
        sum = 0;
        for (int i = 0; i < 10; i++) sum += digits[i] * (11 - i);
        var d2 = sum % 11 < 2 ? 0 : 11 - sum % 11;
        return digits[10] == d2;
    }

    private static string DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        userAgent = userAgent.ToLower();
        if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone")) return "Mobile";
        if (userAgent.Contains("tablet") || userAgent.Contains("ipad")) return "Tablet";
        return "Desktop";
    }

    private static CustomerInfoDto MapToCustomerInfo(CustomerAuth auth) => new()
    {
        Id = auth.CustomerId,
        FullName = auth.Customer?.FullName ?? "",
        Phone = auth.Phone,
        Email = auth.Customer?.Email,
        IsVerified = auth.IsVerified
    };
}
