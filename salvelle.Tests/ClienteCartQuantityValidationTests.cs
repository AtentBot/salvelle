using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Cart;
using Models;
using Controllers.Api;
using salvelle.Tests.Helpers;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Regressão do achado §3/§4 (quantidade negativa/inválida no carrinho): o portal
/// do cliente aceitava Quantity 0/negativa em AddToCart (a checagem de estoque
/// `StockQuantity < negativo` é falsa) e Delta arbitrário em UpdateQuantity,
/// gerando subtotal negativo. Estes testes travam os limites 1..99.
/// </summary>
public class ClienteCartQuantityValidationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ClienteCartApiController _controller;
    private readonly MarketplaceTestData _data;

    public ClienteCartQuantityValidationTests()
    {
        _db = TestHelpers.CreateInMemoryDb();
        _data = TestHelpers.SeedMarketplaceData(_db);
        _controller = new ClienteCartApiController(_db, TestHelpers.CreateMockLogger<ClienteCartApiController>());
        var customer = _db.Customers.First(c => c.Id == _data.CustomerId);
        TestHelpers.SetClienteCustomer(_controller, customer, _data.EstablishmentId);
    }

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(100)]
    public async Task AddToCart_QuantidadeForaDoIntervalo_RetornaBadRequest(int quantity)
    {
        var result = await _controller.AddToCart(new AddProductToCartDto
        {
            ProductId = _data.ProductId1,
            Quantity = quantity
        });

        Assert.IsType<BadRequestObjectResult>(result);

        // Nada é persistido no carrinho.
        var items = await _db.Set<CustomerCartItem>().CountAsync();
        Assert.Equal(0, items);
    }

    [Fact]
    public async Task AddToCart_QuantidadeValida_Persiste()
    {
        var result = await _controller.AddToCart(new AddProductToCartDto
        {
            ProductId = _data.ProductId1,
            Quantity = 3
        });

        Assert.IsType<OkObjectResult>(result);
        var item = await _db.Set<CustomerCartItem>().SingleAsync();
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task UpdateQuantity_DeltaEstouraTeto_RetornaBadRequest()
    {
        // Item começa com 1 unidade.
        await _controller.AddToCart(new AddProductToCartDto { ProductId = _data.ProductId1, Quantity = 1 });
        var item = await _db.Set<CustomerCartItem>().SingleAsync();

        var result = await _controller.UpdateQuantity(new UpdateCartItemDto
        {
            ItemId = item.Id,
            Delta = 999
        });

        Assert.IsType<BadRequestObjectResult>(result);
        // Quantidade permanece inalterada.
        var reloaded = await _db.Set<CustomerCartItem>().SingleAsync();
        Assert.Equal(1, reloaded.Quantity);
    }
}
