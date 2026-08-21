using Microsoft.AspNetCore.Http;

namespace Helpers;

/// <summary>
/// Extensões para checar features de plano nas Views/controllers a partir do HttpContext.
/// As features são carregadas na sessão pelo EmployeeAuthMiddleware.
///
/// Exemplo em Razor:
/// <code>@if (Context.HasPlanFeature(PlanFeatureKeys.FinancialReports)) { ... }</code>
/// </summary>
public static class PlanFeatureViewExtensions
{
    public static bool HasPlanFeature(this HttpContext context, string featureKey)
    {
        var features = context.Items[PlanFeatures.ContextItemsKey] as IReadOnlyDictionary<string, bool>;
        return PlanFeatures.IsEnabled(features, featureKey);
    }
}
