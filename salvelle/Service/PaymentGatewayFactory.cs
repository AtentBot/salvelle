using Microsoft.Extensions.DependencyInjection;
using Models;

namespace Service;

/// <summary>
/// Interface para factory de gateways de pagamento
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// Obtém o serviço de pagamento para o tipo de gateway especificado
    /// </summary>
    IPaymentGatewayService GetService(PaymentGatewayType gatewayType);

    /// <summary>
    /// Obtém o serviço de pagamento baseado na configuração
    /// </summary>
    IPaymentGatewayService GetService(PaymentGatewayConfig config);

    /// <summary>
    /// Verifica se um tipo de gateway está disponível
    /// </summary>
    bool IsAvailable(PaymentGatewayType gatewayType);

    /// <summary>
    /// Lista todos os gateways disponíveis
    /// </summary>
    IEnumerable<PaymentGatewayType> GetAvailableGateways();
}

/// <summary>
/// Factory para resolver serviços de gateway de pagamento
/// </summary>
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<PaymentGatewayType, Type> _gatewayServices;

    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Registra os tipos de serviço para cada gateway
        _gatewayServices = new Dictionary<PaymentGatewayType, Type>
        {
            { PaymentGatewayType.Stripe, typeof(StripePaymentService) },
            { PaymentGatewayType.MercadoPago, typeof(MercadoPagoPaymentService) },
            { PaymentGatewayType.Abacatepay, typeof(AbacatepayPaymentService) }
        };
    }

    public IPaymentGatewayService GetService(PaymentGatewayType gatewayType)
    {
        if (!_gatewayServices.TryGetValue(gatewayType, out var serviceType))
        {
            throw new NotSupportedException($"Gateway de pagamento '{gatewayType}' não é suportado.");
        }

        var service = _serviceProvider.GetService(serviceType) as IPaymentGatewayService;
        
        if (service == null)
        {
            throw new InvalidOperationException($"Serviço para gateway '{gatewayType}' não está registrado no DI.");
        }

        return service;
    }

    public IPaymentGatewayService GetService(PaymentGatewayConfig config)
    {
        return GetService(config.GatewayType);
    }

    public bool IsAvailable(PaymentGatewayType gatewayType)
    {
        if (!_gatewayServices.TryGetValue(gatewayType, out var serviceType))
            return false;

        try
        {
            var service = _serviceProvider.GetService(serviceType);
            return service != null;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<PaymentGatewayType> GetAvailableGateways()
    {
        return _gatewayServices.Keys.Where(IsAvailable);
    }
}

/// <summary>
/// Placeholder para MercadoPago - implementação futura
/// </summary>
public class MercadoPagoPaymentService : IPaymentGatewayService
{
    public PaymentGatewayType GatewayType => PaymentGatewayType.MercadoPago;

    public Task<PaymentResult> CancelSubscriptionAsync(CancelSubscriptionRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<PaymentResult> CreateChargeAsync(CreateChargeRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<CheckoutResult> CreateCheckoutSessionAsync(CreateCheckoutRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<PaymentResult> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<PaymentDetails?> GetPaymentDetailsAsync(string paymentId, PaymentGatewayConfig config)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<SubscriptionDetails?> GetSubscriptionDetailsAsync(string subscriptionId, PaymentGatewayConfig config)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<WebhookResult> ProcessWebhookAsync(ProcessWebhookRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");

    public Task<PaymentTestResult> TestConnectionAsync(PaymentGatewayConfig config)
        => Task.FromResult(PaymentTestResult.Fail("MercadoPago será implementado na Fase 4"));

    public Task<PaymentResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request)
        => throw new NotImplementedException("MercadoPago será implementado na Fase 4");
}

/// <summary>
/// Placeholder para Abacatepay - implementação futura
/// </summary>
public class AbacatepayPaymentService : IPaymentGatewayService
{
    public PaymentGatewayType GatewayType => PaymentGatewayType.Abacatepay;

    public Task<PaymentResult> CancelSubscriptionAsync(CancelSubscriptionRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<PaymentResult> CreateChargeAsync(CreateChargeRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<CheckoutResult> CreateCheckoutSessionAsync(CreateCheckoutRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<PaymentResult> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<PaymentDetails?> GetPaymentDetailsAsync(string paymentId, PaymentGatewayConfig config)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<SubscriptionDetails?> GetSubscriptionDetailsAsync(string subscriptionId, PaymentGatewayConfig config)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<WebhookResult> ProcessWebhookAsync(ProcessWebhookRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");

    public Task<PaymentTestResult> TestConnectionAsync(PaymentGatewayConfig config)
        => Task.FromResult(PaymentTestResult.Fail("Abacatepay será implementado na Fase 4"));

    public Task<PaymentResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request)
        => throw new NotImplementedException("Abacatepay será implementado na Fase 4");
}

/// <summary>
/// Extensões para registro de serviços de pagamento
/// </summary>
public static class PaymentGatewayServiceExtensions
{
    /// <summary>
    /// Registra todos os serviços de gateway de pagamento
    /// </summary>
    public static IServiceCollection AddPaymentGateways(this IServiceCollection services)
    {
        // Encryption
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // Gateway Services
        services.AddScoped<StripePaymentService>();
        services.AddScoped<MercadoPagoPaymentService>();
        services.AddScoped<AbacatepayPaymentService>();

        // Factory
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        return services;
    }
}
