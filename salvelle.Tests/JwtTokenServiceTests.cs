using Microsoft.Extensions.Configuration;
using Service.Marketplace;
using Xunit;

namespace salvelle.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly Guid _customerId = Guid.NewGuid();

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretKeyForTestingAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "salvelle-test",
                ["Jwt:Audience"] = "salvelle-mobile-test",
                ["Jwt:AccessTokenExpirationMinutes"] = "15"
            })
            .Build();

        _service = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var token = _service.GenerateAccessToken(_customerId, "test@test.com", "Test User");
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidToken()
    {
        var token = _service.GenerateAccessToken(_customerId, "test@test.com", "Test User");
        var principal = _service.ValidateToken(token);

        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_ExtractsCorrectCustomerId()
    {
        var token = _service.GenerateAccessToken(_customerId, "test@test.com");
        var principal = _service.ValidateToken(token);

        Assert.NotNull(principal);
        var extractedId = _service.GetCustomerIdFromToken(principal);
        Assert.Equal(_customerId, extractedId);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var result = _service.ValidateToken("invalid.token.here");
        Assert.Null(result);
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        var token = _service.GenerateAccessToken(_customerId, "test@test.com");
        var tampered = token + "x"; // tamper with token
        var result = _service.ValidateToken(tampered);
        Assert.Null(result);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
        Assert.True(token1.Length > 20); // Base64 of 64 bytes
    }

    [Fact]
    public void GenerateAccessToken_ContainsEmailClaim()
    {
        var email = "test@example.com";
        var token = _service.GenerateAccessToken(_customerId, email);
        var principal = _service.ValidateToken(token);

        Assert.NotNull(principal);
        var emailClaim = principal.FindFirst("email")?.Value
                         ?? principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value
                         ?? principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        Assert.Equal(email, emailClaim);
    }
}
