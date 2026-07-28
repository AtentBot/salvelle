using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Controllers;

/// <summary>
/// Controller MVC para Farmácia gerenciar Cupons de Desconto
/// Rota: /CuponsDesconto/*
/// NOTA: Diferente do CupomController (API) que gera cupons não-fiscais para impressão
/// </summary>
public class CuponsDescontoController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<CuponsDescontoController> _logger;

    public CuponsDescontoController(AppDbContext context, ILogger<CuponsDescontoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;
        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    private Guid? GetEmployeeId()
    {
        if (HttpContext.Items.TryGetValue("EmployeeId", out var empId) && empId is Guid id)
            return id;
        return null;
    }

    /// <summary>
    /// Lista todos os cupons de desconto
    /// GET /CuponsDesconto
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var establishmentId = GetEstablishmentId();

        var coupons = await _context.Coupons
            .Where(c => c.EstablishmentId == establishmentId || c.EstablishmentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Coupons = coupons;
        return View();
    }

    /// <summary>
    /// Formulário de criação
    /// GET /CuponsDesconto/Criar
    /// </summary>
    [HttpGet]
    public IActionResult Criar()
    {
        return View();
    }

    /// <summary>
    /// Criar novo cupom
    /// POST /CuponsDesconto/Criar
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(CriarCupomDescontoDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            // Verificar código único
            var exists = await _context.Coupons
                .AnyAsync(c => c.Code == dto.Code.ToUpper());

            if (exists)
            {
                TempData["Error"] = "Já existe um cupom com este código";
                return View(dto);
            }

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = dto.Code.ToUpper().Trim(),
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountPercentage = dto.DiscountType == "PERCENTAGE" ? dto.DiscountValue : null,
                DiscountValue = dto.DiscountType == "FIXED_VALUE" ? dto.DiscountValue : null,
                MinOrderValue = dto.MinOrderValue,
                MaxDiscountValue = dto.MaxDiscountValue,
                ValidFrom = dto.ValidFrom,
                ValidUntil = dto.ValidUntil,
                MaxUses = dto.MaxUses,
                MaxUsesPerCustomer = dto.MaxUsesPerCustomer,
                FirstPurchaseOnly = dto.FirstPurchaseOnly,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = GetEmployeeId(),
                UpdatedAt = DateTime.UtcNow
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cupom {coupon.Code} criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cupom");
            TempData["Error"] = "Erro ao criar cupom";
            return View(dto);
        }
    }

    /// <summary>
    /// Formulário de edição
    /// GET /CuponsDesconto/Editar/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);
        if (coupon == null)
            return NotFound();

        ViewBag.Coupon = coupon;
        return View("Criar");
    }

    /// <summary>
    /// Salvar edição
    /// POST /CuponsDesconto/Editar/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Guid id, CriarCupomDescontoDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);
            if (coupon == null)
                return NotFound();

            coupon.Description = dto.Description;
            coupon.DiscountType = dto.DiscountType;
            coupon.DiscountPercentage = dto.DiscountType == "PERCENTAGE" ? dto.DiscountValue : null;
            coupon.DiscountValue = dto.DiscountType == "FIXED_VALUE" ? dto.DiscountValue : null;
            coupon.MinOrderValue = dto.MinOrderValue;
            coupon.MaxDiscountValue = dto.MaxDiscountValue;
            coupon.ValidFrom = dto.ValidFrom;
            coupon.ValidUntil = dto.ValidUntil;
            coupon.MaxUses = dto.MaxUses;
            coupon.MaxUsesPerCustomer = dto.MaxUsesPerCustomer;
            coupon.FirstPurchaseOnly = dto.FirstPurchaseOnly;
            coupon.UpdatedAt = DateTime.UtcNow;
            coupon.UpdatedByEmployeeId = GetEmployeeId();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cupom atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar cupom {CouponId}", id);
            TempData["Error"] = "Erro ao atualizar cupom";
            return RedirectToAction(nameof(Editar), new { id });
        }
    }

    /// <summary>
    /// Ativar/Desativar cupom
    /// POST /CuponsDesconto/Toggle/{id}
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);
            if (coupon == null)
                return NotFound(new { success = false });

            coupon.IsActive = !coupon.IsActive;
            coupon.UpdatedAt = DateTime.UtcNow;
            coupon.UpdatedByEmployeeId = GetEmployeeId();

            await _context.SaveChangesAsync();

            return Ok(new { success = true, isActive = coupon.IsActive });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar cupom {CouponId}", id);
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// Excluir cupom
    /// POST /CuponsDesconto/Excluir/{id}
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Excluir(Guid id)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var coupon = await _context.Coupons
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);

            if (coupon == null)
                return NotFound();

            // Se já foi usado, apenas desativa
            if (coupon.UsedCount > 0 || coupon.Usages?.Any() == true)
            {
                coupon.IsActive = false;
                coupon.UpdatedAt = DateTime.UtcNow;
                TempData["Success"] = "Cupom desativado (possui histórico de uso)";
            }
            else
            {
                _context.Coupons.Remove(coupon);
                TempData["Success"] = "Cupom excluído com sucesso!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir cupom {CouponId}", id);
            TempData["Error"] = "Erro ao excluir cupom";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Relatório de uso do cupom
    /// GET /CuponsDesconto/Relatorio/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Relatorio(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);

        if (coupon == null)
            return NotFound();

        var usages = await _context.CouponUsages
            .Include(u => u.Customer)
            .Where(u => u.CouponId == id)
            .OrderByDescending(u => u.UsedAt)
            .ToListAsync();

        ViewBag.Coupon = coupon;
        ViewBag.Usages = usages;

        return View();
    }
}

/// <summary>
/// DTO para criar/editar cupom de desconto
/// </summary>
public class CriarCupomDescontoDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE ou FIXED_VALUE
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public DateTime ValidFrom { get; set; } = Helpers.BrazilTime.Now;
    public DateTime ValidUntil { get; set; } = Helpers.BrazilTime.Now.AddDays(30);
    public int? MaxUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public bool FirstPurchaseOnly { get; set; }
}