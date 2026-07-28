using Service;

namespace Extensions;

public static class ManipulationServicesExtensions
{
    public static IServiceCollection AddManipulationServices(this IServiceCollection services)
    {
        services.AddScoped<WeighingStepService>();
        
        return services;
    }
}
