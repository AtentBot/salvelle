using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Models;
using Stripe;
using Stripe.Checkout;

// Aliases para evitar conflito com classes do projeto
using StripeCustomerService = Stripe.CustomerService;

namespace Service;

/// <summary>
/// Implementação do gateway de pagamento para Stripe
/// Fase 1-2: Apenas configuração e teste de conexão
/// Fase 3+: Implementação completa de pagamentos
/// </summary>
public class StripePaymentService : IPaymentGatewayService
{
    private readonly IEncryptionService _encryption;
    private readonly ILogger<StripePaymentService> _logger;

    public PaymentGatewayType GatewayType => PaymentGatewayType.Stripe;

    public StripePaymentService(
        IEncryptionService encryption,
        ILogger<StripePaymentService> logger)
    {
        _encryption = encryption;
        _logger = logger;
    }

    /// <summary>
    /// Configura o cliente Stripe com as credenciais do config
    /// </summary>
    private void ConfigureStripe(PaymentGatewayConfig config)
    {
        var secretKey = _encryption.Decrypt(config.SecretKeyEncrypted ?? "");
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("Secret Key não configurada para Stripe");

        StripeConfiguration.ApiKey = secretKey;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // FASE 1-2: TESTE DE CONEXÃO (IMPLEMENTADO)
    // ══════════════════════════════════════════════════════════════════════════════

    public async Task<PaymentTestResult> TestConnectionAsync(PaymentGatewayConfig config)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            ConfigureStripe(config);

            // Tenta listar 1 cliente para verificar se as credenciais funcionam
            var service = new StripeCustomerService();
            var options = new CustomerListOptions { Limit = 1 };
            
            var customers = await service.ListAsync(options);
            
            sw.Stop();
            
            return PaymentTestResult.Ok(
                $"Conexão estabelecida com sucesso. API Version: {StripeConfiguration.ApiVersion}",
                sw.ElapsedMilliseconds
            );
        }
        catch (StripeException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Erro ao testar conexão Stripe");
            
            return PaymentTestResult.Fail(
                $"Erro de autenticação Stripe: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Erro inesperado ao testar conexão Stripe");
            
            return PaymentTestResult.Fail($"Erro: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // FASE 3+: MÉTODOS DE PAGAMENTO (PLACEHOLDERS - IMPLEMENTAR DEPOIS)
    // ══════════════════════════════════════════════════════════════════════════════
    // Estes métodos serão implementados na Fase 3 quando integrar com o fluxo
    // de pagamento existente do Salvelle (StripeService)

    public Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request)
    {
        _logger.LogWarning("CreateCustomerAsync será implementado na Fase 3");
        return Task.FromResult(CustomerResult.Fail("Será implementado na Fase 3 - Use StripeService existente"));
    }

    public Task<PaymentResult> CreateChargeAsync(CreateChargeRequest request)
    {
        _logger.LogWarning("CreateChargeAsync será implementado na Fase 3");
        return Task.FromResult(PaymentResult.Fail("Será implementado na Fase 3 - Use StripeService existente"));
    }

    public Task<PaymentResult> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        _logger.LogWarning("CreateSubscriptionAsync será implementado na Fase 3");
        return Task.FromResult(PaymentResult.Fail("Será implementado na Fase 3 - Use StripeService existente"));
    }

    public Task<PaymentResult> CancelSubscriptionAsync(CancelSubscriptionRequest request)
    {
        _logger.LogWarning("CancelSubscriptionAsync será implementado na Fase 3");
        return Task.FromResult(PaymentResult.Fail("Será implementado na Fase 3 - Use StripeService existente"));
    }

    public Task<PaymentResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request)
    {
        _logger.LogWarning("UpdateSubscriptionAsync será implementado na Fase 3");
        return Task.FromResult(PaymentResult.Fail("Será implementado na Fase 3 - Use StripeService existente"));
    }

    public Task<WebhookResult> ProcessWebhookAsync(ProcessWebhookRequest request)
    {
        _logger.LogWarning("ProcessWebhookAsync será implementado na Fase 3");
        return Task.FromResult(new WebhookResult 
        { 
            Success = false, 
            Message = "Será implementado na Fase 3 - Use StripeService existente" 
        });
    }

    public Task<PaymentDetails?> GetPaymentDetailsAsync(string paymentId, PaymentGatewayConfig config)
    {
        _logger.LogWarning("GetPaymentDetailsAsync será implementado na Fase 3");
        return Task.FromResult<PaymentDetails?>(null);
    }

    public Task<SubscriptionDetails?> GetSubscriptionDetailsAsync(string subscriptionId, PaymentGatewayConfig config)
    {
        _logger.LogWarning("GetSubscriptionDetailsAsync será implementado na Fase 3");
        return Task.FromResult<SubscriptionDetails?>(null);
    }

    public Task<CheckoutResult> CreateCheckoutSessionAsync(CreateCheckoutRequest request)
    {
        _logger.LogWarning("CreateCheckoutSessionAsync será implementado na Fase 3");
        return Task.FromResult(CheckoutResult.Fail("Será implementado na Fase 3 - Use StripeService existente"));
    }
}
