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

        // Resolver o carrinho e gravar o item ATOMICAMENTE.
        //
        // Dois cuidados que já custaram bugs em produção:
        //  1) O índice único é (CustomerId, EstablishmentId): pode já existir UMA linha para esta
        //     farmácia — inclusive CONVERTED de um pedido anterior. Inserir um carrinho novo sem
        //     checar isso viola o índice (23505) e quebra a recompra na mesma farmácia. Então
        //     reaproveitamos a linha da farmácia destino quando ela existir.
        //  2) NÃO carregamos os itens no change tracker (nada de Include(c => c.Items)) e limpamos
        //     itens antigos com ExecuteDelete (SQL direto). Se um DELETE de item antigo e o INSERT
        //     do item novo caíssem no mesmo SaveChanges pelo mesmo carrinho pai, o EF troca as
        //     identidades e emite um DELETE do item recém-criado (afeta 0 linhas -> 500).
        //  3) Tudo dentro de ExecuteInTransactionAsync: roda sob a execution strategy do EF
        //     (EnableRetryOnFailure) e é atômico — nada de estado meio-gravado se algo falhar.
        var (novoCartId, erro) = await _db.Database.ExecuteInTransactionAsync(async tx =>
        {
            var activeCart = await _db.CustomerCarts
                .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.Status == "ACTIVE");

            CustomerCart cart;
            var reaproveitou = false;

            if (activeCart != null && activeCart.EstablishmentId == request.EstablishmentId)
            {
                // Já há carrinho ativo nesta farmácia — apenas somar o item.
                cart = activeCart;
            }
            else
            {
                // Existe alguma linha (qualquer status) para a farmácia destino? Reaproveitá-la respeita o índice.
                var cartDaFarmacia = await _db.CustomerCarts
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value
                                              && c.EstablishmentId == request.EstablishmentId);

                if (cartDaFarmacia != null)
                {
                    // Reativa a linha existente (ex.: CONVERTED após pedido) e zera itens antigos.
                    await _db.CustomerCartItems.Where(i => i.CartId == cartDaFarmacia.Id).ExecuteDeleteAsync();
                    cartDaFarmacia.Status = "ACTIVE";
                    cart = cartDaFarmacia;
                    reaproveitou = true;
                }
                else if (activeCart != null)
                {
                    // Troca de farmácia sem linha pré-existente no destino — repontar o carrinho ativo.
                    await _db.CustomerCartItems.Where(i => i.CartId == activeCart.Id).ExecuteDeleteAsync();
                    activeCart.EstablishmentId = request.EstablishmentId;
                    cart = activeCart;
                    reaproveitou = true;
                }
                else
                {
                    // Cliente sem carrinho — criar novo.
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

                // Se havia um carrinho ativo de OUTRA farmácia e passamos a usar outra linha,
                // abandona o antigo para não deixar dois carrinhos ativos (quebraria o GetCart).
                if (activeCart != null && !ReferenceEquals(activeCart, cart) && activeCart.Status == "ACTIVE")
                {
                    await _db.CustomerCartItems.Where(i => i.CartId == activeCart.Id).ExecuteDeleteAsync();
                    activeCart.Status = "ABANDONED";
                }
            }

            // Somar em item existente ou inserir um novo. Consulta direta na tabela (não pela navegação):
            // num carrinho reaproveitado os itens acabaram de ser apagados, então existingItem será null.
            var existingItem = reaproveitou
                ? null
                : await _db.CustomerCartItems
                    .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + request.Quantity;
                if (newQty > 99)
                    // Sem commit -> a transação faz rollback (nada foi gravado ainda neste caminho).
                    return ((Guid?)null, "Quantidade máxima por item é 99.");
                existingItem.Quantity = newQty;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CustomerCartItems.Add(new CustomerCartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = product.Id,
                    // ProductName is computed via DisplayName
                    UnitPrice = product.CurrentPrice,
                    Quantity = request.Quantity,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return ((Guid?)cart.Id, (string?)null);
        });

        if (erro != null)
            return BadRequest(ApiResponse.ErrorResponse(erro));

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
