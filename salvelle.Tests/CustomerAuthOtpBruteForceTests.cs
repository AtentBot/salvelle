using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Cliente;
using Models;
using Service;
using salvelle.Tests.Helpers;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Regressão do achado §2.2: o reset de senha do cliente por OTP não limitava
/// tentativas — o código de 6 dígitos podia ser forçado por bruta sem bloqueio,
/// permitindo takeover de conta. Estes testes travam o contador/lockout.
/// </summary>
public class CustomerAuthOtpBruteForceTests
{
    private const string Phone = "11999998888";

    private static CustomerAuthService CreateService(AppDbContext db) =>
        // ResetPasswordAsync não usa WhatsAppService, logo null! é seguro aqui.
        new CustomerAuthService(db, null!, TestHelpers.CreateMockLogger<CustomerAuthService>());

    private static async Task<AppDbContext> SeedAuthAsync(int attempts = 0)
    {
        var db = TestHelpers.CreateInMemoryDb();
        db.CustomerAuths.Add(new CustomerAuth
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Phone = Phone,
            PasswordHash = "hash-antigo",
            VerificationCode = "hash-de-um-codigo-valido", // não coincide com o palpite
            VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(10),
            VerificationAttempts = attempts,
        });
        await db.SaveChangesAsync();
        return db;
    }

    private static CustomerResetPasswordConfirmDto WrongCodeDto() => new()
    {
        Phone = Phone,
        Code = "000000",
        NewPassword = "novaSenha1",
        ConfirmPassword = "novaSenha1",
    };

    [Fact]
    public async Task ResetPassword_ComCodigoErrado_IncrementaTentativas()
    {
        await using var db = await SeedAuthAsync();
        var service = CreateService(db);

        var (success, _) = await service.ResetPasswordAsync(WrongCodeDto());

        Assert.False(success);
        var auth = await db.CustomerAuths.FirstAsync(a => a.Phone == Phone);
        Assert.Equal(1, auth.VerificationAttempts);
    }

    [Fact]
    public async Task ResetPassword_AposCincoTentativas_BloqueiaComMuitasTentativas()
    {
        // Já no limite: qualquer nova tentativa deve ser barrada antes de checar o código.
        await using var db = await SeedAuthAsync(attempts: 5);
        var service = CreateService(db);

        var (success, message) = await service.ResetPasswordAsync(WrongCodeDto());

        Assert.False(success);
        Assert.Contains("Muitas tentativas", message);
        // Não incrementa além do teto (short-circuit).
        var auth = await db.CustomerAuths.FirstAsync(a => a.Phone == Phone);
        Assert.Equal(5, auth.VerificationAttempts);
    }

    [Fact]
    public async Task ResetPassword_CincoErrosSeguidos_EsgotamAsTentativas()
    {
        await using var db = await SeedAuthAsync();
        var service = CreateService(db);

        for (var i = 0; i < 5; i++)
        {
            var (ok, _) = await service.ResetPasswordAsync(WrongCodeDto());
            Assert.False(ok);
        }

        // A 6ª tentativa cai no lockout, não em "código inválido".
        var (success, message) = await service.ResetPasswordAsync(WrongCodeDto());
        Assert.False(success);
        Assert.Contains("Muitas tentativas", message);
    }
}
