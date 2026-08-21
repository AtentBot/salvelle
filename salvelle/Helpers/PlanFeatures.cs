using System.Text.Json;

namespace Helpers;

/// <summary>
/// Chaves de feature por nível de assinatura. Espelham `subscription_plans.features`
/// (copiado para `Establishment.FeaturesEnabled`). Use estas constantes no gating.
/// </summary>
public static class PlanFeatureKeys
{
    public const string CustomReports = "custom_reports";
    public const string FinancialReports = "financial_reports";
    public const string AdvancedAnalytics = "advanced_analytics";
    public const string ApiAccess = "api_access";
    public const string WhiteLabel = "white_label";
    public const string MultiEstablishment = "multi_establishment";
    public const string CustomIntegrations = "custom_integrations";
    public const string SngpcIntegration = "sngpc_integration";
    public const string OcrProcessing = "ocr_processing";
    public const string CustomerPortal = "customer_portal";
    public const string InventoryAlerts = "inventory_alerts";
    public const string PrioritySupport = "priority_support";
    public const string DedicatedSupport = "dedicated_support";
    public const string WhatsappNotifications = "whatsapp_notifications";
}

/// <summary>
/// Utilitário de features de plano: parse robusto do JSON e checagem de habilitação.
/// As features do estabelecimento já vêm carregadas na sessão pelo EmployeeAuthMiddleware,
/// em `HttpContext.Items[PlanFeatures.ContextItemsKey]` — sem consulta extra ao banco.
/// </summary>
public static class PlanFeatures
{
    /// <summary>Chave usada em HttpContext.Items para o dicionário de features do estabelecimento atual.</summary>
    public const string ContextItemsKey = "PlanFeatures";

    // Case-insensitive para tolerar chaves em minúsculas no formato [{"name":..,"enabled":..}].
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Parseia o JSON de features tratando os 3 formatos históricos:
    /// objeto {"k": true}, array ["k1","k2"], ou lista [{"name":"k","enabled":true}].
    /// </summary>
    public static Dictionary<string, bool> Parse(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
            return new Dictionary<string, bool>();

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(featuresJson);
            if (dict != null) return dict;
        }
        catch { /* tenta próximo formato */ }

        try
        {
            var array = JsonSerializer.Deserialize<string[]>(featuresJson);
            if (array != null) return array.ToDictionary(f => f, _ => true);
        }
        catch { /* tenta próximo formato */ }

        try
        {
            var list = JsonSerializer.Deserialize<List<FeatureItem>>(featuresJson, JsonOpts);
            if (list != null)
                return list.Where(f => !string.IsNullOrEmpty(f.Name))
                           .ToDictionary(f => f.Name!, f => f.Enabled);
        }
        catch { /* formato desconhecido */ }

        return new Dictionary<string, bool>();
    }

    /// <summary>
    /// True se a feature está habilitada no plano.
    /// **Fail-open**: se o dicionário está vazio (plano sem features gravadas / estabelecimento
    /// legado sem `FeaturesEnabled`), libera — para não bloquear acesso já existente. O gating só
    /// nega quando o plano tem features registradas e a chave está ausente ou explicitamente false.
    /// </summary>
    public static bool IsEnabled(IReadOnlyDictionary<string, bool>? features, string key)
    {
        if (features == null || features.Count == 0) return true;
        return features.TryGetValue(key, out var enabled) && enabled;
    }

    private sealed class FeatureItem
    {
        public string? Name { get; set; }
        public bool Enabled { get; set; }
    }
}
