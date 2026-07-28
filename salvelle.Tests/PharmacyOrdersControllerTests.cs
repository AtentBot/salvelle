using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using DTOs.Pharmacy.Marketplace;
using Models;
using Models.Marketplace;
using Controllers.Pharmacy;
using salvelle.Tests.Helpers;
using Xunit;

namespace salvelle.Tests;

public class PharmacyOrdersControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PharmacyOrdersController _controller;
    private readonly MarketplaceTestData _data;

    public PharmacyOrdersControllerTests()
    {
        _db = TestHelpers.CreateInMemoryDb();
        _data = TestHelpers.SeedMarketplaceData(_db);
        _controller = new PharmacyOrdersController(
            _db, TestHelpers.CreateMockLogger<PharmacyOrdersController>());
        TestHelpers.SetPharmacyEmployee(_controller, _data.EstablishmentId);
    }

    public void Dispose() => _db.Dispose();

    // ==================== HELPERS ====================

    private async Task<OnlineOrder> CreateTestOrder(string status = "PENDING", bool withProduct = true)
    {
        var order = new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"MKT-TST-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = status,
            Subtotal = 100m,
            Total = 100m,
            PlatformCommissionRate = 0.07m,
            PlatformCommissionAmount = 7m,
            NetAmountToPharmacy = 93m,
            CreatedAt = DateTime.UtcNow
        };

        if (withProduct)
        {
            order.Items.Add(new OnlineOrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = _data.ProductId1,
                ProductName = "Vitamina C 500mg",
                Quantity = 2,
                UnitPrice = 50m,
                TotalPrice = 100m
            });
        }

        _db.OnlineOrders.Add(order);

        // Criar DeliveryEstimate
        _db.DeliveryEstimates.Add(new DeliveryEstimate
        {
            OrderId = order.Id,
            EstimatedMinutes = 45,
            EstimatedDeliveryAt = DateTime.UtcNow.AddMinutes(45),
            Status = "ESTIMADO",
            CreatedAt = DateTime.UtcNow
        });

        // Criar PlatformTransaction
        _db.PlatformTransactions.Add(new PlatformTransaction
        {
            OrderId = order.Id,
            EstablishmentId = _data.EstablishmentId,
            CustomerId = _data.CustomerId,
            GrossAmount = 100m,
            CommissionRate = 0.07m,
            CommissionAmount = 7m,
            NetAmountToPharmacy = 93m,
            Status = "PENDENTE",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return order;
    }

    // ==================== GET ORDERS ====================

    [Fact]
    public async Task GetOrders_ReturnsPharmacyOrders()
    {
        await CreateTestOrder();
        await CreateTestOrder("CONFIRMED");

        var result = await _controller.GetOrders(new OrdersFilterRequest { Page = 1, PageSize = 20 });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PaginatedResponse<PharmacyOrderListItemDto>>>(ok.Value);

        Assert.Equal(2, response.Data!.TotalCount);
    }

    [Fact]
    public async Task GetOrders_FilterByStatus_ReturnsFiltered()
    {
        await CreateTestOrder("PENDING");
        await CreateTestOrder("CONFIRMED");
        await CreateTestOrder("CONFIRMED");

        var result = await _controller.GetOrders(new OrdersFilterRequest
        {
            Status = "CONFIRMED",
            Page = 1,
            PageSize = 20
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PaginatedResponse<PharmacyOrderListItemDto>>>(ok.Value);

        Assert.Equal(2, response.Data!.TotalCount);
        Assert.All(response.Data.Items, item => Assert.Equal("CONFIRMED", item.Status));
    }

    [Fact]
    public async Task GetOrders_Unauthenticated_ReturnsUnauthorized()
    {
        TestHelpers.SetUnauthenticated(_controller);
        var result = await _controller.GetOrders(new OrdersFilterRequest { Page = 1, PageSize = 20 });
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetOrders_DoesNotReturnOtherPharmacyOrders()
    {
        // Pedido de outra farmácia
        var otherPharmacyId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "MKT-OTHER-0001",
            CustomerId = _data.CustomerId,
            EstablishmentId = otherPharmacyId,
            Status = "PENDING",
            Subtotal = 50m,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetOrders(new OrdersFilterRequest { Page = 1, PageSize = 20 });
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PaginatedResponse<PharmacyOrderListItemDto>>>(ok.Value);

        Assert.Equal(0, response.Data!.TotalCount);
    }

    // ==================== GET ORDER COUNTS ====================

    [Fact]
    public async Task GetOrderCounts_ReturnsCorrectBreakdown()
    {
        await CreateTestOrder("PENDING");
        await CreateTestOrder("PENDING");
        await CreateTestOrder("CONFIRMED");
        await CreateTestOrder("PREPARING");

        var result = await _controller.GetOrderCounts();
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<OrderStatusBreakdownDto>>(ok.Value);

        Assert.Equal(2, response.Data!.Pending);
        Assert.Equal(1, response.Data.Confirmed);
        Assert.Equal(1, response.Data.Preparing);
        Assert.Equal(0, response.Data.Delivered);
    }

    // ==================== GET ORDER DETAIL ====================

    [Fact]
    public async Task GetOrder_ValidId_ReturnsDetailWithCommission()
    {
        var order = await CreateTestOrder();

        var result = await _controller.GetOrder(order.Id);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PharmacyOrderDetailDto>>(ok.Value);

        Assert.Equal(order.Id, response.Data!.Id);
        Assert.Equal(0.07m, response.Data.CommissionRate);
        Assert.Equal(7m, response.Data.CommissionAmount);
        Assert.Equal(93m, response.Data.NetAmount);
        Assert.Single(response.Data.Items);
    }

    [Fact]
    public async Task GetOrder_OtherPharmacyOrder_ReturnsNotFound()
    {
        var otherPharmacyOrder = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = otherPharmacyOrder,
            OrderNumber = "MKT-OTHER-X001",
            CustomerId = _data.CustomerId,
            EstablishmentId = Guid.NewGuid(), // outra farmácia
            Status = "PENDING",
            Subtotal = 50m,
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller.GetOrder(otherPharmacyOrder);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ==================== UPDATE STATUS ====================

    [Fact]
    public async Task UpdateStatus_PendingToConfirmed_Success()
    {
        var order = await CreateTestOrder("PENDING");

        var result = await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest
        {
            NewStatus = "CONFIRMED",
            EstimatedMinutes = 30
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PharmacyOrderDetailDto>>(ok.Value);

        Assert.Equal("CONFIRMED", response.Data!.Status);
        Assert.NotNull(response.Data.EstimatedReadyAt);
    }

    [Fact]
    public async Task UpdateStatus_FullWorkflow_PENDING_to_DELIVERED()
    {
        var order = await CreateTestOrder("PENDING");

        // PENDING → CONFIRMED
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest
        {
            NewStatus = "CONFIRMED",
            EstimatedMinutes = 30
        });

        // CONFIRMED → PREPARING
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "PREPARING" });

        // PREPARING → READY
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "READY" });

        // READY → DELIVERED
        var result = await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "DELIVERED" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PharmacyOrderDetailDto>>(ok.Value);

        Assert.Equal("DELIVERED", response.Data!.Status);

        // Verificar PaymentStatus
        var savedOrder = await _db.OnlineOrders.FindAsync(order.Id);
        Assert.Equal("PAID", savedOrder!.PaymentStatus);
        Assert.NotNull(savedOrder.DeliveredAt);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsBadRequest()
    {
        var order = await CreateTestOrder("PENDING");

        var result = await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest
        {
            NewStatus = "DELIVERED" // Não pode pular de PENDING para DELIVERED
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_Cancel_RestoresStockAndEstornaTransaction()
    {
        var order = await CreateTestOrder("PENDING");

        // Pegar estoque antes
        var productBefore = await _db.CatalogProducts.FindAsync(_data.ProductId1);
        var stockBefore = productBefore!.StockQuantity;

        var result = await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest
        {
            NewStatus = "CANCELLED"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PharmacyOrderDetailDto>>(ok.Value);
        Assert.Equal("CANCELLED", response.Data!.Status);

        // Verificar estoque restaurado
        await _db.Entry(productBefore).ReloadAsync();
        Assert.Equal(stockBefore + 2, productBefore.StockQuantity); // +2 do item do pedido

        // Verificar transação estornada
        var transaction = await _db.PlatformTransactions.FirstOrDefaultAsync(t => t.OrderId == order.Id);
        Assert.Equal("ESTORNADO", transaction!.Status);

        // Verificar PaymentStatus
        var savedOrder = await _db.OnlineOrders.FindAsync(order.Id);
        Assert.Equal("REFUNDED", savedOrder!.PaymentStatus);
        Assert.NotNull(savedOrder.CancelledAt);
    }

    [Fact]
    public async Task UpdateStatus_CancelAfterReady_ReturnsBadRequest()
    {
        var order = await CreateTestOrder("PENDING");

        // Avançar para READY
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "CONFIRMED" });
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "PREPARING" });
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "READY" });

        // Tentar cancelar depois de READY — deve falhar
        var result = await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest
        {
            NewStatus = "CANCELLED"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_DeliveryEstimate_UpdatesOnEachStep()
    {
        var order = await CreateTestOrder("PENDING");

        // CONFIRMED
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest
        {
            NewStatus = "CONFIRMED",
            EstimatedMinutes = 20
        });
        var estimate = await _db.DeliveryEstimates.FirstAsync(d => d.OrderId == order.Id);
        Assert.Equal("CONFIRMADO", estimate.Status);
        Assert.Equal(20, estimate.EstimatedMinutes);

        // PREPARING
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "PREPARING" });
        await _db.Entry(estimate).ReloadAsync();
        Assert.Equal("PREPARANDO", estimate.Status);

        // READY
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "READY" });
        await _db.Entry(estimate).ReloadAsync();
        Assert.Equal("PRONTO", estimate.Status);

        // DELIVERED
        await _controller.UpdateOrderStatus(order.Id, new UpdateOrderStatusRequest { NewStatus = "DELIVERED" });
        await _db.Entry(estimate).ReloadAsync();
        Assert.Equal("ENTREGUE", estimate.Status);
        Assert.NotNull(estimate.ActualDeliveryAt);
    }
}
