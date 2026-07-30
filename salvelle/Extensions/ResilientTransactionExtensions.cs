using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

// Declarado no namespace do EF Core de propósito: todo arquivo que já usa
// `using Microsoft.EntityFrameworkCore;` enxerga estas extensões sem import extra.
namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Executa uma transação manual DENTRO da execution strategy do EF Core.
///
/// O contexto está configurado com <c>EnableRetryOnFailure</c> (ver Program.cs), o que ativa a
/// <c>NpgsqlRetryingExecutionStrategy</c>. Abrir <c>BeginTransaction</c> fora dessa strategy lança
/// em runtime: "The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not
/// support user-initiated transactions." — quebrando o endpoint com HTTP 500.
///
/// Estes helpers criam a transação sob <c>CreateExecutionStrategy().ExecuteAsync(...)</c>, de modo
/// que todo o bloco vira uma unidade reexecutável. A <paramref name="operation"/> recebe a transação
/// e é responsável por chamar <c>CommitAsync</c>; se ela lançar, o <c>await using</c> descarta a
/// transação (rollback automático) antes de a exceção subir para a strategy.
/// </summary>
public static class ResilientTransactionExtensions
{
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this DatabaseFacade database,
        Func<IDbContextTransaction, Task<T>> operation)
    {
        var strategy = database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await database.BeginTransactionAsync();
            return await operation(transaction);
        });
    }

    public static async Task ExecuteInTransactionAsync(
        this DatabaseFacade database,
        Func<IDbContextTransaction, Task> operation)
    {
        var strategy = database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await database.BeginTransactionAsync();
            await operation(transaction);
        });
    }
}
