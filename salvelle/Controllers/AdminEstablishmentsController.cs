using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/establishments")]
public class AdminEstablishmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminEstablishmentsController> _logger;
    private readonly AuditService _audit;

    /// <summary>Sanitiza campo CSV contra formula injection (=, +, -, @, tab, carriage return)</summary>
    private static string SanitizeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var trimmed = value.Replace(";", ",").Replace("\n", " ").Replace("\r", " ");
        if (trimmed.StartsWith("=") || trimmed.StartsWith("+") || trimmed.StartsWith("-") || trimmed.StartsWith("@") || trimmed.StartsWith("\t"))
            trimmed = "'" + trimmed;
        return trimmed;
    }

    public AdminEstablishmentsController(AppDbContext context, ILogger<AdminEstablishmentsController> logger, AuditService audit)
    {
        _context = context;
        _logger = logger;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        try
        {
            var query = _context.Establishments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(e =>
                    e.NomeFantasia.ToLower().Contains(searchLower) ||
                    (e.Cnpj != null && e.Cnpj.Contains(search)) ||
                    (e.Email != null && e.Email.ToLower().Contains(searchLower)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var isActive = status.ToUpper() == "ACTIVE";
                query = query.Where(e => e.IsActive == isActive);
            }

            var total = await query.CountAsync();

            var establishments = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip(skip)
                .Take(Math.Min(take, 100))
                .Select(e => new
                {
                    e.Id,
                    e.NomeFantasia,
                    e.RazaoSocial,
                    e.Cnpj,
                    e.Email,
                    e.Phone,
                    e.WhatsApp,
                    e.City,
                    e.State,
                    e.IsActive,
                    e.SubscriptionStatus,
                    e.CreatedAt
                })
                .ToListAsync();

            return Ok(new { total, establishments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estabelecimentos");
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var establishment = await _context.Establishments
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    e.NomeFantasia,
                    e.RazaoSocial,
                    e.Cnpj,
                    e.Email,
                    e.Phone,
                    e.WhatsApp,
                    e.Street,
                    e.Number,
                    e.Complement,
                    e.Neighborhood,
                    e.City,
                    e.State,
                    e.PostalCode,
                    e.Country,
                    e.IsActive,
                    e.SubscriptionStatus,
                    e.OnboardingCompleted,
                    e.MaxEmployeesLimit,
                    e.MaxOrdersLimit,
                    e.TrialEndsAt,
                    e.CreatedAt,
                    e.UpdatedAt,
                    Subscription = _context.Subscriptions
                        .Where(s => s.EstablishmentId == e.Id)
                        .OrderByDescending(s => s.CreatedAt)
                        .Select(s => new
                        {
                            s.Id,
                            s.Status,
                            s.BillingCycle,
                            s.CurrentPeriodStart,
                            s.CurrentPeriodEnd,
                            s.TrialStart,
                            s.TrialEnd,
                            s.CanceledAt,
                            PlanName = s.SubscriptionPlan != null ? s.SubscriptionPlan.Name : null,
                            Amount = s.SubscriptionPlan != null ? 
                                (s.BillingCycle == "YEARLY" ? s.SubscriptionPlan.PriceYearly : s.SubscriptionPlan.PriceMonthly) : 0
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (establishment == null)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var employeesCount = await _context.Employees
                .CountAsync(e => e.EstablishmentId == id);

            return Ok(new { establishment, employeesCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estabelecimento {Id}", id);
            return StatusCode(500, new { message = "Erro ao buscar dados" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEstablishmentDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            // Atualizar campos básicos
            if (!string.IsNullOrWhiteSpace(dto.NomeFantasia))
                establishment.NomeFantasia = dto.NomeFantasia;

            if (dto.RazaoSocial != null)
                establishment.RazaoSocial = dto.RazaoSocial;

            if (!string.IsNullOrWhiteSpace(dto.Cnpj))
                establishment.Cnpj = dto.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "");

            if (!string.IsNullOrWhiteSpace(dto.Email))
                establishment.Email = dto.Email;

            if (dto.WhatsApp != null)
                establishment.WhatsApp = dto.WhatsApp;

            if (dto.Phone != null)
                establishment.Phone = dto.Phone;

            // Endereço
            if (dto.Street != null)
                establishment.Street = dto.Street;

            if (dto.Number != null)
                establishment.Number = dto.Number;

            if (dto.Complement != null)
                establishment.Complement = dto.Complement;

            if (dto.Neighborhood != null)
                establishment.Neighborhood = dto.Neighborhood;

            if (dto.City != null)
                establishment.City = dto.City;

            if (dto.State != null)
                establishment.State = dto.State.ToUpper();

            if (dto.PostalCode != null)
                establishment.PostalCode = dto.PostalCode;

            // Limites
            if (dto.MaxEmployeesLimit.HasValue)
                establishment.MaxEmployeesLimit = dto.MaxEmployeesLimit.Value;

            if (dto.MaxOrdersLimit.HasValue)
                establishment.MaxOrdersLimit = dto.MaxOrdersLimit.Value;

            // Status
            if (dto.IsActive.HasValue)
                establishment.IsActive = dto.IsActive.Value;

            if (dto.OnboardingCompleted.HasValue)
                establishment.OnboardingCompleted = dto.OnboardingCompleted.Value;

            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Estabelecimento {Id} atualizado", id);

            return Ok(new { message = "Estabelecimento atualizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar estabelecimento {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar dados" });
        }
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockReasonDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            establishment.IsActive = false;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Estabelecimento {Id} bloqueado. Motivo: {Reason}", id, dto.Reason);
            await _audit.LogAsync(HttpContext, "ESTABLISHMENT_BLOCKED", "Establishment", id.ToString(), dto.Reason);

            return Ok(new { message = "Estabelecimento bloqueado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao bloquear estabelecimento {Id}", id);
            return StatusCode(500, new { message = "Erro ao bloquear" });
        }
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var establishment = await _context.Establishments.FindAsync(id);
            if (establishment == null)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            establishment.IsActive = true;
            establishment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Estabelecimento {Id} desbloqueado", id);
            await _audit.LogAsync(HttpContext, "ESTABLISHMENT_UNBLOCKED", "Establishment", id.ToString());

            return Ok(new { message = "Estabelecimento desbloqueado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desbloquear estabelecimento {Id}", id);
            return StatusCode(500, new { message = "Erro ao desbloquear" });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        try
        {
            var establishments = await _context.Establishments
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.NomeFantasia,
                    e.RazaoSocial,
                    e.Cnpj,
                    e.Email,
                    e.WhatsApp,
                    e.City,
                    e.State,
                    e.IsActive,
                    e.SubscriptionStatus,
                    e.CreatedAt
                })
                .ToListAsync();

            var csv = "Nome Fantasia;Razão Social;CNPJ;Email;WhatsApp;Cidade;UF;Ativo;Assinatura;Cadastro\n";
            foreach (var e in establishments)
            {
                csv += $"{SanitizeCsv(e.NomeFantasia)};{SanitizeCsv(e.RazaoSocial)};{SanitizeCsv(e.Cnpj)};{SanitizeCsv(e.Email)};{SanitizeCsv(e.WhatsApp)};{SanitizeCsv(e.City)};{SanitizeCsv(e.State)};{(e.IsActive ? "Sim" : "Não")};{e.SubscriptionStatus};{e.CreatedAt:dd/MM/yyyy}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileName = $"estabelecimentos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar estabelecimentos");
            return StatusCode(500, new { message = "Erro ao exportar" });
        }
    }

    private bool IsSuperAdmin()
    {
        var role = HttpContext.Items["SaasAdminRole"] as string;
        return string.Equals(role, "SUPER_ADMIN", StringComparison.Ordinal);
    }
}

public class UpdateEstablishmentDto
{
    public string? NomeFantasia { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Cnpj { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public int? MaxEmployeesLimit { get; set; }
    public int? MaxOrdersLimit { get; set; }
    public bool? IsActive { get; set; }
    public bool? OnboardingCompleted { get; set; }
}

public class BlockReasonDto
{
    public string? Reason { get; set; }
}
