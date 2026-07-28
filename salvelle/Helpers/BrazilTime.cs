namespace Helpers;

/// <summary>
/// Relógio central do horário oficial do Brasil (America/Sao_Paulo, BRT).
///
/// Regra de uso:
///  - Para EXIBIR/GERAR a hora de parede para o usuário brasileiro (rótulos Anvisa,
///    documentos, número de lote, cabeçalhos de relatório, defaults de formulário),
///    use <see cref="Now"/> / <see cref="Today"/>.
///  - Para COMPARAR com valores já armazenados em UTC (ex.: ValidUntil, ExpectedDate),
///    NÃO use este helper — use <c>DateTime.UtcNow</c> diretamente.
///
/// Nunca dependa de <c>DateTime.Now</c> (hora local do servidor): em produção o
/// servidor normalmente roda em UTC, o que desloca datas ~3h para o Brasil.
/// </summary>
public static class BrazilTime
{
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc; // fallback seguro
    }

    /// <summary>Agora, no horário de Brasília.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    /// <summary>Data de hoje, no horário de Brasília.</summary>
    public static DateTime Today => Now.Date;

    /// <summary>Converte um instante UTC para o horário de Brasília.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);
}
