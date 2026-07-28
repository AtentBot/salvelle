using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/cart")]
public class MobileCartController : ControllerBase
{
    private readonly AppDbContext _db;

    public MobileCartController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Obter carrinho do cliente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MobileCartDto>>> GetCart()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var cart = await _db.CustomerCarts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.Status == "ACTIVE");

        if (cart == null)
        {
            return Ok(ApiResponse<MobileCartDto>.SuccessResponse(new MobileCartDto
            {
                Items = new List<MobileCartItemDto>(),
                Subtotal = 0,
                Total = 0,
                ItemCount = 0
            }));
        }

        var pharmacy = await _db.Establishments.FindAsync(cart.EstablishmentId);

        var cartDto = new MobileCartDto
        {
            Id = cart.Id,
            EstablishmentId = cart.EstablishmentId,
            PharmacyName = pharmacy?.NomeFantasia ?? "",
            PharmacyLogoUrl = pharmacy?.LogoUrl,
            Items = cart.Items.Select(i => new MobileCartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId ?? Guid.Empty,
                ProductName = i.Product?.Name ?? i.DisplayName,
                ProductImageUrl = i.Product?.ImageUrl,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.UnitPrice * i.Quantity,
                Notes = i.Notes,
                InStock = i.Product?.StockQuantity > 0
            }).ToList(),
            Subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity),
            DeliveryFee = 0, // TODO: calcular taxa de entrega
            ItemCount = cart.Items.Sum(i => i.Quantity)
        };
        cartDto.Total = cartDto.Subtotal + cartDto.DeliveryFee;

        return Ok(ApiResponse<MobileCartDto>.SuccessResponse(cartDto));
    }

    /// <summary>
    /// Adicionar item ao carrinho
    /// </summary>
    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<MobileCartDto>>> AddItem([FromBody] AddToCartRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        if (request.Quantity < 1 || request.Quantity > 99)
            return BadRequest(ApiResponse.ErrorResponse("Quantidade inválida. Informe entre 1 e 99."));

        var product = await _db.CatalogProducts
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive && p.IsMarketplaceVisible);

        if (product == null)
            return NotFound(ApiResponse.ErrorResponse("Produto não encontrado"));

        if (product.StockQuantity <= 0)
            return BadRequest(ApiResponse.ErrorResponse("Produto fora de estoque"));

        // Validar farmácia destino antes de trocar o carrinho
        var pharmacy = await _db.Establishments
            .FirstOrDefaultAsync(e => e.Id == request.EstablishmentId && e.IsMarketplaceActive && e.AcceptingOrders);
        if (pharmacy == null)
            return BadRequest(ApiResponse.ErrorResponse("Farmácia não está aceitando pedidos no momento"));

        // Buscar ou criar carrinho
        var cart = await _db.CustomerCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.Status == "ACTIVE");

        if (cart != null && cart.EstablishmentId != request.EstablishmentId)
        {
            // Carrinho de outra farmácia — limpar
            _db.CustomerCartItems.RemoveRange(cart.Items);
            cart.EstablishmentId = request.EstablishmentId;
            cart.Items.Clear();
        }

        if (cart == null)
        {
            cart = new CustomerCart
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.Value,
                EstablishmentId = request.EstablishmentId,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            _db.CustomerCarts.Add(cart);
        }

        // Verificar se item já existe
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            var newQty = existingItem.Quantity + request.Quantity;
            if (newQty > 99)
                return BadRequest(ApiResponse.ErrorResponse("Quantidade máxima por item é 99."));
            existingItem.Quantity = newQty;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var item = new CustomerCartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                // ProductName is computed via DisplayName
                UnitPrice = product.CurrentPrice,
                Quantity = request.Quantity,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };
            cart.Items.Add(item);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetCart();
    }

    /// <summary>
    /// Atualizar quantidade de item no carrinho
    /// </summary>
    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<MobileCartDto>>> UpdateItem(Guid itemId, [FromBody] UpdateCartItemRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var item = await _db.CustomerCartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart!.CustomerId == customerId.Value);

        if (item == null)
            return NotFound(ApiResponse.ErrorResponse("Item não encontrado"));

        if (request.Quantity < 1 || request.Quantity > 99)
            return BadRequest(ApiResponse.ErrorResponse("Quantidade inválida. Informe entre 1 e 99."));

        item.Quantity = request.Quantity;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetCart();
    }

    /// <summary>
    /// Remover item do carrinho
    /// </summary>
    [HttpDelete("items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<MobileCartDto>>> RemoveItem(Guid itemId)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var item = await _db.CustomerCartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart!.CustomerId == customerId.Value);

        if (item == null)
            return NotFound(ApiResponse.ErrorResponse("Item não encontrado"));

        _db.CustomerCartItems.Remove(item);
        await _db.SaveChangesAsync();

        return await GetCart();
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }
}
