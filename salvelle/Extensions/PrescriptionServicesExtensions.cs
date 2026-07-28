using Service;
using Service.Prescriptions;

namespace Extensions;

/// <summary>
/// Extensões para configuração de serviços de prescrições
/// </summary>
public static class PrescriptionServicesExtensions
{
    /// <summary>
    /// Adiciona os serviços de prescrições ao container DI
    /// </summary>
    public static IServiceCollection AddPrescriptionServices(this IServiceCollection services)
    {
        // Serviço de OCR com OpenAI
        services.AddScoped<OpenAIPrescriptionParserService>();

        // Serviço de matching de ingredientes (de Service/)
        services.AddScoped<Service.IngredientMatcherService>();

        // Serviço de orçamentos
        services.AddScoped<PrescriptionQuoteService>();

        // Serviço de workflow completo
        services.AddScoped<PrescriptionWorkflowService>();

        // Serviço de WhatsApp
        services.AddScoped<QuoteWhatsAppService>();

        return services;
    }
}