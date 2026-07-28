using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Marketplace;
using Service.Marketplace;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Testes do fluxo completo de pedido no marketplace:
/// Carrinho → Pedido → Confirmação → Preparação → Pronto → Entregue
/// </summary>
public class OrderWorkflowTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CommissionService _commissionService;
    private readonly Guid _establishmentId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public OrderWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _commissionService = new CommissionService(_db);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _db.Establishments.Add(new Establishment
        {
            Id = _establishmentId,
            NomeFantasia = "Farmácia Teste",
            RazaoSocial = "Teste LTDA",
            Cnpj = "12345678000190",
            PasswordHash = "test-hash",
            IsActive = true,
            IsMarketplaceActive = true,
            AcceptingOrders = true,
            MinOrderAmount = 10m,
            AverageDeliveryMinutes = 45
        });

        _db.CatalogProducts.Add(new CatalogProduct
        {
            Id = _productId,
            EstablishmentId = _establishmentId,
            Name = "Vitamina C 1000mg",
            Price = 29.90m,
            StockQuantity = 100,
            IsActive = true,
            IsMarketplaceVisible = true
        });

        _db.Customers.Add(new Customer
        {
            Id = _customerId,
            FullName = "João Silva",
            Email = "joao@test.com",
            Phone = "11999999999",
            Status = "ATIVO",
            ConsentDataProcessing = true,
            ConsentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateCart_AddsItemToCart()
    {
        var cart = new CustomerCart
        {
            Id = Guid.NewGuid(),
            CustomerId = _customerId,
            EstablishmentId = _establishmentId,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.CustomerCarts.Add(cart);

        var item = new CustomerCartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = _productId,
            UnitPrice = 29.90m,
            Quantity = 3,
            CreatedAt = DateTime.UtcNow
        };
        _db.CustomerCartItems.Add(item);
        await _db.SaveChangesAsync();

        var savedCart = await _db.CustomerCarts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == cart.Id);

        Assert.Single(savedCart.Items);
        Assert.Equal(3, savedCart.Items.First().Quantity);
        Assert.Equal(89.70m, savedCart.TotalValue);
    }

    [Fact]
    public async Task FullOrderWorkflow_CompletesSuccessfully()
    {
        // 1. Criar pedido
        var subtotal = 59.80m; // 2 x 29.90
        var (rate, commission, netAmount) =
            await _commissionService.CalculateCommissionAsync(_establishmentId, subtotal);

        var order = new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"MKT-{DateTime.UtcNow:yyyyMMdd}-TEST01",
            CustomerId = _customerId,
            EstablishmentId = _establishmentId,
            Status = "PENDING",
            Subtotal = subtotal,
            Discount = 0,
            DeliveryFee = 0,
            Total = subtotal,
            PaymentMethod = "CREDIT_CARD",
            PaymentStatus = "PENDING",
            DeliveryType = "DELIVERY",
            PlatformCommissionRate = rate,
            PlatformCommissionAmount = commission,
            NetAmountToPharmacy = netAmount,
            CreatedAt = DateTime.UtcNow
        };

        order.Items.Add(new OnlineOrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = _productId,
            ProductName = "Vitamina C 1000mg",
            Quantity = 2,
            UnitPrice = 29.90m,
            TotalPrice = 59.80m
        });

        _db.OnlineOrders.Add(order);

        // 2. Registrar transação
        await _commissionService.RegisterTransactionAsync(
            order.Id, _establishmentId, _customerId, subtotal, rate);

        // 3. Verificar comissão aplicada
        Assert.Equal(0.07m, rate); // Primeira venda → 7%
        Assert.Equal(4.19m, commission); // 59.80 * 0.07 = 4.186 → 4.19
        Assert.Equal(55.61m, netAmount);

        // 4. Simular fluxo de status
        order.Status = "CONFIRMED";
        order.EstimatedReadyAt = DateTime.UtcNow.AddMinutes(30);
        order.UpdatedAt = DateTime.UtcNow;

        order.Status = "PREPARING";
        order.UpdatedAt = DateTime.UtcNow;

        order.Status = "READY";
        order.ReadyAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        order.Status = "DELIVERED";
        order.DeliveredAt = DateTime.UtcNow;
        order.PaymentStatus = "PAID";
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // 5. Verificar estado final
        var savedOrder = await _db.OnlineOrders.FindAsync(order.Id);
        Assert.NotNull(savedOrder);
        Assert.Equal("DELIVERED", savedOrder.Status);
        Assert.Equal("PAID", savedOrder.PaymentStatus);
        Assert.NotNull(savedOrder.DeliveredAt);

        // 6. Verificar transação registrada
        var transaction = await _db.PlatformTransactions
            .FirstOrDefaultAsync(t => t.OrderId == order.Id);
        Assert.NotNull(transaction);
        Assert.Equal(4.19m, transaction.CommissionAmount);
    }

    [Fact]
    public async Task CancelledOrder_RestoresStock()
    {
        // Criar pedido
        var product = await _db.CatalogProducts.FindAsync(_productId);
        Assert.NotNull(product);
        var originalStock = product.StockQuantity;

        // Simular redução de estoque
        product.StockQuantity -= 5;
        product.TotalSold += 5;
        await _db.SaveChangesAsync();

        Assert.Equal(originalStock - 5, product.StockQuantity);

        // Simular cancelamento (devolve estoque)
        product.StockQuantity += 5;
        product.TotalSold = Math.Max(0, product.TotalSold - 5);
        await _db.SaveChangesAsync();

        Assert.Equal(originalStock, product.StockQuantity);
        Assert.Equal(0, product.TotalSold);
    }

    [Fact]
    public async Task CartSwitchPharmacy_ClearsItems()
    {
        var otherEstablishmentId = Guid.NewGuid();
        _db.Establishments.Add(new Establishment
        {
            Id = otherEstablishmentId,
            NomeFantasia = "Outra Farmácia",
            RazaoSocial = "Outra LTDA",
            Cnpj = "98765432000190",
            PasswordHash = "test-hash",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        // Criar carrinho na farmácia 1
        var cart = new CustomerCart
        {
            Id = Guid.NewGuid(),
            CustomerId = _customerId,
            EstablishmentId = _establishmentId,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.CustomerCarts.Add(cart);

        _db.CustomerCartItems.Add(new CustomerCartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = _productId,
            UnitPrice = 29.90m,
            Quantity = 1,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Simular troca de farmácia — limpa itens
        var items = await _db.CustomerCartItems.Where(i => i.CartId == cart.Id).ToListAsync();
        _db.CustomerCartItems.RemoveRange(items);
        cart.EstablishmentId = otherEstablishmentId;
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var updatedCart = await _db.CustomerCarts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == cart.Id);

        Assert.Empty(updatedCart.Items);
        Assert.Equal(otherEstablishmentId, updatedCart.EstablishmentId);
    }

    [Fact]
    public async Task Rating_UpdatesPharmacyAverage()
    {
        // Adicionar avaliações
        _db.PharmacyRatings.AddRange(
            new PharmacyRating { EstablishmentId = _establishmentId, CustomerId = _customerId, Rating = 5 },
            new PharmacyRating { EstablishmentId = _establishmentId, CustomerId = Guid.NewGuid(), Rating = 4 },
            new PharmacyRating { EstablishmentId = _establishmentId, CustomerId = Guid.NewGuid(), Rating = 3 }
        );
        await _db.SaveChangesAsync();

        // Calcular média
        var avg = await _db.PharmacyRatings
            .Where(r => r.EstablishmentId == _establishmentId)
            .AverageAsync(r => (decimal)r.Rating);

        Assert.Equal(4m, avg);

        // Atualizar no establishment
        var pharmacy = await _db.Establishments.FindAsync(_establishmentId);
        pharmacy!.AverageRating = Math.Round(avg, 2);
        pharmacy.TotalRatings = 3;
        await _db.SaveChangesAsync();

        Assert.Equal(4m, pharmacy.AverageRating);
        Assert.Equal(3, pharmacy.TotalRatings);
    }
}
