using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Data;
using Models;
using Controllers;
using salvelle.Tests.Helpers;
using Moq;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Regressão do achado §3/§4 (mass assignment em Establishment): o cadastro
/// público `POST /api/establishments` fazia bind da entidade inteira, permitindo
/// ao cliente injetar AccessLevelId (escalada de privilégio via FK de papel),
/// CategoryId e flags de marketplace. Agora só uma allow-list é aceita e os
/// campos controlados pela plataforma são definidos no servidor.
/// </summary>
public class EstablishmentCreateMassAssignmentTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly EstablishmentsController _controller;
    private readonly Guid _ownerAccessLevelId = Guid.NewGuid();

    public EstablishmentCreateMassAssignmentTests()
    {
        _db = TestHelpers.CreateInMemoryDb();
        _db.Set<AccessLevel>().Add(new AccessLevel
        {
            Id = _ownerAccessLevelId,
            Code = "owner",
            Name = "Proprietário",
            Description = "Acesso total",
            IsActive = true,
        });
        _db.SaveChanges();

        var config = new ConfigurationBuilder().Build(); // sem AtentBot:ApiKey → não dispara WhatsApp
        _controller = new EstablishmentsController(_db, Mock.Of<IHttpClientFactory>(), config);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_DefineAccessLevelECategoriaNoServidor_IgnorandoCliente()
    {
        var request = new EstablishmentsController.CreateEstablishmentRequest
        {
            NomeFantasia = "Farmácia Nova",
            RazaoSocial = "Farmácia Nova LTDA",
            Cnpj = "99887766554433",
            WhatsApp = "11988887777",
            Email = "nova@farmacia.com",
            Password = "SenhaForte123",
        };

        var result = await _controller.Create(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);

        var est = await _db.Establishments.FirstAsync(e => e.Cnpj == "99887766554433");
        // Papel e categoria vieram da plataforma, não do corpo da requisição.
        Assert.Equal(_ownerAccessLevelId, est.AccessLevelId);
        Assert.NotEqual(Guid.Empty, est.CategoryId);
        // Flags sensíveis forçadas para valores seguros.
        Assert.False(est.IsMarketplaceActive);
        Assert.Null(est.SubscriptionStatus);
        Assert.False(est.OnboardingCompleted);
        // Senha foi hasheada (Argon2), nunca armazenada em texto puro.
        Assert.StartsWith("$argon2", est.PasswordHash);
    }

    [Fact]
    public async Task Create_CnpjDuplicado_RetornaConflict()
    {
        var request = new EstablishmentsController.CreateEstablishmentRequest
        {
            NomeFantasia = "Farmácia A",
            RazaoSocial = "Farmácia A LTDA",
            Cnpj = "11111111111111",
            WhatsApp = "11900000000",
            Password = "SenhaForte123",
        };
        await _controller.Create(request, CancellationToken.None);

        var dup = new EstablishmentsController.CreateEstablishmentRequest
        {
            NomeFantasia = "Farmácia B",
            RazaoSocial = "Farmácia B LTDA",
            Cnpj = "11111111111111",
            WhatsApp = "11900000001",
            Password = "SenhaForte123",
        };
        var result = await _controller.Create(dup, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }
}
