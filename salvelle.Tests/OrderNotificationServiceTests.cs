using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Data;
using Models;
using Models.Marketplace;
using Service.Marketplace;
using salvelle.Tests.Helpers;
using Xunit;

namespace salvelle.Tests;

public class OrderNotificationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ILogger<OrderNotificationService>> _loggerMock;
    private readonly OrderNotificationService _service;
    private readonly MarketplaceTestData _data;

    public OrderNotificationServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDb();
        _data = TestHelpers.SeedMarketplaceData(_db);
        _loggerMock = new Mock<ILogger<OrderNotificationService>>();
        _service = new OrderNotificationService(_db, _loggerMock.Object);
    }

    public void Dispose() => _db.Dispose();

    // ==================== NOTIFY PHARMACY ====================

    [Fact]
    public async Task NotifyPharmacyNewOrder_ValidOrder_LogsNotification()
    {
        var order = new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "MKT-NOTIFY-001",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "PENDING",
            Total = 150m,
            CreatedAt = DateTime.UtcNow
        };

        await _service.NotifyPharmacyNewOrder(order);

        // Verifica que logou a notificação
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MKT-NOTIFY-001")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPharmacyNewOrder_PharmacyNotFound_DoesNotThrow()
    {
        var order = new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "MKT-NOTIFY-002",
            CustomerId = _data.CustomerId,
            EstablishmentId = Guid.NewGuid(), // farmácia inexistente
            Status = "PENDING",
            Total = 50m,
            CreatedAt = DateTime.UtcNow
        };

        // Não deve lançar exceção
        await _service.NotifyPharmacyNewOrder(order);
    }

    // ==================== NOTIFY CUSTOMER ====================

    [Fact]
    public async Task NotifyCustomerOrderUpdate_CONFIRMED_LogsCorrectTitle()
    {
        var orderId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = orderId,
            OrderNumber = "MKT-CUST-001",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "CONFIRMED",
            Subtotal = 100m,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _service.NotifyCustomerOrderUpdate(orderId, "CONFIRMED");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Notificação para cliente")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyCustomerOrderUpdate_WithDevice_SendsFcmNotification()
    {
        var orderId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = orderId,
            OrderNumber = "MKT-FCM-001",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = "READY",
            Subtotal = 100m,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });

        _db.CustomerDevices.Add(new CustomerDevice
        {
            Id = Guid.NewGuid(),
            CustomerId = _data.CustomerId,
            DeviceToken = "fcm-token-123456789012345678901234567890",
            Platform = "ANDROID",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _service.NotifyCustomerOrderUpdate(orderId, "READY");

        // Verifica que logou tentativa de FCM
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("FCM Push")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyCustomerOrderUpdate_OrderNotFound_DoesNotThrow()
    {
        await _service.NotifyCustomerOrderUpdate(Guid.NewGuid(), "CONFIRMED");
        // Não deve lançar exceção
    }

    [Theory]
    [InlineData("CONFIRMED")]
    [InlineData("PREPARING")]
    [InlineData("READY")]
    [InlineData("DELIVERED")]
    [InlineData("CANCELLED")]
    public async Task NotifyCustomerOrderUpdate_AllStatuses_DoNotThrow(string status)
    {
        var orderId = Guid.NewGuid();
        _db.OnlineOrders.Add(new OnlineOrder
        {
            Id = orderId,
            OrderNumber = $"MKT-STATUS-{status}",
            CustomerId = _data.CustomerId,
            EstablishmentId = _data.EstablishmentId,
            Status = status,
            Subtotal = 100m,
            Total = 100m,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _service.NotifyCustomerOrderUpdate(orderId, status);
        // Nenhum status deve causar exceção
    }
}
