using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Filters;

/// <summary>
/// Bloqueia a ação/controller quando o plano do estabelecimento atual não tem a feature.
/// Lê as features já carregadas na sessão (HttpContext.Items["PlanFeatures"]) — sem consulta ao banco.
///
/// - Controller MVC (retorna View): redireciona para `/minha-assinatura?upgrade=&lt;feature&gt;`.
/// - API (ControllerBase): retorna 403 com JSON `{ message, feature, upgradeRequired }`.
///
/// Uso: <c>[RequireFeature(PlanFeatureKeys.FinancialReports)]</c> na ação ou no controller.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireFeatureAttribute : Attribute, IActionFilter
{
    private readonly string _feature;

    public RequireFeatureAttribute(string feature) => _feature = feature;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var features = context.HttpContext.Items[Helpers.PlanFeatures.ContextItemsKey]
            as IReadOnlyDictionary<string, bool>;

        if (Helpers.PlanFeatures.IsEnabled(features, _feature))
            return;

        if (context.Controller is Controller mvc)
        {
            mvc.TempData["FeatureBlocked"] = _feature;
            context.Result = new RedirectResult($"/minha-assinatura?upgrade={_feature}");
        }
        else
        {
            context.Result = new ObjectResult(new
            {
                message = "Recurso não disponível no seu plano. Faça upgrade para acessar.",
                feature = _feature,
                upgradeRequired = true
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
