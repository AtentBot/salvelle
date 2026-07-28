using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.CustomerFormulas;
using Models.Cart;
using Service.CustomerFormulas;

namespace Controllers;

[Route("api/cart")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CustomFormulaService _formulaService;
    private readonly ILogger<CartController> _logger;

    public CartController(
        AppDbContext context,
        CustomFormulaService formulaService,
        ILogger<CartController> logger)
    {
        _context = context;
        _formulaService = formulaService;
        _logger = logger;
    }

    /// <summary>
    /// POST api/cart/add-custom-formula
    /// Adicionar fórmula personalizada ao carrinho (SEM esperar aprovação)
    /// </summary>
    [HttpPost("add-custom-formula")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddCustomFormulaToCart(
        [FromBody] AddToCartDto dto)
    {
        try
        {
            // 1. Buscar CustomerFormula
            var formula = await _context.CustomerFormulas
                .Include(cf => cf.ProductType)
                .Include(cf => cf.ProductSubType)
                .FirstOrDefaultAsync(cf => cf.Id == dto.CustomerFormulaId);

            if (formula == null)
                return NotFound(ApiResponse<CartDto>.ErrorResponse("Fórmula não encontrada"));

            // IMPORTANTE: NÃO validar se está aprovada!
            // Cliente pode adicionar mesmo sem aprovação
            if (formula.Status != "AGUARDANDO_COMPRA")
                return BadRequest(ApiResponse<CartDto>.ErrorResponse(
                    "Fórmula já foi adicionada ao carrinho"));

            // 2. Obter ou criar token de sessão
            var sessionToken = GetOrCreateSessionToken();

            // 3. Criar CartItem
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                SessionToken = sessionToken,
                CustomerId = GetCurrentCustomerId(), // Pode ser null
                EstablishmentId = formula.EstablishmentId,
                ItemType = "CUSTOM_FORMULA",
                ReferenceId = formula.Id,
                Name = $"{formula.ProductSubType?.Name} {formula.Quantity}{formula.Unit}",
                Description = "Fórmula personalizada - Sujeita a aprovação farmacêutica",
                Quantity = 1, // Sempre 1 para fórmulas
                UnitPrice = formula.EstimatedPrice ?? 0,
                TotalPrice = formula.EstimatedPrice ?? 0,
                RequiresPrescription = false, // Será validado depois
                IsControlled = formula.IsControlledSubstance,
                IsCustomFormula = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.CartItems.Add(cartItem);

            // 4. Atualizar status da fórmula
            formula.Status = "NO_CARRINHO";
            formula.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 5. Retornar carrinho atualizado
            var cart = await GetCurrentCartAsync(sessionToken);

            _logger.LogInformation(
                "Fórmula {Code} adicionada ao carrinho",
                formula.Code);

            return Ok(ApiResponse<CartDto>.SuccessResponse(
                cart,
                "Fórmula adicionada ao carrinho com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar fórmula ao carrinho");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                "Erro ao adicionar fórmula ao carrinho"));
        }
    }

    /// <summary>
    /// POST api/cart/add-product
    /// Adicionar produto regular ao carrinho
    /// </summary>
    [HttpPost("add-product")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddProductToCart(
        [FromBody] AddProductToCartDto dto)
    {
        try
        {
            if (dto.Quantity < 1 || dto.Quantity > 99)
                return BadRequest(ApiResponse<CartDto>.ErrorResponse("Quantidade inválida"));

            // Buscar produto do banco — nunca confiar no preço enviado pelo cliente
            var product = await _context.CatalogProducts
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId
                                       && p.EstablishmentId == dto.EstablishmentId
                                       && p.IsActive);

            if (product == null)
                return NotFound(ApiResponse<CartDto>.ErrorResponse("Produto não encontrado"));

            var sessionToken = GetOrCreateSessionToken();

            // Verificar se produto já está no carrinho
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.SessionToken == sessionToken
                                        && ci.ItemType == "PRODUTO"
                                        && ci.ReferenceId == dto.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    CustomerId = GetCurrentCustomerId(),
                    EstablishmentId = dto.EstablishmentId,
                    ItemType = "PRODUTO",
                    ReferenceId = dto.ProductId,
                    Name = product.Name,
                    Description = product.ShortDescription ?? product.Description,
                    Quantity = dto.Quantity,
                    UnitPrice = product.CurrentPrice,
                    TotalPrice = product.CurrentPrice * dto.Quantity,
                    RequiresPrescription = dto.RequiresPrescription,
                    IsControlled = dto.IsControlled,
                    IsCustomFormula = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            var cart = await GetCurrentCartAsync(sessionToken);

            return Ok(ApiResponse<CartDto>.SuccessResponse(
                cart,
                "Produto adicionado ao carrinho"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar produto ao carrinho");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                "Erro ao adicionar produto ao carrinho"));
        }
    }

    /// <summary>
    /// GET api/cart
    /// Obter carrinho atual
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
    {
        try
        {
            var sessionToken = GetSessionToken();

            if (string.IsNullOrEmpty(sessionToken))
                return Ok(ApiResponse<CartDto>.SuccessResponse(new CartDto()));

            var cart = await GetCurrentCartAsync(sessionToken);

            return Ok(ApiResponse<CartDto>.SuccessResponse(cart));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar carrinho");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse("Erro ao buscar carrinho"));
        }
    }

    /// <summary>
    /// PUT api/cart/item/{id}/quantity
    /// Atualizar quantidade de um item
    /// </summary>
    [HttpPut("item/{id}/quantity")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateItemQuantity(
        Guid id,
        [FromBody] UpdateQuantityDto dto)
    {
        try
        {
            var sessionToken = GetSessionToken();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.SessionToken == sessionToken);

            if (item == null)
                return NotFound(ApiResponse<CartDto>.ErrorResponse("Item não encontrado"));

            if (item.IsCustomFormula)
                return BadRequest(ApiResponse<CartDto>.ErrorResponse(
                    "Não é possível alterar quantidade de fórmulas personalizadas"));

            if (dto.Quantity <= 0 || dto.Quantity > 99)
                return BadRequest(ApiResponse<CartDto>.ErrorResponse("Quantidade inválida"));

            item.Quantity = dto.Quantity;
            item.TotalPrice = item.UnitPrice * item.Quantity;

            await _context.SaveChangesAsync();

            var cart = await GetCurrentCartAsync(sessionToken!);

            return Ok(ApiResponse<CartDto>.SuccessResponse(cart, "Quantidade atualizada"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar quantidade");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse(
                "Erro ao atualizar quantidade"));
        }
    }

    /// <summary>
    /// DELETE api/cart/item/{id}
    /// Remover item do carrinho
    /// </summary>
    [HttpDelete("item/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveItem(Guid id)
    {
        try
        {
            var sessionToken = GetSessionToken();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.SessionToken == sessionToken);

            if (item == null)
                return NotFound(ApiResponse<CartDto>.ErrorResponse("Item não encontrado"));

            // Se for fórmula personalizada, voltar status para AGUARDANDO_COMPRA
            if (item.IsCustomFormula && item.ReferenceId.HasValue)
            {
                var formula = await _context.CustomerFormulas.FindAsync(item.ReferenceId.Value);
                if (formula != null && formula.Status == "NO_CARRINHO")
                {
                    formula.Status = "AGUARDANDO_COMPRA";
                    formula.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            var cart = await GetCurrentCartAsync(sessionToken!);

            return Ok(ApiResponse<CartDto>.SuccessResponse(cart, "Item removido do carrinho"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse("Erro ao remover item"));
        }
    }

    /// <summary>
    /// DELETE api/cart/clear
    /// Limpar carrinho
    /// </summary>
    [HttpDelete("clear")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ClearCart()
    {
        try
        {
            var sessionToken = GetSessionToken();

            if (string.IsNullOrEmpty(sessionToken))
                return Ok(ApiResponse<bool>.SuccessResponse(true));

            var items = await _context.CartItems
                .Where(ci => ci.SessionToken == sessionToken)
                .ToListAsync();

            // Reverter status de fórmulas personalizadas
            foreach (var item in items.Where(i => i.IsCustomFormula && i.ReferenceId.HasValue))
            {
                var formula = await _context.CustomerFormulas.FindAsync(item.ReferenceId.Value);
                if (formula != null && formula.Status == "NO_CARRINHO")
                {
                    formula.Status = "AGUARDANDO_COMPRA";
                    formula.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Carrinho limpo"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao limpar carrinho");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Erro ao limpar carrinho"));
        }
    }

    /// <summary>
    /// GET api/cart/count
    /// Obter quantidade de itens no carrinho
    /// </summary>
    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<int>>> GetCartItemCount()
    {
        try
        {
            var sessionToken = GetSessionToken();

            if (string.IsNullOrEmpty(sessionToken))
                return Ok(ApiResponse<int>.SuccessResponse(0));

            var count = await _context.CartItems
                .Where(ci => ci.SessionToken == sessionToken)
                .SumAsync(ci => ci.Quantity);

            return Ok(ApiResponse<int>.SuccessResponse(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao contar itens");
            return StatusCode(500, ApiResponse<int>.ErrorResponse("Erro ao contar itens"));
        }
    }

    // ==================== MÉTODOS AUXILIARES ====================

    private string? GetSessionToken()
    {
        return Request.Cookies["cart_session"];
    }

    private string GetOrCreateSessionToken()
    {
        var token = GetSessionToken();

        if (string.IsNullOrEmpty(token))
        {
            token = Guid.NewGuid().ToString();
            Response.Cookies.Append("cart_session", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        return token;
    }

    private Guid? GetCurrentCustomerId()
    {
        // TODO: Implementar lógica para obter CustomerId do usuário autenticado
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("customerId");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var customerId))
            return customerId;

        return null;
    }

    private async Task<CartDto> GetCurrentCartAsync(string sessionToken)
    {
        var items = await _context.CartItems
            .Where(ci => ci.SessionToken == sessionToken
                      && (ci.ExpiresAt == null || ci.ExpiresAt > DateTime.UtcNow))
            .OrderBy(ci => ci.CreatedAt)
            .ToListAsync();

        var cartDto = new CartDto
        {
            Items = items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ItemType = i.ItemType,
                ReferenceId = i.ReferenceId,
                Name = i.Name,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                RequiresPrescription = i.RequiresPrescription,
                IsControlled = i.IsControlled,
                IsCustomFormula = i.IsCustomFormula
            }).ToList(),
            Subtotal = items.Sum(i => i.TotalPrice),
            ItemCount = items.Sum(i => i.Quantity),
            HasCustomFormulas = items.Any(i => i.IsCustomFormula),
            HasControlledItems = items.Any(i => i.IsControlled)
        };

        return cartDto;
    }
}

// DTOs
public class AddProductToCartDto
{
    public Guid ProductId { get; set; }
    public Guid EstablishmentId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? ProductDescription { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsControlled { get; set; }
}

public class UpdateQuantityDto
{
    public int Quantity { get; set; }
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public int ItemCount { get; set; }
    public bool HasCustomFormulas { get; set; }
    public bool HasControlledItems { get; set; }
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public string ItemType { get; set; } = default!;
    public Guid? ReferenceId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsControlled { get; set; }
    public bool IsCustomFormula { get; set; }
}