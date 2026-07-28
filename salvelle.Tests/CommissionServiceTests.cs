using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Marketplace;
using Service.Marketplace;
using Xunit;

namespace salvelle.Tests;

public class CommissionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CommissionService _service;
    private readonly Guid _establishmentId = Guid.NewGuid();

    public CommissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CommissionService(_db);

        // Seed establishment
        _db.Establishments.Add(new Establishment
        {
            Id = _establishmentId,
            NomeFantasia = "Farmácia Teste",
            RazaoSocial = "Farmácia Teste LTDA",
            Cnpj = "12345678901234",
            PasswordHash = "test-hash",
            IsActive = true,
            IsMarketplaceActive = true
        });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ==================== COMMISSION RATE TESTS ====================

    [Theory]
    [InlineData(0, 0.07)]   // 0 vendas → 7%
    [InlineData(1, 0.07)]   // 1 venda → 7%
    [InlineData(20, 0.07)]  // 20 vendas → 7%
    [InlineData(21, 0.05)]  // 21 vendas → 5%
    [InlineData(50, 0.05)]  // 50 vendas → 5%
    [InlineData(100, 0.05)] // 100 vendas → 5%
    [InlineData(101, 0.03)] // 101 vendas → 3%
    [InlineData(500, 0.03)] // 500 vendas → 3%
    public void GetCommissionRate_ReturnsCorrectTier(int weeklySales, decimal expectedRate)
    {
        var rate = _service.GetCommissionRate(weeklySales);
        Assert.Equal(expectedRate, rate);
    }

    // ==================== CALCULATE COMMISSION TESTS ====================

    [Fact]
    public async Task CalculateCommissionAsync_NoOrders_Returns7Percent()
    {
        var (rate, commission, netAmount) = await _service.CalculateCommissionAsync(_establishmentId, 100m);

        Assert.Equal(0.07m, rate);
        Assert.Equal(7.00m, commission);
        Assert.Equal(93.00m, netAmount);
    }

    [Fact]
    public async Task CalculateCommissionAsync_CorrectlyCalculatesNetAmount()
    {
        var (rate, commission, netAmount) = await _service.CalculateCommissionAsync(_establishmentId, 250.50m);

        Assert.Equal(0.07m, rate);
        Assert.Equal(17.54m, commission); // 250.50 * 0.07 = 17.535 → 17.54
        Assert.Equal(232.96m, netAmount); // 250.50 - 17.54
    }

    // ==================== REGISTER TRANSACTION TESTS ====================

    [Fact]
    public async Task RegisterTransactionAsync_CreatesTransaction()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var transaction = await _service.RegisterTransactionAsync(
            orderId, _establishmentId, customerId, 200m, 0.07m);

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(orderId, transaction.OrderId);
        Assert.Equal(200m, transaction.GrossAmount);
        Assert.Equal(0.07m, transaction.CommissionRate);
        Assert.Equal(14m, transaction.CommissionAmount);
        Assert.Equal(186m, transaction.NetAmountToPharmacy);
        Assert.Equal("PENDENTE", transaction.Status);
    }

    [Fact]
    public async Task RegisterTransactionAsync_PersistsToDatabase()
    {
        var orderId = Guid.NewGuid();
        await _service.RegisterTransactionAsync(orderId, _establishmentId, null, 100m, 0.05m);

        var saved = await _db.PlatformTransactions.FirstOrDefaultAsync(t => t.OrderId == orderId);
        Assert.NotNull(saved);
        Assert.Equal(5m, saved.CommissionAmount);
    }

    // ==================== WEEKLY COMMISSIONS TESTS ====================

    [Fact]
    public async Task CalculateWeeklyCommissionsAsync_NoTransactions_ReturnsEmpty()
    {
        var result = await _service.CalculateWeeklyCommissionsAsync(DateTime.UtcNow);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CalculateWeeklyCommissionsAsync_GroupsByPharmacy()
    {
        // Seed transactions for this week
        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            _db.PlatformTransactions.Add(new PlatformTransaction
            {
                OrderId = Guid.NewGuid(),
                EstablishmentId = _establishmentId,
                GrossAmount = 100m,
                CommissionRate = 0.07m,
                CommissionAmount = 7m,
                NetAmountToPharmacy = 93m,
                Status = "PENDENTE",
                CreatedAt = now
            });
        }
        await _db.SaveChangesAsync();

        var result = await _service.CalculateWeeklyCommissionsAsync(now);

        Assert.Single(result);
        Assert.Equal(_establishmentId, result[0].EstablishmentId);
        Assert.Equal(5, result[0].TotalSalesCount);
        Assert.Equal(500m, result[0].TotalSalesAmount);
        Assert.Equal(35m, result[0].TotalCommissionAmount);
        Assert.Equal("CALCULADO", result[0].Status);
    }

    [Fact]
    public async Task CalculateWeeklyCommissionsAsync_IdempotentForSameWeek()
    {
        var now = DateTime.UtcNow;
        _db.PlatformTransactions.Add(new PlatformTransaction
        {
            OrderId = Guid.NewGuid(),
            EstablishmentId = _establishmentId,
            GrossAmount = 100m,
            CommissionRate = 0.07m,
            CommissionAmount = 7m,
            NetAmountToPharmacy = 93m,
            Status = "PENDENTE",
            CreatedAt = now
        });
        await _db.SaveChangesAsync();

        var first = await _service.CalculateWeeklyCommissionsAsync(now);
        var second = await _service.CalculateWeeklyCommissionsAsync(now);

        // Should return same results without duplicating
        Assert.Equal(first.Count, second.Count);
    }

    // ==================== FINANCIAL SUMMARY TESTS ====================

    [Fact]
    public async Task GetPharmacyFinancialSummaryAsync_ReturnsCorrectTotals()
    {
        var now = DateTime.UtcNow;
        _db.PlatformTransactions.AddRange(
            new PlatformTransaction
            {
                OrderId = Guid.NewGuid(),
                EstablishmentId = _establishmentId,
                GrossAmount = 200m,
                CommissionRate = 0.07m,
                CommissionAmount = 14m,
                NetAmountToPharmacy = 186m,
                Status = "PENDENTE",
                CreatedAt = now
            },
            new PlatformTransaction
            {
                OrderId = Guid.NewGuid(),
                EstablishmentId = _establishmentId,
                GrossAmount = 300m,
                CommissionRate = 0.07m,
                CommissionAmount = 21m,
                NetAmountToPharmacy = 279m,
                Status = "PENDENTE",
                CreatedAt = now
            },
            new PlatformTransaction
            {
                OrderId = Guid.NewGuid(),
                EstablishmentId = _establishmentId,
                GrossAmount = 100m,
                CommissionRate = 0.07m,
                CommissionAmount = 7m,
                NetAmountToPharmacy = 93m,
                Status = "ESTORNADO", // Should be excluded
                CreatedAt = now
            }
        );
        await _db.SaveChangesAsync();

        var summary = await _service.GetPharmacyFinancialSummaryAsync(
            _establishmentId, now.AddDays(-1), now.AddDays(1));

        Assert.Equal(2, summary.TotalOrders); // Estornado excluded
        Assert.Equal(500m, summary.GrossRevenue);
        Assert.Equal(35m, summary.TotalCommission);
        Assert.Equal(465m, summary.NetRevenue);
    }
}
