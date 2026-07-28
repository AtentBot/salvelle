using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Data;
using Models;
using Models.Marketplace;

namespace salvelle.Tests.Helpers;

/// <summary>
/// Helper para criar contexto de testes de controllers com HttpContext mockado.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Cria AppDbContext com InMemoryDatabase
    /// </summary>
    public static AppDbContext CreateInMemoryDb(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Configura HttpContext com MobileCustomerId (simula JWT auth)
    /// </summary>
    public static void SetMobileCustomer(ControllerBase controller, Guid customerId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["MobileCustomerId"] = customerId;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>
    /// Configura HttpContext com EstablishmentId (simula EmployeeAuth)
    /// </summary>
    public static void SetPharmacyEmployee(ControllerBase controller, Guid establishmentId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["EstablishmentId"] = establishmentId;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>
    /// Configura HttpContext sem autenticação
    /// </summary>
    public static void SetUnauthenticated(ControllerBase controller)
    {
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    /// <summary>
    /// Cria ILogger mock
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    /// <summary>
    /// Seed dados base para testes de marketplace
    /// </summary>
    public static MarketplaceTestData SeedMarketplaceData(AppDbContext db)
    {
        var data = new MarketplaceTestData();

        // Establishment
        db.Establishments.Add(new Establishment
        {
            Id = data.EstablishmentId,
            NomeFantasia = "Farmácia Teste Marketplace",
            RazaoSocial = "Farmácia Teste LTDA",
            Cnpj = "12345678901234",
            PasswordHash = "test-hash-not-real",
            IsActive = true,
            IsMarketplaceActive = true,
            AcceptingOrders = true,
            MinOrderAmount = 10m,
            AverageDeliveryMinutes = 45,
            DeliveryRadiusKm = 15m
        });

        // Customer
        db.Customers.Add(new Customer
        {
            Id = data.CustomerId,
            FullName = "Cliente Teste",
            Email = "cliente@teste.com",
            Phone = "11999990000",
            Cpf = "12345678901",
            Status = "ATIVO",
            ConsentDataProcessing = true,
            ConsentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        // Products
        db.CatalogProducts.Add(new CatalogProduct
        {
            Id = data.ProductId1,
            EstablishmentId = data.EstablishmentId,
            Name = "Vitamina C 500mg",
            Code = "VIT-C-500",
            Price = 25.90m,
            StockQuantity = 100,
            IsActive = true,
            IsMarketplaceVisible = true,
            CreatedAt = DateTime.UtcNow
        });

        db.CatalogProducts.Add(new CatalogProduct
        {
            Id = data.ProductId2,
            EstablishmentId = data.EstablishmentId,
            Name = "Omega 3 1000mg",
            Code = "OMG-3-1000",
            Price = 45.50m,
            StockQuantity = 50,
            IsActive = true,
            IsMarketplaceVisible = true,
            CreatedAt = DateTime.UtcNow
        });

        // Customer Address
        db.CustomerAddresses.Add(new CustomerAddress
        {
            Id = data.AddressId,
            CustomerId = data.CustomerId,
            Label = "Casa",
            Street = "Rua Teste",
            Number = "123",
            Neighborhood = "Centro",
            City = "São Paulo",
            State = "SP",
            ZipCode = "01000000",
            Latitude = -23.55,
            Longitude = -46.63,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
        return data;
    }
}

/// <summary>
/// IDs padrão para testes
/// </summary>
public class MarketplaceTestData
{
    public Guid EstablishmentId { get; } = Guid.NewGuid();
    public Guid CustomerId { get; } = Guid.NewGuid();
    public Guid ProductId1 { get; } = Guid.NewGuid();
    public Guid ProductId2 { get; } = Guid.NewGuid();
    public Guid AddressId { get; } = Guid.NewGuid();
}
