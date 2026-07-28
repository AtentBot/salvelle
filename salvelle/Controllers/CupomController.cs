using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;
using Models.Pharmacy;

namespace Controllers;

/// <summary>
/// Controller MVC para gerenciamento de Cupons pela Farmácia
/// Rota: /Cupons/*
/// </summary>
[Route("Cupons")]
public class CupomController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<CupomController> _logger;

    public CupomController(AppDbContext context, ILogger<CupomController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee()
    {
        return HttpContext.Items["Employee"] as Employee;
    }

    /// <summary>
    /// Lista de cupons
    /// GET /Cupons
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var coupons = await _context.Coupons
            .Where(c => c.EstablishmentId == null || c.EstablishmentId == employee.EstablishmentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewData["Employee"] = employee;
        ViewBag.Coupons = coupons;
        return View("Index");
    }

    /// <summary>
    /// Criar novo cupom
    /// GET /Cupons/Criar
    /// </summary>
    [HttpGet("Criar")]
    public IActionResult Criar()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewData["Employee"] = employee;
        return View("Criar");
    }

    /// <summary>
    /// Salvar novo cupom
    /// POST /Cupons/Criar
    /// </summary>
    [HttpPost("Criar")]
    public async Task<IActionResult> Criar([FromForm] CriarCupomDto dto)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            ViewData["Employee"] = employee;
            TempData["Error"] = "Preencha todos os campos obrigatórios";
            return View("Criar");
        }

        // Verificar se código já existe
        var existingCode = await _context.Coupons
            .AnyAsync(c => c.Code == dto.Code.ToUpper());
        
        if (existingCode)
        {
            TempData["Error"] = "Já existe um cupom com este código";
            ViewData["Employee"] = employee;
            return View("Criar");
        }

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            EstablishmentId = employee.EstablishmentId,
            Code = dto.Code.ToUpper(),
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
            CreatedByEmployeeId = employee.Id
        };

        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cupom criado com sucesso!";
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Editar cupom
    /// GET /Cupons/Editar/{id}
    /// </summary>
    [HttpGet("Editar/{id}")]
    public async Task<IActionResult> Editar(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == id);

        if (coupon == null)
            return NotFound();

        ViewData["Employee"] = employee;
        ViewBag.Coupon = coupon;
        return View("Editar");
    }

    /// <summary>
    /// Salvar edição
    /// POST /Cupons/Editar/{id}
    /// </summary>
    [HttpPost("Editar/{id}")]
    public async Task<IActionResult> Editar(Guid id, [FromForm] CriarCupomDto dto)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);
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
        coupon.UpdatedByEmployeeId = employee.Id;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cupom atualizado com sucesso!";
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Ativar/Desativar cupom
    /// POST /Cupons/Toggle/{id}
    /// </summary>
    [HttpPost("Toggle/{id}")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return Unauthorized();

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);
        if (coupon == null)
            return NotFound();

        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;
        coupon.UpdatedByEmployeeId = employee.Id;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, isActive = coupon.IsActive });
    }

    /// <summary>
    /// Excluir cupom
    /// POST /Cupons/Excluir/{id}
    /// </summary>
    [HttpPost("Excluir/{id}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return Unauthorized();

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id);
        if (coupon == null)
            return NotFound();

        // Verificar se foi usado
        if (coupon.UsedCount > 0)
        {
            // Apenas desativar se já foi usado
            coupon.IsActive = false;
            coupon.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.Coupons.Remove(coupon);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cupom removido com sucesso!";
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Relatório de uso
    /// GET /Cupons/Relatorio/{id}
    /// </summary>
    [HttpGet("Relatorio/{id}")]
    public async Task<IActionResult> Relatorio(Guid id)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var coupon = await _context.Coupons
            .Include(c => c.Usages!)
                .ThenInclude(u => u.Customer)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (coupon == null)
            return NotFound();

        ViewData["Employee"] = employee;
        ViewBag.Coupon = coupon;
        ViewBag.Usages = coupon.Usages?.OrderByDescending(u => u.UsedAt).ToList();
        return View("Relatorio");
    }
}

// DTO para criar/editar cupom
public class CriarCupomDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "PERCENTAGE";
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(30);
    public int? MaxUses { get; set; }
    public int? MaxUsesPerCustomer { get; set; }
    public bool FirstPurchaseOnly { get; set; }
}
