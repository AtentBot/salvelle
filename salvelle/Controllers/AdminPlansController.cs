using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs;
using Service;
using System.Text.Json;

namespace Controllers.Api;

[ApiController]
[Route("api/admin/plans")]
public class AdminPlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminPlansController> _logger;
    private readonly AuditService _audit;

    public AdminPlansController(AppDbContext context, ILogger<AdminPlansController> logger, AuditService audit)
    {
        _context = context;
        _logger = logger;
        _audit = audit;
    }

    /// <summary>
    /// Lista todos os planos
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _context.Set<SubscriptionPlan>()
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.PriceMonthly,
                p.PriceYearly,
                p.MaxEmployees,
                p.MaxMonthlyOrders,
                p.Features,
                p.StripePriceIdMonthly,
                p.StripePriceIdYearly,
                p.MercadoPagoPlanIdMonthly,
                p.MercadoPagoPlanIdYearly,
                p.AbacatepayPlanIdMonthly,
                p.AbacatepayPlanIdYearly,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// Obtém um plano específico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
        
        if (plan == null)
            return NotFound(new { message = "Plano não encontrado" });

        return Ok(new
        {
            plan.Id,
            plan.Name,
            plan.Description,
            plan.PriceMonthly,
            plan.PriceYearly,
            plan.MaxEmployees,
            plan.MaxMonthlyOrders,
            plan.Features,
            plan.StripePriceIdMonthly,
            plan.StripePriceIdYearly,
            plan.MercadoPagoPlanIdMonthly,
            plan.MercadoPagoPlanIdYearly,
            plan.AbacatepayPlanIdMonthly,
            plan.AbacatepayPlanIdYearly,
            plan.IsActive,
            plan.CreatedAt,
            plan.UpdatedAt
        });
    }

    /// <summary>
    /// Cria um novo plano
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Nome do plano é obrigatório" });

            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                PriceMonthly = dto.PriceMonthly,
                PriceYearly = dto.PriceYearly,
                MaxEmployees = dto.MaxEmployees,
                MaxMonthlyOrders = dto.MaxMonthlyOrders,
                Features = dto.Features != null 
                    ? JsonSerializer.Serialize(dto.Features) 
                    : "{}",
                // Gateway IDs
                StripePriceIdMonthly = dto.StripePriceIdMonthly?.Trim(),
                StripePriceIdYearly = dto.StripePriceIdYearly?.Trim(),
                MercadoPagoPlanIdMonthly = dto.MercadoPagoPlanIdMonthly?.Trim(),
                MercadoPagoPlanIdYearly = dto.MercadoPagoPlanIdYearly?.Trim(),
                AbacatepayPlanIdMonthly = dto.AbacatepayPlanIdMonthly?.Trim(),
                AbacatepayPlanIdYearly = dto.AbacatepayPlanIdYearly?.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<SubscriptionPlan>().Add(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano criado: {Name} (ID: {Id})", plan.Name, plan.Id);

            await _audit.LogAsync(HttpContext, "PLAN_CREATED", "SubscriptionPlan", plan.Id.ToString(), $"Plan: {plan.Name}");

            return Ok(new { message = "Plano criado com sucesso", id = plan.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar plano");
            return StatusCode(500, new { message = "Erro ao criar plano" });
        }
    }

    /// <summary>
    /// Atualiza um plano existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePlanDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Nome do plano é obrigatório" });

            plan.Name = dto.Name.Trim();
            plan.Description = dto.Description?.Trim();
            plan.PriceMonthly = dto.PriceMonthly;
            plan.PriceYearly = dto.PriceYearly;
            plan.MaxEmployees = dto.MaxEmployees;
            plan.MaxMonthlyOrders = dto.MaxMonthlyOrders;
            plan.IsActive = dto.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            if (dto.Features != null)
            {
                plan.Features = JsonSerializer.Serialize(dto.Features);
            }

            // ═══════════════════════════════════════════════════════════════
            // GATEWAY IDs - Stripe, MercadoPago, Abacatepay
            // ═══════════════════════════════════════════════════════════════
            
            // Stripe
            if (dto.StripePriceIdMonthly != null)
                plan.StripePriceIdMonthly = string.IsNullOrWhiteSpace(dto.StripePriceIdMonthly) 
                    ? null : dto.StripePriceIdMonthly.Trim();
            
            if (dto.StripePriceIdYearly != null)
                plan.StripePriceIdYearly = string.IsNullOrWhiteSpace(dto.StripePriceIdYearly) 
                    ? null : dto.StripePriceIdYearly.Trim();
            
            // MercadoPago
            if (dto.MercadoPagoPlanIdMonthly != null)
                plan.MercadoPagoPlanIdMonthly = string.IsNullOrWhiteSpace(dto.MercadoPagoPlanIdMonthly) 
                    ? null : dto.MercadoPagoPlanIdMonthly.Trim();
            
            if (dto.MercadoPagoPlanIdYearly != null)
                plan.MercadoPagoPlanIdYearly = string.IsNullOrWhiteSpace(dto.MercadoPagoPlanIdYearly) 
                    ? null : dto.MercadoPagoPlanIdYearly.Trim();
            
            // Abacatepay
            if (dto.AbacatepayPlanIdMonthly != null)
                plan.AbacatepayPlanIdMonthly = string.IsNullOrWhiteSpace(dto.AbacatepayPlanIdMonthly) 
                    ? null : dto.AbacatepayPlanIdMonthly.Trim();
            
            if (dto.AbacatepayPlanIdYearly != null)
                plan.AbacatepayPlanIdYearly = string.IsNullOrWhiteSpace(dto.AbacatepayPlanIdYearly) 
                    ? null : dto.AbacatepayPlanIdYearly.Trim();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano atualizado: {Name} (ID: {Id})", plan.Name, plan.Id);

            await _audit.LogAsync(HttpContext, "PLAN_UPDATED", "SubscriptionPlan", plan.Id.ToString(), $"Plan: {plan.Name}");

            return Ok(new { message = "Plano atualizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar plano" });
        }
    }

    /// <summary>
    /// Atualiza apenas os Gateway IDs de um plano (endpoint simplificado)
    /// </summary>
    [HttpPatch("{id}/gateway-ids")]
    public async Task<IActionResult> UpdateGatewayIds(Guid id, [FromBody] UpdateGatewayIdsDto dto)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            // Stripe
            if (dto.StripePriceIdMonthly != null)
                plan.StripePriceIdMonthly = string.IsNullOrWhiteSpace(dto.StripePriceIdMonthly) 
                    ? null : dto.StripePriceIdMonthly.Trim();
            
            if (dto.StripePriceIdYearly != null)
                plan.StripePriceIdYearly = string.IsNullOrWhiteSpace(dto.StripePriceIdYearly) 
                    ? null : dto.StripePriceIdYearly.Trim();
            
            // MercadoPago
            if (dto.MercadoPagoPlanIdMonthly != null)
                plan.MercadoPagoPlanIdMonthly = string.IsNullOrWhiteSpace(dto.MercadoPagoPlanIdMonthly) 
                    ? null : dto.MercadoPagoPlanIdMonthly.Trim();
            
            if (dto.MercadoPagoPlanIdYearly != null)
                plan.MercadoPagoPlanIdYearly = string.IsNullOrWhiteSpace(dto.MercadoPagoPlanIdYearly) 
                    ? null : dto.MercadoPagoPlanIdYearly.Trim();
            
            // Abacatepay
            if (dto.AbacatepayPlanIdMonthly != null)
                plan.AbacatepayPlanIdMonthly = string.IsNullOrWhiteSpace(dto.AbacatepayPlanIdMonthly) 
                    ? null : dto.AbacatepayPlanIdMonthly.Trim();
            
            if (dto.AbacatepayPlanIdYearly != null)
                plan.AbacatepayPlanIdYearly = string.IsNullOrWhiteSpace(dto.AbacatepayPlanIdYearly) 
                    ? null : dto.AbacatepayPlanIdYearly.Trim();

            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Gateway IDs atualizados para plano: {Id}", id);

            return Ok(new { message = "IDs dos gateways atualizados com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar gateway IDs do plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar gateway IDs" });
        }
    }

    /// <summary>
    /// Exclui um plano
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            // Verificar se há assinaturas usando este plano
            var hasSubscriptions = await _context.Subscriptions
                .AnyAsync(s => s.SubscriptionPlanId == id);

            if (hasSubscriptions)
            {
                // Soft delete - apenas desativa
                plan.IsActive = false;
                plan.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Plano {Id} desativado (possui assinaturas vinculadas)", id);

                await _audit.LogAsync(HttpContext, "PLAN_DELETED", "SubscriptionPlan", id.ToString(), $"Soft delete (has subscriptions): {plan.Name}");

                return Ok(new { message = "Plano desativado (possui assinaturas vinculadas)" });
            }

            // Hard delete - remove do banco
            _context.Set<SubscriptionPlan>().Remove(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Plano excluído: {Name} (ID: {Id})", plan.Name, id);

            await _audit.LogAsync(HttpContext, "PLAN_DELETED", "SubscriptionPlan", id.ToString(), $"Hard delete: {plan.Name}");

            return Ok(new { message = "Plano excluído com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao excluir plano" });
        }
    }

    /// <summary>
    /// Ativa/Desativa um plano
    /// </summary>
    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        if (!IsSuperAdmin())
            return StatusCode(403, new { message = "Acesso negado. Requer perfil SUPER_ADMIN." });

        try
        {
            var plan = await _context.Set<SubscriptionPlan>().FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plano não encontrado" });

            plan.IsActive = !plan.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var status = plan.IsActive ? "ativado" : "desativado";
            _logger.LogInformation("Plano {Id} {Status}", id, status);

            return Ok(new { message = $"Plano {status} com sucesso", isActive = plan.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar status do plano {Id}", id);
            return StatusCode(500, new { message = "Erro ao atualizar plano" });
        }
    }

    /// <summary>
    /// Estatísticas dos planos
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _context.Set<SubscriptionPlan>()
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.PriceMonthly,
                    ActiveSubscriptions = _context.Subscriptions
                        .Count(s => s.SubscriptionPlanId == p.Id && s.Status == "ACTIVE"),
                    TrialingSubscriptions = _context.Subscriptions
                        .Count(s => s.SubscriptionPlanId == p.Id && s.Status == "TRIALING"),
                    HasStripe = p.StripePriceIdMonthly != null,
                    HasMercadoPago = p.MercadoPagoPlanIdMonthly != null,
                    HasAbacatepay = p.AbacatepayPlanIdMonthly != null
                })
                .ToListAsync();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas dos planos");
            return StatusCode(500, new { message = "Erro ao buscar estatísticas" });
        }
    }

    private bool IsSuperAdmin()
    {
        var role = HttpContext.Items["SaasAdminRole"] as string;
        return string.Equals(role, "SUPER_ADMIN", StringComparison.Ordinal);
    }
}
