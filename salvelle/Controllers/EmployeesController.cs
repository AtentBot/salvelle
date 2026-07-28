using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Isopoh.Cryptography.Argon2;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using DTOs.Auth;
using DTOs.Employees;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmployeesController> _logger;

    /// <summary>Mascara CPF: exibe apenas últimos 4 dígitos (***.***.***-XX)</summary>
    private static string MaskCpf(string? cpf)
    {
        if (string.IsNullOrEmpty(cpf)) return "";
        var digits = cpf.Replace(".", "").Replace("-", "").Replace(" ", "");
        if (digits.Length < 4) return "***";
        return $"***.***.*{digits[^4..^2]}-{digits[^2..]}";
    }

    /// <summary>Mascara conta bancária: exibe apenas últimos 3 dígitos</summary>
    private static string MaskBankAccount(string? account)
    {
        if (string.IsNullOrEmpty(account)) return "";
        if (account.Length <= 3) return "***";
        return new string('*', account.Length - 3) + account[^3..];
    }

    public EmployeesController(AppDbContext db, ILogger<EmployeesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== LOGIN DO FUNCION�RIO ====================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // Remover formata��o do CPF
        var cpf = DocumentValidator.RemoveFormatting(dto.Cpf);

        // Validar CPF
        if (!DocumentValidator.IsValidCpf(cpf))

            return BadRequest(new { error = "CPF inv�lido" });

        try
        {
            // Buscar funcion�rio
            var employee = await _db.Employees
                .Include(e => e.Establishment)
                .Include(e => e.JobPosition)
                .FirstOrDefaultAsync(e => e.Cpf == cpf);


            if (employee == null)
                return Unauthorized(new { error = "Credenciais inv�lidas" });

            // Verificar status do funcion�rio
            if (employee.Status != "Ativo")
                return Unauthorized(new { error = $"Funcion�rio {employee.Status.ToLower()}" });

            // Verificar se est� bloqueado
            if (employee.LockedUntil.HasValue && employee.LockedUntil.Value > DateTime.UtcNow)
            {
                var remainingMinutes = (employee.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
                return Unauthorized(new { error = $"Conta bloqueada. Tente novamente em {Math.Ceiling(remainingMinutes)} minutos" });
            }

            // Verificar senha
            if (!Argon2.Verify(employee.PasswordHash, dto.Password))
            {
                // Incrementar tentativas falhas
                employee.FailedLoginAttempts++;

                if (employee.FailedLoginAttempts >= 5)
                {
                    employee.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                    await _db.SaveChangesAsync();
                    return Unauthorized(new { error = "Conta bloqueada por 30 minutos devido a m�ltiplas tentativas falhas" });
                }

                await _db.SaveChangesAsync();
                return Unauthorized(new { error = "Credenciais inv�lidas" });
            }

            // Verificar se estabelecimento est� ativo
            if (!employee.Establishment!.IsActive)
                return Unauthorized(new { error = "Estabelecimento inativo" });

            // Resetar tentativas falhas
            employee.FailedLoginAttempts = 0;
            employee.LockedUntil = null;

            // Criar sess�o
            var session = new EmployeeSession
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('='),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                DeviceType = GetDeviceType(Request.Headers["User-Agent"].ToString()),
                Browser = GetBrowser(Request.Headers["User-Agent"].ToString()),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                RequiresTwoFactor = employee.TwoFactorEnabled,
                TwoFactorVerified = !employee.TwoFactorEnabled, // Se n�o tem 2FA, j� est� verificado
                SessionName = $"{GetBrowser(Request.Headers["User-Agent"].ToString())} - {employee.City}/{employee.State}"
            };

            _db.EmployeeSessions.Add(session);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Funcion�rio {FullName} (ID: {Id}) fez login com sucesso",
                employee.FullName, employee.Id);

            Response.Cookies.Append("SessionId", session.Token, new CookieOptions
            {
                HttpOnly = true,              // Prote��o contra XSS
                Secure = true,                // Sempre Secure — proxy TLS termina antes do app
                SameSite = SameSiteMode.Strict,
                Expires = session.ExpiresAt,  // Mesmo tempo da sess�o (8h)
                Path = "/",                   // V�lido para todo o site
                IsEssential = true            // Cookie essencial para funcionamento
            });

            return Ok(new
            {
                token = session.Token,
                expiresAt = session.ExpiresAt,
                requiresPasswordChange = employee.RequirePasswordChange,
                requiresTwoFactor = employee.TwoFactorEnabled && !session.TwoFactorVerified,
                employee = new
                {
                    id = employee.Id,
                    fullName = employee.FullName,
                    socialName = employee.SocialName,
                    email = employee.Email,
                    jobPosition = employee.JobPosition?.Name,
                    jobPositionCode = employee.JobPosition?.Code,
                    hierarchyLevel = employee.JobPosition?.HierarchyLevel,
                    establishment = new
                    {
                        id = employee.Establishment.Id,
                        name = employee.Establishment.NomeFantasia,
                        cnpj = employee.Establishment.Cnpj
                    }
                }
            });

        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Erro ao atualizar banco de dados durante login");
            return StatusCode(500, new
            {
                error = "Erro ao processar login. Tente novamente.",
                details = "Problema ao salvar dados no banco"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante login");
            return StatusCode(500, new { error = "Erro interno. Tente novamente." });
        }
    }

    // ==================== CRIAR FUNCION�RIO ====================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        // TODO: Verificar permiss�es do token (implementar na Fase 2)
        // if (!await HasPermission("employees.create")) return Forbid();

        // Validar e limpar CPF
        var cpf = DocumentValidator.RemoveFormatting(dto.Cpf);
        if (!DocumentValidator.IsValidCpf(cpf))
            return BadRequest(new { error = "CPF inv�lido" });

        // Verificar CPF duplicado
        if (await _db.Employees.AnyAsync(e => e.Cpf == cpf))
            return Conflict(new { error = "CPF j� cadastrado" });

        // Validar PIS/PASEP se fornecido
        if (!string.IsNullOrEmpty(dto.PisPasep))
        {
            var pis = DocumentValidator.RemoveFormatting(dto.PisPasep);
            if (!DocumentValidator.IsValidPis(pis))
                return BadRequest(new { error = "PIS/PASEP inv�lido" });
            dto.PisPasep = pis;
        }

        // Validar senha
        var (isPasswordValid, passwordErrors) = PasswordValidator.ValidatePassword(dto.Password);
        if (!isPasswordValid)
            return BadRequest(new { error = "Senha inv�lida", details = passwordErrors });

        // Verificar se cargo existe
        var jobPosition = await _db.JobPositions
            .FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId && jp.IsActive);

        if (jobPosition == null)
            return BadRequest(new { error = "Cargo n�o encontrado ou inativo" });

        // Verificar se estabelecimento existe
        var establishment = await _db.Establishments
            .FirstOrDefaultAsync(e => e.Id == dto.EstablishmentId && e.IsActive);

        if (establishment == null)
            return BadRequest(new { error = "Estabelecimento n�o encontrado ou inativo" });

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EstablishmentId = dto.EstablishmentId,
            JobPositionId = dto.JobPositionId,

            // Dados Pessoais
            FullName = dto.FullName,
            SocialName = dto.SocialName,
            Cpf = cpf,
            Rg = dto.Rg,
            RgIssuer = dto.RgIssuer,
            RgIssueDate = dto.RgIssueDate,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Nationality = dto.Nationality ?? "Brasileira",
            PlaceOfBirth = dto.PlaceOfBirth,
            MaritalStatus = dto.MaritalStatus,

            // Contatos
            Phone = dto.Phone,
            WhatsApp = dto.WhatsApp,
            Email = dto.Email,

            // Endere�o
            Street = dto.Street,
            Number = dto.Number,
            Complement = dto.Complement,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,

            // Dados Trabalhistas
            HireDate = dto.HireDate,
            ContractType = dto.ContractType ?? "CLT",
            WorkShift = dto.WorkShift ?? "COMERCIAL",
            Salary = dto.Salary,
            Department = dto.Department,

            // Documentos
            PisPasep = dto.PisPasep,
            Ctps = dto.Ctps,
            CtpsSeries = dto.CtpsSeries,
            CtpsUf = dto.CtpsUf,
            CtpsIssueDate = dto.CtpsIssueDate,
            VoterRegistration = dto.VoterRegistration,
            MilitaryService = dto.MilitaryService,
            DriverLicense = dto.DriverLicense,
            DriverLicenseCategory = dto.DriverLicenseCategory,
            DriverLicenseExpiry = dto.DriverLicenseExpiry,

            // Dados Banc�rios
            BankCode = dto.BankCode,
            BankName = dto.BankName,
            BankBranch = dto.BankBranch,
            BankAccount = dto.BankAccount,
            BankAccountType = dto.BankAccountType,
            BankAccountDigit = dto.BankAccountDigit,

            // Contato de Emerg�ncia
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactRelationship = dto.EmergencyContactRelationship,
            EmergencyContactPhone = dto.EmergencyContactPhone,

            // Dependentes
            DependentsCount = dto.DependentsCount,

            // Status
            Status = "Ativo",
            ProbationEndDate = dto.HireDate.AddDays(dto.ProbationDays ?? 90),

            // Seguran�a
            PasswordHash = Argon2.Hash(dto.Password),
            PasswordAlgorithm = "argon2id-v1",
            PasswordCreatedAt = DateTime.UtcNow,
            RequirePasswordChange = true, // Primeira senha � tempor�ria
            TwoFactorEnabled = false,
            FailedLoginAttempts = 0,

            // Auditoria
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000") // TODO: Pegar do token na Fase 2
        };

        _db.Employees.Add(employee);

        // Criar hist�rico inicial de cargo
        var jobHistory = new EmployeeJobHistory
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            JobPositionId = employee.JobPositionId,
            StartDate = employee.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            IsCurrent = true,
            SalaryAtTime = employee.Salary ?? 0,
            ChangeReason = "Contrata��o",
            Notes = "Admiss�o inicial",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = employee.CreatedBy
        };

        _db.EmployeeJobHistories.Add(jobHistory);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcionário {FullName} cadastrado com sucesso (ID: {EmployeeId})",
            employee.FullName, employee.Id);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new
        {
            id = employee.Id,
            fullName = employee.FullName,
            cpf = DocumentValidator.FormatCpf(employee.Cpf),
            email = employee.Email,
            jobPosition = jobPosition.Name,
            status = employee.Status,
            hireDate = employee.HireDate
        });
    }

    // ==================== BUSCAR FUNCION�RIO POR ID ====================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound(new { error = "Funcion�rio n�o encontrado" });

        // TODO: Verificar permiss�es (se pode ver dados de outros funcion�rios)

        // Retornar no formato que a view Details.cshtml espera
        return Ok(new
        {
            id = employee.Id,
            establishmentId = employee.EstablishmentId,

            // Objeto establishment para a view
            establishment = new
            {
                id = employee.Establishment?.Id,
                nomeFantasia = employee.Establishment?.NomeFantasia,
                razaoSocial = employee.Establishment?.RazaoSocial,
                cnpj = employee.Establishment?.Cnpj
            },

            // Objeto jobPosition para a view
            jobPosition = new
            {
                id = employee.JobPosition?.Id,
                code = employee.JobPosition?.Code,
                name = employee.JobPosition?.Name,
                hierarchyLevel = employee.JobPosition?.HierarchyLevel
            },

            // Dados Pessoais (no n�vel raiz para a view)
            fullName = employee.FullName,
            socialName = employee.SocialName,
            cpf = employee.Cpf, // Sem formata��o para permitir formata��o no frontend
            rg = employee.Rg,
            rgIssuer = employee.RgIssuer,
            rgIssueDate = employee.RgIssueDate,
            dateOfBirth = employee.DateOfBirth,
            gender = employee.Gender,
            nationality = employee.Nationality,
            placeOfBirth = employee.PlaceOfBirth,
            maritalStatus = employee.MaritalStatus,

            // Contatos
            phone = employee.Phone,
            whatsApp = employee.WhatsApp,
            email = employee.Email,

            // Endere�o (no n�vel raiz para a view)
            street = employee.Street,
            number = employee.Number,
            complement = employee.Complement,
            neighborhood = employee.Neighborhood,
            city = employee.City,
            state = employee.State,
            postalCode = employee.PostalCode,

            // Dados Trabalhistas
            hireDate = employee.HireDate,
            terminationDate = employee.TerminationDate,
            contractType = employee.ContractType,
            workShift = employee.WorkShift,
            salary = employee.Salary,
            department = employee.Department,

            // Dados Bancários (mascarados)
            bankCode = employee.BankCode,
            bankName = employee.BankName,
            bankBranch = employee.BankBranch,
            bankAccount = MaskBankAccount(employee.BankAccount),
            bankAccountType = employee.BankAccountType,
            bankAccountDigit = employee.BankAccountDigit,

            // Status
            status = employee.Status,
            statusNotes = employee.StatusNotes,
            probationEndDate = employee.ProbationEndDate,

            // Contato de Emerg�ncia
            emergencyContactName = employee.EmergencyContactName,
            emergencyContactPhone = employee.EmergencyContactPhone,
            emergencyContactRelationship = employee.EmergencyContactRelationship,

            // Seguran�a
            twoFactorEnabled = employee.TwoFactorEnabled,
            requirePasswordChange = employee.RequirePasswordChange,

            // Auditoria
            createdAt = employee.CreatedAt,
            updatedAt = employee.UpdatedAt,

            // Dados adicionais para as tabs (ser�o preenchidos depois se necess�rio)
            documents = new object[] { },
            benefits = new object[] { },
            sessions = new object[] { },
            jobHistory = new object[] { }
        });
    }

    // ==================== LISTAR FUNCION�RIOS ====================
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? establishmentId,
        [FromQuery] string? status,
        [FromQuery] Guid? jobPositionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        // Verificar autentica��o via middleware
        var currentEmployee = HttpContext.Items["Employee"] as Employee;
        if (currentEmployee == null)
            return Unauthorized(new { error = "N�o autenticado" });

        // Se n�o especificou estabelecimento, usa o do funcion�rio logado
        var estId = establishmentId ?? currentEmployee.EstablishmentId;

        var query = _db.Employees
            .Include(e => e.JobPosition)
            .Include(e => e.Establishment)
            .Where(e => e.EstablishmentId == estId)
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        if (jobPositionId.HasValue)
            query = query.Where(e => e.JobPositionId == jobPositionId);

        var total = await query.CountAsync();
        var rawEmployees = await query
            .OrderBy(e => e.FullName)
            .Skip(skip)
            .Take(take)
            .Select(e => new
            {
                id = e.Id,
                fullName = e.FullName,
                socialName = e.SocialName,
                cpf = e.Cpf,
                email = e.Email,
                phone = e.Phone,
                status = e.Status,
                hireDate = e.HireDate,
                jobPosition = new
                {
                    id = e.JobPosition!.Id,
                    code = e.JobPosition.Code,
                    name = e.JobPosition.Name
                },
                establishment = new
                {
                    id = e.Establishment!.Id,
                    nomeFantasia = e.Establishment.NomeFantasia
                }
            })
            .ToListAsync();

        var employees = rawEmployees.Select(e => new
        {
            e.id, e.fullName, e.socialName,
            cpf = MaskCpf(e.cpf),
            e.email, e.phone, e.status, e.hireDate,
            e.jobPosition, e.establishment
        });

        return Ok(new
        {
            items = employees,
            total,
            skip,
            take
        });
    }

    // ==================== ATUALIZAR FUNCION�RIO ====================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _db.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound(new { error = "Funcion�rio n�o encontrado" });

        // Atualizar campos permitidos
        if (!string.IsNullOrEmpty(dto.FullName))
            employee.FullName = dto.FullName;

        if (dto.SocialName != null)
            employee.SocialName = dto.SocialName;

        if (!string.IsNullOrEmpty(dto.Email))
            employee.Email = dto.Email;

        if (dto.Phone != null)
            employee.Phone = dto.Phone;

        if (dto.WhatsApp != null)
            employee.WhatsApp = dto.WhatsApp;

        // Endere�o
        if (dto.Street != null) employee.Street = dto.Street;
        if (dto.Number != null) employee.Number = dto.Number;
        if (dto.Complement != null) employee.Complement = dto.Complement;
        if (dto.Neighborhood != null) employee.Neighborhood = dto.Neighborhood;
        if (dto.City != null) employee.City = dto.City;
        if (dto.State != null) employee.State = dto.State;
        if (dto.PostalCode != null) employee.PostalCode = dto.PostalCode;

        // Dados pessoais
        if (dto.Gender != null) employee.Gender = dto.Gender;
        if (dto.MaritalStatus != null) employee.MaritalStatus = dto.MaritalStatus;

        // Dados banc�rios
        if (dto.BankCode != null) employee.BankCode = dto.BankCode;
        if (dto.BankName != null) employee.BankName = dto.BankName;
        if (dto.BankBranch != null) employee.BankBranch = dto.BankBranch;
        if (dto.BankAccount != null) employee.BankAccount = dto.BankAccount;
        if (dto.BankAccountType != null) employee.BankAccountType = dto.BankAccountType;
        if (dto.BankAccountDigit != null) employee.BankAccountDigit = dto.BankAccountDigit;

        // Contato de emerg�ncia
        if (dto.EmergencyContactName != null) employee.EmergencyContactName = dto.EmergencyContactName;
        if (dto.EmergencyContactPhone != null) employee.EmergencyContactPhone = dto.EmergencyContactPhone;
        if (dto.EmergencyContactRelationship != null) employee.EmergencyContactRelationship = dto.EmergencyContactRelationship;

        // Status
        if (!string.IsNullOrEmpty(dto.Status))
        {
            employee.Status = dto.Status;
            if (dto.Status == "Demitido" && dto.TerminationDate.HasValue)
                employee.TerminationDate = dto.TerminationDate;
        }

        if (dto.StatusNotes != null)
            employee.StatusNotes = dto.StatusNotes;

        // Mudan�a de cargo
        if (dto.JobPositionId.HasValue && dto.JobPositionId != employee.JobPositionId)
        {
            var newJobPosition = await _db.JobPositions.FindAsync(dto.JobPositionId);
            if (newJobPosition == null || !newJobPosition.IsActive)
                return BadRequest(new { error = "Cargo n�o encontrado ou inativo" });

            // Fechar hist�rico anterior
            var currentHistory = await _db.EmployeeJobHistories
                .FirstOrDefaultAsync(h => h.EmployeeId == id && h.IsCurrent);

            if (currentHistory != null)
            {
                currentHistory.IsCurrent = false;
                currentHistory.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            // Criar novo hist�rico
            var newHistory = new EmployeeJobHistory
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                JobPositionId = dto.JobPositionId.Value,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                IsCurrent = true,
                SalaryAtTime = dto.Salary ?? employee.Salary ?? 0,
                ChangeReason = dto.ChangeReason ?? "Altera��o de cargo",
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.EmployeeJobHistories.Add(newHistory);
            employee.JobPositionId = dto.JobPositionId.Value;
        }

        // Sal�rio
        if (dto.Salary.HasValue)
            employee.Salary = dto.Salary.Value;

        employee.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcion�rio {FullName} (ID: {Id}) atualizado com sucesso",
            employee.FullName, employee.Id);

        return Ok(new { message = "Funcion�rio atualizado com sucesso" });
    }

    // ==================== ALTERAR SENHA ====================
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangeEmployeePasswordDto dto)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { error = "Funcion�rio n�o encontrado" });

        // Verificar senha atual (se n�o for reset administrativo)
        if (!dto.IsAdminReset)
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword))
                return BadRequest(new { error = "Senha atual � obrigat�ria" });

            if (!Argon2.Verify(employee.PasswordHash, dto.CurrentPassword))
                return BadRequest(new { error = "Senha atual incorreta" });
        }

        // Validar nova senha
        var (isValid, errors) = PasswordValidator.ValidatePassword(dto.NewPassword);
        if (!isValid)
            return BadRequest(new { error = "Nova senha inv�lida", details = errors });

        // Atualizar senha
        employee.PasswordHash = Argon2.Hash(dto.NewPassword);
        employee.PasswordCreatedAt = DateTime.UtcNow;
        employee.PasswordLastChanged = DateTime.UtcNow;
        employee.RequirePasswordChange = false;
        employee.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Senha do funcion�rio {Id} alterada com sucesso", id);

        return Ok(new { message = "Senha alterada com sucesso" });
    }

    // ==================== DESATIVAR FUNCION�RIO ====================
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateEmployeeDto dto)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { error = "Funcion�rio n�o encontrado" });

        employee.Status = "Demitido";
        employee.StatusNotes = dto.Reason;
        employee.TerminationDate = dto.TerminationDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        employee.UpdatedAt = DateTime.UtcNow;

        // Revogar todas as sess�es
        var sessions = await _db.EmployeeSessions
            .Where(s => s.EmployeeId == id && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "Funcion�rio desativado";
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcion�rio {Id} desativado. Motivo: {Reason}", id, dto.Reason);

        return Ok(new { message = "Funcion�rio desativado com sucesso" });
    }

    // ==================== REATIVAR FUNCION�RIO ====================
    [HttpPost("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { error = "Funcion�rio n�o encontrado" });

        employee.Status = "Ativo";
        employee.StatusNotes = null;
        employee.TerminationDate = null;
        employee.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcion�rio {Id} reativado com sucesso", id);

        return Ok(new { message = "Funcion�rio reativado com sucesso" });
    }

    // ==================== MEU PERFIL (auto-serviço) ====================

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee == null) return Unauthorized();

        return Ok(new
        {
            employee.Id,
            employee.FullName,
            employee.SocialName,
            employee.Email,
            employee.Phone,
            employee.WhatsApp,
            employee.Cpf,
            jobPosition = employee.JobPosition?.Name,
            jobPositionCode = employee.JobPosition?.Code,
            employee.RequirePasswordChange
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeDto dto)
    {
        var current = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (current == null) return Unauthorized();

        var employee = await _db.Employees.FindAsync(current.Id);
        if (employee == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            employee.FullName = dto.FullName.Trim();

        employee.SocialName = string.IsNullOrWhiteSpace(dto.SocialName) ? null : dto.SocialName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var newEmail = dto.Email.Trim().ToLowerInvariant();
            if (newEmail != employee.Email)
            {
                var emailInUse = await _db.Employees.AnyAsync(e => e.Id != employee.Id && e.Email == newEmail);
                if (emailInUse)
                    return BadRequest(new { error = "Este e-mail já está em uso por outro funcionário" });

                employee.Email = newEmail;
            }
        }

        if (dto.Phone != null)
            employee.Phone = dto.Phone.Trim();

        if (dto.WhatsApp != null)
            employee.WhatsApp = dto.WhatsApp.Trim();

        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Perfil atualizado com sucesso" });
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordDto dto)
    {
        var current = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (current == null) return Unauthorized();

        var employee = await _db.Employees.FindAsync(current.Id);
        if (employee == null) return NotFound();

        if (!Argon2.Verify(employee.PasswordHash, dto.CurrentPassword))
            return BadRequest(new { error = "Senha atual incorreta" });

        var (isValid, errors) = PasswordValidator.ValidatePassword(dto.NewPassword);
        if (!isValid)
            return BadRequest(new { error = "Senha inválida: " + string.Join(", ", errors) });

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new { error = "As senhas não coincidem" });

        employee.PasswordHash = Argon2.Hash(dto.NewPassword);
        employee.PasswordLastChanged = DateTime.UtcNow;
        employee.RequirePasswordChange = false;
        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Senha alterada com sucesso" });
    }

    // ==================== HELPERS ====================
    private string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)) return "Mobile";
        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)) return "Tablet";
        return "Desktop";
    }

    private string GetBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        return "Unknown";
    }
}

// DTOs est�o em DTOs/Employees/EmployeeRequests.cs