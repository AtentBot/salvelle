using Helpers;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Testes do gating de features por nível de assinatura (<see cref="PlanFeatures"/>).
/// Cobre o parse dos 3 formatos de JSON históricos e a decisão de habilitação
/// (incluindo o comportamento fail-open para planos sem features gravadas).
/// </summary>
public class PlanFeaturesTests
{
    // ---------- Parse ----------

    [Fact]
    public void Parse_ObjetoBool_LeCorreto()
    {
        var f = PlanFeatures.Parse("{\"api_access\": true, \"custom_reports\": false}");
        Assert.True(f["api_access"]);
        Assert.False(f["custom_reports"]);
    }

    [Fact]
    public void Parse_ArrayDeStrings_TodasHabilitadas()
    {
        var f = PlanFeatures.Parse("[\"api_access\", \"ocr_processing\"]");
        Assert.Equal(2, f.Count);
        Assert.True(f["api_access"]);
        Assert.True(f["ocr_processing"]);
    }

    [Fact]
    public void Parse_ListaNameEnabled_LeCorreto()
    {
        var f = PlanFeatures.Parse("[{\"name\":\"white_label\",\"enabled\":true},{\"name\":\"api_access\",\"enabled\":false}]");
        Assert.True(f["white_label"]);
        Assert.False(f["api_access"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("isto nao e json")]
    public void Parse_NuloOuInvalido_RetornaVazio(string? json)
    {
        Assert.Empty(PlanFeatures.Parse(json));
    }

    // ---------- IsEnabled ----------

    [Fact]
    public void IsEnabled_DicionarioVazioOuNulo_FailOpen()
    {
        Assert.True(PlanFeatures.IsEnabled(new Dictionary<string, bool>(), PlanFeatureKeys.FinancialReports));
        Assert.True(PlanFeatures.IsEnabled(null, PlanFeatureKeys.FinancialReports));
    }

    [Fact]
    public void IsEnabled_ChaveAusenteComOutrasPresentes_Nega()
    {
        var f = new Dictionary<string, bool> { ["ocr_processing"] = true };
        Assert.False(PlanFeatures.IsEnabled(f, PlanFeatureKeys.FinancialReports));
    }

    [Fact]
    public void IsEnabled_ChaveExplicitamenteFalse_Nega()
    {
        var f = new Dictionary<string, bool> { [PlanFeatureKeys.FinancialReports] = false };
        Assert.False(PlanFeatures.IsEnabled(f, PlanFeatureKeys.FinancialReports));
    }

    [Fact]
    public void IsEnabled_ChaveTrue_Permite()
    {
        var f = new Dictionary<string, bool> { [PlanFeatureKeys.FinancialReports] = true };
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.FinancialReports));
    }

    // ---------- Cenários reais por plano (dados de subscription_plans) ----------

    private static Dictionary<string, bool> Basico() => PlanFeatures.Parse(
        "{\"api_access\": false, \"custom_reports\": false, \"ocr_processing\": true, \"customer_portal\": true, " +
        "\"inventory_alerts\": true, \"priority_support\": false, \"sngpc_integration\": true, " +
        "\"advanced_analytics\": false, \"whatsapp_notifications\": true}");

    private static Dictionary<string, bool> Profissional() => PlanFeatures.Parse(
        "{\"api_access\": false, \"custom_reports\": true, \"financial_reports\": true, " +
        "\"advanced_analytics\": false, \"priority_support\": true, \"sngpc_integration\": true}");

    private static Dictionary<string, bool> Enterprise() => PlanFeatures.Parse(
        "{\"api_access\": true, \"white_label\": true, \"financial_reports\": true, \"advanced_analytics\": true, " +
        "\"custom_integrations\": true, \"multi_establishment\": true, \"sngpc_integration\": true}");

    [Fact]
    public void Cenario_Basico_BloqueiaFinanceiroAnalyticsEApi()
    {
        var f = Basico();
        Assert.False(PlanFeatures.IsEnabled(f, PlanFeatureKeys.FinancialReports));
        Assert.False(PlanFeatures.IsEnabled(f, PlanFeatureKeys.AdvancedAnalytics));
        Assert.False(PlanFeatures.IsEnabled(f, PlanFeatureKeys.ApiAccess));
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.SngpcIntegration)); // Básico ainda tem SNGPC
    }

    [Fact]
    public void Cenario_Profissional_FinanceiroSim_AnalyticsNao()
    {
        var f = Profissional();
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.FinancialReports));
        Assert.False(PlanFeatures.IsEnabled(f, PlanFeatureKeys.AdvancedAnalytics));
    }

    [Fact]
    public void Cenario_Enterprise_TudoLiberado()
    {
        var f = Enterprise();
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.FinancialReports));
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.AdvancedAnalytics));
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.ApiAccess));
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.WhiteLabel));
        Assert.True(PlanFeatures.IsEnabled(f, PlanFeatureKeys.MultiEstablishment));
    }
}
