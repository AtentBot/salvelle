using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Pharmacy;

namespace Controllers.Api;

/// <summary>
/// API Controller para operações do Portal do Cliente
/// Rota: /api/cliente/*
/// </summary>
[ApiController]
[Route("api/cliente")]
public class ClienteApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClienteApiController> _logger;
    private readonly Service.CustomerFormulas.PricingService _pricingService;

    public ClienteApiController(
        AppDbContext context,
        ILogger<ClienteApiController> logger,
        Service.CustomerFormulas.PricingService pricingService)
    {
        _context = context;
        _logger = logger;
        _pricingService = pricingService;
    }

    private Customer? GetCurrentCustomer() => HttpContext.Items["Customer"] as Customer;
    private CustomerSession? GetCurrentSession() => HttpContext.Items["CustomerSession"] as CustomerSession;

    // ═══════════════════════════════════════════════════════════════════════════
    // SESSION
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Selecionar estabelecimento
    /// POST /api/cliente/session/select-establishment
    /// </summary>
    [HttpPost("session/select-establishment")]
    public async Task<IActionResult> SelectEstablishment([FromBody] SelectEstablishmentDto dto)
    {
        var session = GetCurrentSession();
        if (session == null)
            return Unauthorized(new { success = false, message = "Sessão não encontrada" });

        var establishment = await _context.Establishments.FindAsync(dto.EstablishmentId);
        if (establishment == null || !establishment.IsActive)
            return NotFound(new { success = false, message = "Estabelecimento não encontrado" });

        session.CurrentEstablishmentId = establishment.Id;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Estabelecimento selecionado", establishment = new { establishment.Id, establishment.NomeFantasia } });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ORDERS (apenas pedidos ativos - detalhes via ClienteOrdersApiController)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Listar pedidos do cliente (paginado, com filtro opcional de status)
    /// GET /api/cliente/orders?page=1&status=IN_PROGRESS
    /// O app trata IN_PROGRESS como agregado de PENDING/CONFIRMED/PREPARING.
    /// </summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] string? status = null)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        if (page < 1) page = 1;
        const int pageSize = 10;

        var query = _context.OnlineOrders.Where(o => o.CustomerId == customer.Id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status == "IN_PROGRESS")
            {
                query = query.Where(o => o.Status == "PENDING" || o.Status == "CONFIRMED" || o.Status == "PREPARING");
            }
            else
            {
                query = query.Where(o => o.Status == status);
            }
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                id = o.Id,
                orderNumber = o.OrderNumber,
                code = o.OrderNumber,
                status = o.Status,
                description = o.Items.Select(i => i.ProductName).FirstOrDefault(),
                itemCount = o.Items.Count,
                total = o.Total,
                createdAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, orders });
    }

    /// <summary>
    /// Listar pedidos ativos
    /// GET /api/cliente/orders/active
    /// </summary>
    [HttpGet("orders/active")]
    public async Task<IActionResult> GetActiveOrders()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        var orders = await _context.OnlineOrders
            .Include(o => o.Establishment)
            .Where(o => o.CustomerId == customer.Id &&
                       o.Status != "DELIVERED" &&
                       o.Status != "CANCELLED")
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new {
                id = o.Id,
                orderNumber = o.OrderNumber,
                status = o.Status,
                statusDisplay = o.Status == "PENDING" ? "Aguardando" :
                               o.Status == "CONFIRMED" ? "Confirmado" :
                               o.Status == "PREPARING" ? "Preparando" :
                               o.Status == "READY" ? "Pronto" : o.Status,
                description = "Fórmula personalizada",
                eta = "3 dias úteis",
                pharmacy = o.Establishment!.NomeFantasia,
                total = o.Total,
                createdAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, orders });
    }

    // NOTA: GET /api/cliente/orders/{id} está em ClienteOrdersApiController
    // para evitar duplicação de rotas

    // ═══════════════════════════════════════════════════════════════════════════
    // CART
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Obter carrinho atual
    /// GET /api/cliente/cart
    /// </summary>
    [HttpGet("cart")]
    public async Task<IActionResult> GetCart()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        var session = GetCurrentSession();

        var cart = await _context.CustomerCarts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.CustomerFormula)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id &&
                                     c.EstablishmentId == session!.CurrentEstablishmentId);

        if (cart == null)
            return Ok(new { success = true, cart = new { items = new List<object>(), total = 0 } });

        return Ok(new
        {
            success = true,
            cart = new
            {
                id = cart.Id,
                items = cart.Items?.Select(i => new {
                    id = i.Id,
                    productName = i.DisplayName,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    totalPrice = i.TotalPrice
                }),
                itemCount = cart.TotalItems,
                total = cart.TotalValue
            }
        });
    }

    /// <summary>
    /// Remover item do carrinho
    /// DELETE /api/cliente/cart/items/{id}
    /// </summary>
    [HttpDelete("cart/items/{id}")]
    public async Task<IActionResult> RemoveCartItem(Guid id)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized();

        var item = await _context.CustomerCartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == id && i.Cart!.CustomerId == customer.Id);

        if (item == null)
            return NotFound();

        _context.CustomerCartItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    /// <summary>
    /// Atualizar quantidade do item (via PATCH com id na URL)
    /// PATCH /api/cliente/cart/items/{id}
    /// </summary>
    [HttpPatch("cart/items/{id}")]
    public async Task<IActionResult> UpdateCartItem(Guid id, [FromBody] UpdateCartItemQuantityDto dto)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized();

        var item = await _context.CustomerCartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == id && i.Cart!.CustomerId == customer.Id);

        if (item == null)
            return NotFound();

        item.Quantity = dto.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, newTotal = item.TotalPrice });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COUPONS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Validar cupom
    /// POST /api/cliente/coupons/validate
    /// </summary>
    [HttpPost("coupons/validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponDto dto)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        var session = GetCurrentSession();

        var currentEstablishmentId = session?.CurrentEstablishmentId;
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code == dto.Code.ToUpper() && c.IsActive &&
                (c.EstablishmentId == null || c.EstablishmentId == currentEstablishmentId));

        if (coupon == null)
            return Ok(new { success = false, message = "Cupom não encontrado" });

        // Verificar validade
        if (coupon.ValidFrom > DateTime.UtcNow)
            return Ok(new { success = false, message = "Cupom ainda não está válido" });

        if (coupon.ValidUntil < DateTime.UtcNow)
            return Ok(new { success = false, message = "Cupom expirado" });

        // Verificar limite de uso total
        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses)
            return Ok(new { success = false, message = "Cupom esgotado" });

        // Verificar limite por cliente
        if (coupon.MaxUsesPerCustomer.HasValue)
        {
            var customerUsage = await _context.CouponUsages
                .CountAsync(u => u.CouponId == coupon.Id && u.CustomerId == customer.Id);

            if (customerUsage >= coupon.MaxUsesPerCustomer)
                return Ok(new { success = false, message = "Você já utilizou este cupom o máximo permitido" });
        }

        // Verificar primeira compra
        if (coupon.FirstPurchaseOnly)
        {
            var hasOrders = await _context.OnlineOrders
                .AnyAsync(o => o.CustomerId == customer.Id && o.Status != "CANCELLED");

            if (hasOrders)
                return Ok(new { success = false, message = "Cupom válido apenas para primeira compra" });
        }

        // Verificar valor mínimo
        if (coupon.MinOrderValue.HasValue && dto.Subtotal < coupon.MinOrderValue)
            return Ok(new { success = false, message = $"Pedido mínimo de R$ {coupon.MinOrderValue:N2}" });

        // Calcular desconto
        decimal discountAmount = 0;
        if (coupon.DiscountType == "PERCENTAGE" && coupon.DiscountPercentage.HasValue)
        {
            discountAmount = dto.Subtotal * (coupon.DiscountPercentage.Value / 100);
            if (coupon.MaxDiscountValue.HasValue && discountAmount > coupon.MaxDiscountValue)
                discountAmount = coupon.MaxDiscountValue.Value;
        }
        else if (coupon.DiscountValue.HasValue)
        {
            discountAmount = Math.Min(coupon.DiscountValue.Value, dto.Subtotal);
        }

        return Ok(new
        {
            success = true,
            coupon = new
            {
                id = coupon.Id,
                code = coupon.Code,
                discountDisplay = coupon.DiscountDisplay
            },
            discountAmount
        });
    }

    /// <summary>
    /// Listar cupons disponíveis para o cliente
    /// GET /api/cliente/coupons/available
    /// </summary>
    [HttpGet("coupons/available")]
    public async Task<IActionResult> GetAvailableCoupons()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        var session = GetCurrentSession();

        var coupons = await _context.Coupons
            .Where(c => c.IsActive &&
                       c.ValidFrom <= DateTime.UtcNow &&
                       c.ValidUntil >= DateTime.UtcNow &&
                       (c.EstablishmentId == null || c.EstablishmentId == session!.CurrentEstablishmentId) &&
                       (c.MaxUses == null || c.UsedCount < c.MaxUses))
            .OrderByDescending(c => c.DiscountPercentage)
            .Select(c => new {
                id = c.Id,
                code = c.Code,
                description = c.Description,
                discountDisplay = c.DiscountType == "PERCENTAGE"
                    ? $"{c.DiscountPercentage}% OFF"
                    : $"R$ {c.DiscountValue:N2} OFF",
                minOrderValue = c.MinOrderValue,
                validUntil = c.ValidUntil,
                daysRemaining = (c.ValidUntil - DateTime.UtcNow).Days
            })
            .ToListAsync();

        return Ok(new { success = true, coupons });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PROFILE
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Estatísticas do perfil
    /// GET /api/cliente/profile/stats
    /// </summary>
    [HttpGet("profile/stats")]
    public async Task<IActionResult> GetProfileStats()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        var session = GetCurrentSession();

        var ordersCount = await _context.OnlineOrders
            .CountAsync(o => o.CustomerId == customer.Id);

        var formulasCount = await _context.CustomerFormulas
            .CountAsync(f => f.CustomerId == customer.Id);

        var couponsCount = await _context.Coupons
            .CountAsync(c => c.IsActive &&
                           c.ValidFrom <= DateTime.UtcNow &&
                           c.ValidUntil >= DateTime.UtcNow &&
                           (c.EstablishmentId == null || c.EstablishmentId == session!.CurrentEstablishmentId) &&
                           (c.MaxUses == null || c.UsedCount < c.MaxUses));

        return Ok(new
        {
            success = true,
            ordersCount,
            formulasCount,
            couponsCount
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AUTH (Logout está em ClienteAuthApiController)
    // ═══════════════════════════════════════════════════════════════════════════

    // NOTA: POST /api/cliente/auth/logout está em ClienteAuthApiController

    // ═══════════════════════════════════════════════════════════════════════════
    // SEARCH
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Buscar produtos
    /// GET /api/cliente/search
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return BadRequest(new { success = false, message = "Selecione uma farmácia" });

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new { success = true, results = new List<object>() });

        if (limit <= 0 || limit > 50) limit = 20;
        var term = q.Trim().ToLower();

        var results = await _context.RawMaterials
            .Where(r => r.EstablishmentId == session.CurrentEstablishmentId &&
                       r.IsActive &&
                       (r.Name.ToLower().Contains(term) ||
                        (r.Synonyms != null && r.Synonyms.ToLower().Contains(term)) ||
                        (r.DcbCode != null && r.DcbCode.ToLower().Contains(term))))
            .OrderByDescending(r => r.Name.ToLower().StartsWith(term))
            .ThenByDescending(r => r.Popularity)
            .ThenBy(r => r.Name)
            .Take(limit)
            .Select(r => new {
                id = r.Id,
                name = r.Name,
                category = r.Category,
                unit = r.Unit,
                dcbCode = r.DcbCode,
                inStock = r.CurrentStock > 0,
                price = r.BasePrice ?? r.LastPurchasePrice ?? 0
            })
            .ToListAsync();

        return Ok(new { success = true, results });
    }

    /// <summary>
    /// Calcular preço de uma fórmula personalizada (estabelecimento da sessão).
    /// POST /api/cliente/formula/calculate
    /// </summary>
    [HttpPost("formula/calculate")]
    public async Task<IActionResult> CalculateFormula([FromBody] CustomerFormulaCalcRequest request)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return Unauthorized(new { success = false });

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return BadRequest(new { success = false, message = "Selecione uma farmácia" });

        if (request?.Ingredients == null || request.Ingredients.Count == 0)
            return BadRequest(new { success = false, message = "Lista de ingredientes é obrigatória" });

        var quantity = request.ProductQuantity > 0 ? request.ProductQuantity : 1;

        var inputs = request.Ingredients.Select(i => new Service.CustomerFormulas.FormulaIngredientInput
        {
            RawMaterialId = i.RawMaterialId,
            Name = i.Name,
            Quantity = i.Quantity,
            Unit = string.IsNullOrWhiteSpace(i.Unit) ? "mg" : i.Unit
        }).ToList();

        var result = await _pricingService.CalculateFormulaWithIngredientsAsync(
            inputs,
            quantity,
            session.CurrentEstablishmentId.Value,
            string.IsNullOrWhiteSpace(request.ProductType) ? "Cápsula" : request.ProductType);

        return Ok(new
        {
            success = true,
            totalPrice = result.SuggestedPrice,
            breakdown = new
            {
                ingredientsCost = result.TotalIngredientsCost,
                manipulationCost = result.ManipulationCost,
                packagingCost = result.PackagingCost,
                totalCost = result.TotalCost,
                profitMargin = result.ProfitMargin
            },
            confidence = result.AverageConfidence,
            ingredients = result.Ingredients.Select(it => new
            {
                rawMaterialId = it.RawMaterialId,
                name = it.Name,
                quantity = it.Quantity,
                unit = it.Unit,
                unitPrice = it.UnitPrice,
                totalPrice = it.TotalPrice,
                source = it.Source.ToString(),
                warning = it.Warning
            }),
            warnings = result.Warnings
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ESTABLISHMENTS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Listar estabelecimentos disponíveis
    /// GET /api/cliente/establishments
    /// </summary>
    [HttpGet("establishments")]
    public async Task<IActionResult> GetEstablishments([FromQuery] string? search, [FromQuery] string? city)
    {
        var query = _context.Establishments
            .Where(e => e.IsActive && e.OnboardingCompleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.NomeFantasia.ToLower().Contains(search.ToLower()) ||
                (e.City != null && e.City.ToLower().Contains(search.ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(e => e.City == city);
        }

        var establishments = await query
            .OrderBy(e => e.NomeFantasia)
            .Take(50)
            .Select(e => new {
                id = e.Id,
                name = e.NomeFantasia,
                address = $"{e.Street}, {e.Number} - {e.Neighborhood}",
                city = e.City,
                state = e.State,
                phone = e.Phone
            })
            .ToListAsync();

        return Ok(new { success = true, establishments });
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DTOs LOCAIS (específicos deste controller)
// ═══════════════════════════════════════════════════════════════════════════

public class SelectEstablishmentDto
{
    public Guid EstablishmentId { get; set; }
}

/// <summary>
/// DTO para atualizar quantidade via PATCH (id na URL)
/// DIFERENTE de DTOs.Cart.UpdateCartItemDto que usa ItemId + Delta
/// </summary>
public class UpdateCartItemQuantityDto
{
    public int Quantity { get; set; }
}

public class ValidateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
}

public class CustomerFormulaCalcRequest
{
    public string? ProductType { get; set; }
    public int ProductQuantity { get; set; } = 1;
    public List<CustomerFormulaCalcIngredient> Ingredients { get; set; } = new();
}

public class CustomerFormulaCalcIngredient
{
    public Guid? RawMaterialId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}
