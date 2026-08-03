using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models;
using Models.Marketplace;
using Models.Pharmacy;
using Controllers.Mobile;
using Service.Marketplace;
using salvelle.Tests.Helpers;
using Xunit;

namespace salvelle.Tests;

public class MobileOrdersControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CommissionService _commission;
    private readonly MobileOrdersController _controller;
    private readonly MarketplaceTestData _data;

    public MobileOrdersControllerTests()
    {
        _db = TestHelpers.CreateInMemoryDb();
        _data = TestHelpers.SeedMarketplaceData(_db);
        _commission = new CommissionService(_db);
        _controller = new MobileOrdersController(
            _db, _commission, TestHelpers.CreateMockLogger<MobileOrdersController>());
        TestHelpers.SetMobileCustomer(_controller, _data.CustomerId);
    }

    public void Dispose() => _db.Dispose();

    // ==================== HELPERS ====================

    private async Task<CustomerCart> CreateActiveCart(int quantity1 = 2, int quantity2 = 1)
    {
        var cart = new CustomerCart
        {
            Id = Guid.NewGuid(),
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        cart.Items.Add(new CustomerCartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = _data.ProductId1,
            UnitPrice = 25.90m,
            Quantity = quantity1,
            CreatedAt = DateTime.UtcNow
        });
        if (quantity2 > 0)
        {
            cart.Items.Add(new CustomerCartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = _data.ProductId2,
                UnitPrice = 45.50m,
                Quantity = quantity2,
                CreatedAt = DateTime.UtcNow
            });
        }
        _db.CustomerCarts.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    // ==================== CREATE ORDER ====================

    [Fact]
    public async Task CreateOrder_ValidCart_CreatesOrderSuccessfully()
    {
        await CreateActiveCart();

        var request = new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        };

        var result = await _controller.CreateOrder(request);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OrderDetailDto>>(ok.Value);

        Assert.True(response.Success);
        Assert.StartsWith("MKT-", response.Data!.OrderNumber);
        Assert.Equal("PENDING", response.Data.Status);
        Assert.Equal(97.30m, response.Data.Total); // 25.90*2 + 45.50*1
        Assert.Equal(2, response.Data.Items.Count);
    }

    [Fact]
    public async Task CreateOrder_EmptyCart_ReturnsBadRequest()
    {
        var request = new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        };

        var result = await _controller.CreateOrder(request);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_Unauthenticated_ReturnsUnauthorized()
    {
        TestHelpers.SetUnauthenticated(_controller);

        var result = await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_StockInsufficient_ReturnsBadRequest()
    {
        await CreateActiveCart(quantity1: 200, quantity2: 0); // Mais que o estoque (100)

        var request = new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        };

        var result = await _controller.CreateOrder(request);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_BelowMinOrderAmount_ReturnsBadRequest()
    {
        // MinOrderAmount é 10. Criar carrinho com valor abaixo
        var cart = new CustomerCart
        {
            Id = Guid.NewGuid(),
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        // Criar produto barato
        var cheapProduct = new CatalogProduct
        {
            Id = Guid.NewGuid(),
            EstablishmentId = _data.EstablishmentId,
            Name = "Produto Barato",
            Code = "CHEAP-01",
            Price = 1m,
            StockQuantity = 100,
            IsActive = true,
            IsMarketplaceVisible = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.CatalogProducts.Add(cheapProduct);

        cart.Items.Add(new CustomerCartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = cheapProduct.Id,
            UnitPrice = 1m,
            Quantity = 1,
            CreatedAt = DateTime.UtcNow
        });
        _db.CustomerCarts.Add(cart);
        await _db.SaveChangesAsync();

        var result = await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_PharmacyNotAccepting_ReturnsBadRequest()
    {
        await CreateActiveCart();

        var pharmacy = await _db.Establishments.FindAsync(_data.EstablishmentId);
        pharmacy!.AcceptingOrders = false;
        await _db.SaveChangesAsync();

        var result = await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_DeductsStockAndUpdatesTotalSold()
    {
        await CreateActiveCart(quantity1: 3, quantity2: 0);

        await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        var product = await _db.CatalogProducts.FindAsync(_data.ProductId1);
        Assert.Equal(97, product!.StockQuantity); // 100 - 3
        Assert.Equal(3, product.TotalSold);
    }

    [Fact]
    public async Task CreateOrder_ConvertsCartStatus()
    {
        await CreateActiveCart();

        await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        var cart = await _db.CustomerCarts.FirstAsync(c => c.CustomerId == _data.CustomerId);
        Assert.Equal("CONVERTED", cart.Status);
    }

    [Fact]
    public async Task CreateOrder_RegistersCommissionTransaction()
    {
        await CreateActiveCart();

        await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        var transaction = await _db.PlatformTransactions.FirstOrDefaultAsync();
        Assert.NotNull(transaction);
        Assert.Equal(0.07m, transaction.CommissionRate); // Tier 1 (0-20 vendas)
        Assert.Equal("PENDENTE", transaction.Status);
    }

    [Fact]
    public async Task CreateOrder_CreatesDeliveryEstimate()
    {
        await CreateActiveCart();

        await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX"
        });

        var estimate = await _db.DeliveryEstimates.FirstOrDefaultAsync();
        Assert.NotNull(estimate);
        Assert.Equal(45, estimate.EstimatedMinutes); // AverageDeliveryMinutes do seed
        Assert.Equal("ESTIMADO", estimate.Status);
    }

    [Fact]
    public async Task CreateOrder_WithDeliveryAddress_SetsAddressFields()
    {
        await CreateActiveCart();

        var result = await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "DELIVERY",
            DeliveryAddressId = _data.AddressId,
            PaymentMethod = "PIX"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OrderDetailDto>>(ok.Value);

        Assert.Contains("Rua Teste", response.Data!.DeliveryAddress);
        Assert.Equal("DELIVERY", response.Data.DeliveryType);
    }

    // ==================== CUPOM (regressão race de resgate) ====================

    private async Task<Coupon> SeedCoupon(int? maxUses, int usedCount = 0, decimal percentage = 10m)
    {
        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            EstablishmentId = _data.EstablishmentId,
            Code = "PROMO10",
            DiscountType = "PERCENTAGE",
            DiscountPercentage = percentage,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidUntil = DateTime.UtcNow.AddDays(1),
            MaxUses = maxUses,
            UsedCount = usedCount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return coupon;
    }

    // NOTA: o caminho feliz (cupom válido → incremento) usa ExecuteUpdateAsync para um
    // UPDATE condicional atômico (evita over-redemption sob concorrência no Npgsql). O
    // provedor InMemory não traduz ExecuteUpdate, então esse caminho é validado apenas em
    // produção/Npgsql — mesma limitação já aceita pelo ExecuteDeleteAsync em MobileCartController.
    // Aqui cobrimos deterministicamente a REJEIÇÃO, que retorna antes do UPDATE atômico.

    [Fact]
    public async Task CreateOrder_CupomEsgotado_RejeitaSemDebitar()
    {
        await CreateActiveCart();
        // Já no limite: IsValid=false → rejeitado antes de qualquer débito.
        var coupon = await SeedCoupon(maxUses: 1, usedCount: 1);

        var result = await _controller.CreateOrder(new CreateOrderRequest
        {
            EstablishmentId = _data.EstablishmentId,
            DeliveryType = "PICKUP",
            PaymentMethod = "PIX",
            CouponCode = "PROMO10"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        // Contador NÃO foi além do máximo e nenhum uso foi registrado.
        var refreshed = await _db.Coupons.AsNoTracking().FirstAsync(c => c.Id == coupon.Id);
        Assert.Equal(1, refreshed.UsedCount);
        Assert.Equal(0, await _db.CouponUsages.CountAsync(u => u.CouponId == coupon.Id));
    }

    // ==================== GET ORDERS ====================

    [Fact]
    public async Task GetOrders_ReturnsCustomerOrders()
    {
        // Criar 2 pedidos
        for (int i = 0; i < 2; i++)
        {
            _db.OnlineOrders.Add(new OnlineOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"MKT-TEST-{i:D4}",
                CustomerId = _data.CustomerId,
                EstablishmentId = _data.EstablishmentId,
                Status = "PENDING",
                Subtotal = 50m,
                Total = 50m,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        var result = await _controller.GetOrders();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PaginatedResponse<OrderListItemDto>>>(ok.Value);

        Assert.Equal(2, response.Data!.TotalCount);
        Assert.Equal(2, response.Data.Items.Count);
    }

    [Fact]
    public async Task GetOrders_DoesNotReturnOtherCustomerOrders()
    {
        var otherCustomerId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "MKT-OTHER-0001",
            CustomerId = otherCustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "PENDING",
            Subtotal = 50m,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetOrders();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PaginatedResponse<OrderListItemDto>>>(ok.Value);

        Assert.Equal(0, response.Data!.TotalCount);
    }

    // ==================== GET ORDER DETAIL ====================

    [Fact]
    public async Task GetOrder_ValidId_ReturnsDetail()
    {
        var orderId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = orderId,
            OrderNumber = "MKT-DET-0001",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "CONFIRMED",
            Subtotal = 100m,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetOrder(orderId);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OrderDetailDto>>(ok.Value);

        Assert.Equal(orderId, response.Data!.Id);
        Assert.Equal("CONFIRMED", response.Data.Status);
    }

    [Fact]
    public async Task GetOrder_NotFound_ReturnsNotFound()
    {
        var result = await _controller.GetOrder(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ==================== TRACK ORDER ====================

    [Fact]
    public async Task TrackOrder_ReturnsTrackingEvents()
    {
        var orderId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = orderId,
            OrderNumber = "MKT-TRK-0001",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "PREPARING",
            Subtotal = 50m,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });
        _db.DeliveryEstimates.Add(new DeliveryEstimate
        {
            OrderId = orderId,
            EstimatedMinutes = 30,
            EstimatedDeliveryAt = DateTime.UtcNow.AddMinutes(30),
            Status = "PREPARANDO",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller.TrackOrder(orderId);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<DeliveryTrackingDto>>(ok.Value);

        Assert.Equal("PREPARANDO", response.Data!.Status);
        Assert.True(response.Data.Events.Count >= 2); // PENDING + CONFIRMED + PREPARING
    }
}
