namespace Helpers;

/// <summary>
/// Vocabulário canônico de status de <c>ManipulationOrder</c> e <c>PrescriptionQuote</c>.
///
/// A base histórica mistura termos (FINALIZADO/CONCLUIDA, CANCELADO/CANCELADA),
/// então os conjuntos abaixo tratam os sinônimos de forma defensiva. Centralizado
/// aqui para que KPIs, alertas e relatórios usem exatamente a mesma cobertura e
/// não divirjam entre telas.
/// </summary>
public static class ManipulationStatus
{
    // Ordens são finalizadas como "FINALIZADO" e entregues como "ENTREGUE".
    // (CONCLUIDA/CONCLUIDO são status de etapa, mantidos por robustez com dados legados.)
    public static readonly string[] Done = { "FINALIZADO", "ENTREGUE", "CONCLUIDA", "CONCLUIDO" };
    public static readonly string[] Cancelled = { "CANCELADO", "CANCELADA" };

    // "Fechadas" = finalizadas/entregues + canceladas (não estão mais em produção).
    public static readonly string[] Closed =
        { "FINALIZADO", "ENTREGUE", "CONCLUIDA", "CONCLUIDO", "CANCELADO", "CANCELADA" };
}

/// <summary>
/// Vocabulário canônico de status de <c>PrescriptionQuote</c>.
/// Orçamento nasce "PENDENTE"; é recusado como "RECUSADO" e aprovado como "APROVADO".
/// </summary>
public static class QuoteStatus
{
    public const string Pending = "PENDENTE";
    public const string Approved = "APROVADO";
    public const string Rejected = "RECUSADO";
}
