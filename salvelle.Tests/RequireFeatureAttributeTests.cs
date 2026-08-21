using Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Testes do enforcement do gating (<see cref="RequireFeatureAttribute"/>).
/// Valida que, num controller de API (ControllerBase), a ausência da feature no plano
/// resulta em 403, enquanto feature presente (ou plano sem features = fail-open) prossegue.
/// </summary>
public class RequireFeatureAttributeTests
{
    private sealed class FakeApiController : ControllerBase { }

    private static ActionExecutingContext MakeContext(IDictionary<string, bool>? features)
    {
        var http = new DefaultHttpContext();
        if (features != null)
            http.Items[PlanFeatures_ContextKey] = features;

        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new FakeApiController());
    }

    // Chave usada pelo middleware/atributo em HttpContext.Items
    private const string PlanFeatures_ContextKey = "PlanFeatures";

    [Fact]
    public void Api_FeatureAusente_Retorna403()
    {
        var ctx = MakeContext(new Dictionary<string, bool> { ["ocr_processing"] = true });
        new RequireFeatureAttribute("financial_reports").OnActionExecuting(ctx);

        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void Api_FeatureFalse_Retorna403()
    {
        var ctx = MakeContext(new Dictionary<string, bool> { ["financial_reports"] = false });
        new RequireFeatureAttribute("financial_reports").OnActionExecuting(ctx);

        Assert.IsType<ObjectResult>(ctx.Result);
    }

    [Fact]
    public void Api_FeaturePresente_Prossegue()
    {
        var ctx = MakeContext(new Dictionary<string, bool> { ["financial_reports"] = true });
        new RequireFeatureAttribute("financial_reports").OnActionExecuting(ctx);

        Assert.Null(ctx.Result); // não bloqueou
    }

    [Fact]
    public void PlanoSemFeatures_FailOpen_Prossegue()
    {
        var ctx = MakeContext(new Dictionary<string, bool>()); // vazio
        new RequireFeatureAttribute("financial_reports").OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }

    [Fact]
    public void SemContextoDeFeatures_FailOpen_Prossegue()
    {
        var ctx = MakeContext(null); // Items["PlanFeatures"] ausente
        new RequireFeatureAttribute("financial_reports").OnActionExecuting(ctx);

        Assert.Null(ctx.Result);
    }
}
