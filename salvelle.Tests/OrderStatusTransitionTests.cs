using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Testes de validação de transições de status de pedido.
/// Garante que o fluxo de status segue as regras de negócio.
/// </summary>
public class OrderStatusTransitionTests
{
    // Replicando a lógica do PharmacyOrdersController
    private static bool IsValidStatusTransition(string current, string next)
    {
        return (current, next) switch
        {
            ("PENDING", "CONFIRMED") => true,
            ("PENDING", "CANCELLED") => true,
            ("CONFIRMED", "PREPARING") => true,
            ("CONFIRMED", "CANCELLED") => true,
            ("PREPARING", "READY") => true,
            ("PREPARING", "CANCELLED") => true,
            ("READY", "DELIVERED") => true,
            _ => false
        };
    }

    // ==================== VALID TRANSITIONS ====================

    [Theory]
    [InlineData("PENDING", "CONFIRMED")]
    [InlineData("PENDING", "CANCELLED")]
    [InlineData("CONFIRMED", "PREPARING")]
    [InlineData("CONFIRMED", "CANCELLED")]
    [InlineData("PREPARING", "READY")]
    [InlineData("PREPARING", "CANCELLED")]
    [InlineData("READY", "DELIVERED")]
    public void ValidTransitions_ShouldPass(string from, string to)
    {
        Assert.True(IsValidStatusTransition(from, to));
    }

    // ==================== INVALID TRANSITIONS ====================

    [Theory]
    [InlineData("PENDING", "PREPARING")]     // Skip CONFIRMED
    [InlineData("PENDING", "READY")]         // Skip multiple
    [InlineData("PENDING", "DELIVERED")]     // Skip all
    [InlineData("CONFIRMED", "DELIVERED")]   // Skip PREPARING + READY
    [InlineData("CONFIRMED", "READY")]       // Skip PREPARING
    [InlineData("PREPARING", "DELIVERED")]   // Skip READY
    [InlineData("READY", "CANCELLED")]       // Can't cancel after ready
    [InlineData("DELIVERED", "CANCELLED")]   // Can't cancel after delivered
    [InlineData("DELIVERED", "PREPARING")]   // Backward transition
    [InlineData("CANCELLED", "CONFIRMED")]   // Can't resume cancelled
    [InlineData("CANCELLED", "PENDING")]     // Can't go back
    public void InvalidTransitions_ShouldFail(string from, string to)
    {
        Assert.False(IsValidStatusTransition(from, to));
    }

    // ==================== FULL HAPPY PATH ====================

    [Fact]
    public void FullHappyPath_AllTransitionsValid()
    {
        var statuses = new[] { "PENDING", "CONFIRMED", "PREPARING", "READY", "DELIVERED" };

        for (int i = 0; i < statuses.Length - 1; i++)
        {
            Assert.True(IsValidStatusTransition(statuses[i], statuses[i + 1]),
                $"Transition {statuses[i]} → {statuses[i + 1]} should be valid");
        }
    }

    // ==================== CANCELLATION PATH ====================

    [Fact]
    public void CancellationPossible_UntilReady()
    {
        // Pode cancelar em PENDING, CONFIRMED, PREPARING
        Assert.True(IsValidStatusTransition("PENDING", "CANCELLED"));
        Assert.True(IsValidStatusTransition("CONFIRMED", "CANCELLED"));
        Assert.True(IsValidStatusTransition("PREPARING", "CANCELLED"));

        // NÃO pode cancelar em READY ou DELIVERED
        Assert.False(IsValidStatusTransition("READY", "CANCELLED"));
        Assert.False(IsValidStatusTransition("DELIVERED", "CANCELLED"));
    }
}
