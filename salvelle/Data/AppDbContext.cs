using Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Core;
using Models.Employees;
using Models.Pharmacy;
using Models.Security;
using Models.Purchasing;
using Models.Auth;
using Models.Fiscal;
using Models.Billing;
using Models.Controlled;
using Models.Cart;
using Models.Support;

namespace Data;

/// <summary>
/// DbContext principal do Salvelle - Sistema de Gestão para Farmácias de Manipulação
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ════════════════════════════════════════════════════════════════════════
    // PORTAL DO CLIENTE - FÓRMULAS PERSONALIZADAS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tipos de produtos farmacêuticos (Cápsula, Creme, Solução, etc.)
    /// </summary>
    public DbSet<ProductType> ProductTypes { get; set; } = null!;

    /// <summary>
    /// Subtipos/apresentações (30 cápsulas, 60 cápsulas, 100g, etc.)
    /// </summary>
    public DbSet<ProductSubType> ProductSubTypes { get; set; } = null!;

    /// <summary>
    /// Fórmulas personalizadas criadas pelos clientes no portal
    /// </summary>
    public DbSet<CustomerFormula> CustomerFormulas { get; set; } = null!;

    /// <summary>
    /// Carrinhos de compras dos clientes (fórmulas personalizadas)
    /// </summary>
    public DbSet<Cart> Carts { get; set; } = null!;

    /// <summary>
    /// Itens dos carrinhos de compras
    /// </summary>
    public DbSet<CartItem> CartItems { get; set; } = null!;

    /// <summary>
    /// Itens do carrinho de fórmulas personalizadas
    /// </summary>
    public DbSet<FormulaCartItem> FormulaCartItems { get; set; } = null!;
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs { get; set; } = null!;

    /// <summary>
    /// Configurações de integrações do estabelecimento (Balança, Impressora, SNGPC, etc.)
    /// </summary>
    public DbSet<Controllers.Api.IntegrationConfig> IntegrationConfigs { get; set; } = null!;

    /// <summary>
    /// Logs de webhooks recebidos dos gateways
    /// </summary>
    public DbSet<PaymentWebhookLog> PaymentWebhookLogs { get; set; } = null!;

    /// <summary>
    /// Tokens de recuperação de senha dos admins SaaS
    /// </summary>
    public DbSet<SaasAdminPasswordReset> SaasAdminPasswordResets { get; set; } = null!;



    // ════════════════════════════════════════════════════════════════════════
    // CORE & INFRAESTRUTURA
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Establishment> Establishments => Set<Establishment>();
    public DbSet<AccessLevel> AccessLevels => Set<AccessLevel>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<ClientOnboarding> ClientOnboardings { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // SAAS - BILLING & SUBSCRIPTIONS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Planos de assinatura disponíveis (Básico, Profissional, Enterprise)
    /// </summary>
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;

    /// <summary>
    /// Assinaturas ativas/canceladas dos estabelecimentos
    /// </summary>
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<SubscriptionCancellation> SubscriptionCancellations { get; set; } = null!;

    /// <summary>
    /// Faturas de cobrança da assinatura SaaS (Farmácia → Salvelle via Stripe)
    /// </summary>
    public DbSet<SubscriptionInvoice> SubscriptionInvoices { get; set; } = null!;

    /// <summary>
    /// Métodos de pagamento cadastrados (cartões de crédito)
    /// </summary>
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;

    /// <summary>
    /// Administradores do sistema SaaS
    /// </summary>
    public DbSet<SaasAdmin> SaasAdmins { get; set; } = null!;

    /// <summary>
    /// Sessões dos administradores SaaS
    /// </summary>
    public DbSet<SaasAdminSession> SaasAdminSessions { get; set; } = null!;

    /// <summary>
    /// Configurações de precificação por estabelecimento
    /// </summary>
    public DbSet<EstablishmentPricingSettings> EstablishmentPricingSettings { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // FUNCIONÁRIOS & RH
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSession> EmployeeSessions => Set<EmployeeSession>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<EmployeeJobHistory> EmployeeJobHistories => Set<EmployeeJobHistory>();
    public DbSet<EmployeeBenefit> EmployeeBenefits => Set<EmployeeBenefit>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    // ════════════════════════════════════════════════════════════════════════
    // SEGURANÇA & PERMISSÕES
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AccessProfile> AccessProfiles => Set<AccessProfile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ════════════════════════════════════════════════════════════════════════
    // AUTENTICAÇÃO & SEGURANÇA
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<TwoFactorToken> TwoFactorAuths { get; set; } = null!;
    public DbSet<LoginAttempt> LoginAttempts { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // FARMÁCIA - ESTOQUE & MATÉRIAS-PRIMAS
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<RawMaterialCatalog> RawMaterialsCatalog => Set<RawMaterialCatalog>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ControlledSubstanceMovement> ControlledSubstanceMovements => Set<ControlledSubstanceMovement>();
    public DbSet<ControlledSubstanceBalance> ControlledSubstanceBalances => Set<ControlledSubstanceBalance>();

    // ════════════════════════════════════════════════════════════════════════
    // FARMÁCIA - FÓRMULAS & MANIPULAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Formula> Formulas => Set<Formula>();
    public DbSet<FormulaComponent> FormulaComponents => Set<FormulaComponent>();
    public DbSet<ManipulationOrder> ManipulationOrders => Set<ManipulationOrder>();
    public DbSet<ManipulationStep> ManipulationSteps { get; set; } = null!;
    public DbSet<ManipulationPhoto> ManipulationPhotos { get; set; } = null!;
    public DbSet<ManipulationLoss> ManipulationLosses { get; set; } = null!;
    public DbSet<ManipulationLeftover> ManipulationLeftovers { get; set; } = null!;
    public DbSet<DualVerification> DualVerifications { get; set; } = null!;
    public DbSet<ProductionRecord> ProductionRecords { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // FORNECEDORES & COMPRAS
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierContact> SupplierContacts { get; set; } = null!;
    public DbSet<SupplierCertificate> SupplierCertificates { get; set; } = null!;
    public DbSet<SupplierEvaluation> SupplierEvaluations { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
    public DbSet<BatchReceiving> BatchReceivings { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CLIENTES & PRESCRIÇÕES
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionFile> PrescriptionFiles { get; set; } = null!;
    public DbSet<SpecialPrescriptionControl> SpecialPrescriptionControls => Set<SpecialPrescriptionControl>();

    // ════════════════════════════════════════════════════════════════════════
    // VENDAS & PAGAMENTOS
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    // ════════════════════════════════════════════════════════════════════════
    // FISCAL - NOTAS FISCAIS (NF-e/NFC-e)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Notas Fiscais emitidas pela farmácia (Cliente → Farmácia via SEFAZ)
    /// </summary>
    public DbSet<FiscalInvoice> FiscalInvoices { get; set; } = null!;

    /// <summary>
    /// Itens das Notas Fiscais com detalhes de tributação
    /// </summary>
    public DbSet<FiscalInvoiceItem> FiscalInvoiceItems { get; set; } = null!;

    /// <summary>
    /// Configurações fiscais por estabelecimento (certificado, séries, CSC)
    /// </summary>
    public DbSet<FiscalConfig> FiscalConfigs { get; set; } = null!;

    /// <summary>
    /// Fila de contingência para notas não transmitidas
    /// </summary>
    public DbSet<FiscalQueue> FiscalQueues { get; set; } = null!;

    /// <summary>
    /// Log de eventos fiscais (emissão, cancelamento, erros)
    /// </summary>
    public DbSet<FiscalLog> FiscalLogs { get; set; } = null!;

    /// <summary>
    /// Inutilização de numeração fiscal
    /// </summary>
    public DbSet<FiscalNumberGap> FiscalNumberGaps { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CAIXA & FINANCEIRO
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CashRegister> CashRegisters { get; set; } = null!;
    public DbSet<CashMovement> CashMovements { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // RÓTULOS ANVISA
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<LabelTemplate> LabelTemplates => Set<LabelTemplate>();
    public DbSet<GeneratedLabel> GeneratedLabels => Set<GeneratedLabel>();
    public DbSet<LabelPrintLog> LabelPrintLogs => Set<LabelPrintLog>();

    // ════════════════════════════════════════════════════════════════════════
    // CONFIGURAÇÕES
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
    public DbSet<PrescriptionQuote> PrescriptionQuotes { get; set; } = null!;
    public DbSet<ManipulationOrderComponent> ManipulationOrderComponents { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // PORTAL DO CLIENTE
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CustomerAuth> CustomerAuths { get; set; } = null!;
    public DbSet<CustomerSession> CustomerSessions { get; set; } = null!;
    public DbSet<EstablishmentQRCode> EstablishmentQRCodes { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CATÁLOGO & E-COMMERCE
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CatalogCategory> CatalogCategories { get; set; } = null!;
    public DbSet<CatalogProduct> CatalogProducts { get; set; } = null!;
    public DbSet<CustomerCart> CustomerCarts { get; set; } = null!;
    public DbSet<CustomerCartItem> CustomerCartItems { get; set; } = null!;
    public DbSet<OnlineOrder> OnlineOrders { get; set; } = null!;
    public DbSet<OnlineOrderItem> OnlineOrderItems { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CONTROLADOS - SNGPC & COMPLIANCE
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aprovações farmacêuticas de manipulações com substâncias controladas
    /// </summary>
    public DbSet<PharmacistApproval> PharmacistApprovals { get; set; } = null!;

    /// <summary>
    /// Inventários físicos mensais de substâncias controladas
    /// </summary>
    public DbSet<ControlledInventoryCheck> ControlledInventoryChecks { get; set; } = null!;

    /// <summary>
    /// Itens conferidos em cada inventário de controlados
    /// </summary>
    public DbSet<ControlledInventoryItem> ControlledInventoryItems { get; set; } = null!;

    /// <summary>
    /// Certificações de fornecedores (AFE, AE, BPF, Alvará)
    /// </summary>
    public DbSet<SupplierCertification> SupplierCertifications { get; set; } = null!;

    /// <summary>
    /// Scores de qualidade de fornecedores
    /// </summary>
    public DbSet<SupplierQualityScore> SupplierQualityScores { get; set; } = null!;

    /// <summary>
    /// Solicitações de acesso de auditores externos (VISA, CRF, ANVISA)
    /// </summary>
    public DbSet<AuditorAccessRequest> AuditorAccessRequests { get; set; } = null!;

    /// <summary>
    /// Log de ações de auditores externos
    /// </summary>
    public DbSet<AuditorAccessLog> AuditorAccessLogs { get; set; } = null!;


    /// <summary>
    /// Log de análises farmacêuticas de fórmulas personalizadas
    /// </summary>
    public DbSet<PharmaceuticalAnalysisLog> PharmaceuticalAnalysisLogs { get; set; } = null!;

    /// <summary>
    /// Reembolsos de fórmulas reprovadas pela análise farmacêutica
    /// </summary>
    public DbSet<Refund> Refunds { get; set; } = null!;

    public DbSet<ActiveIngredient> ActiveIngredients { get; set; }

    // ════════════════════════════════════════════════════════════════════════
    // FORMAS FARMACÊUTICAS & PRECIFICAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Formas farmacêuticas disponíveis (Cápsula, Creme, etc.)
    /// </summary>
    public DbSet<PharmaceuticalForm> PharmaceuticalForms { get; set; } = null!;

    /// <summary>
    /// Subtipos de formas farmacêuticas (Cápsula 00, Creme Lanette, etc.)
    /// </summary>
    public DbSet<PharmaceuticalFormSubtype> PharmaceuticalFormSubtypes { get; set; } = null!;

    /// <summary>
    /// Composições de subtipos (matérias-primas que compõem bases/veículos)
    /// </summary>
    public DbSet<PharmaceuticalFormComposition> PharmaceuticalFormCompositions { get; set; } = null!;

    /// <summary>
    /// Tabela de referência de tamanhos de cápsulas
    /// </summary>
    public DbSet<CapsuleSizeReference> CapsuleSizeReferences { get; set; } = null!;

    /// <summary>
    /// Configuração de precificação por estabelecimento (taxas, impostos, markup)
    /// </summary>
    public DbSet<EstablishmentPricingConfig> EstablishmentPricingConfigs { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CUPONS DE DESCONTO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cupons de desconto promocionais
    /// </summary>
    public DbSet<Coupon> Coupons { get; set; } = null!;

    /// <summary>
    /// Histórico de uso de cupons por cliente
    /// </summary>
    public DbSet<CouponUsage> CouponUsages { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // MARKETPLACE
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Models.Marketplace.PlatformCommission> PlatformCommissions { get; set; } = null!;
    public DbSet<Models.Marketplace.PlatformTransaction> PlatformTransactions { get; set; } = null!;
    public DbSet<Models.Marketplace.PharmacyRating> PharmacyRatings { get; set; } = null!;
    public DbSet<Models.Marketplace.ProductRating> ProductRatings { get; set; } = null!;
    public DbSet<Models.Marketplace.DeliveryEstimate> DeliveryEstimates { get; set; } = null!;
    public DbSet<Models.Marketplace.CustomerDevice> CustomerDevices { get; set; } = null!;
    public DbSet<Models.Marketplace.CustomerAddress> CustomerAddresses { get; set; } = null!;
    public DbSet<Models.Marketplace.PharmacyPayoutAccount> PharmacyPayoutAccounts { get; set; } = null!;
    public DbSet<Models.Marketplace.SearchHistory> SearchHistories { get; set; } = null!;
    public DbSet<PendingTwoFactorSession> PendingTwoFactorSessions { get; set; } = null!;
    public DbSet<RevokedJwt> RevokedJwts { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // SUPORTE & MONITORAMENTO
    // ════════════════════════════════════════════════════════════════════════
    public DbSet<SupportTicket> SupportTickets { get; set; } = null!;
    public DbSet<SupportTicketMessage> SupportTicketMessages { get; set; } = null!;
    public DbSet<WhatsAppInstanceStatus> WhatsAppInstanceStatuses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ──────────────────────────────────────────────────────────────────
        // PAYMENT GATEWAY CONFIG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PaymentGatewayConfig>(entity =>
        {
            entity.ToTable("payment_gateway_configs");
            entity.HasKey(e => e.Id);

            // Conversão de enum para string
            entity.Property(e => e.GatewayType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Environment)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.LastTestStatus)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Índices
            entity.HasIndex(e => e.GatewayType);
            entity.HasIndex(e => e.Environment);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDefault);

            // Relacionamentos com SaasAdmin
            entity.HasOne(e => e.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.CreatedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.UpdatedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ──────────────────────────────────────────────────────────────────
        // PAYMENT WEBHOOK LOG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PaymentWebhookLog>(entity =>
        {
            entity.ToTable("payment_webhook_logs");
            entity.HasKey(e => e.Id);

            // Conversão de enum para string
            entity.Property(e => e.GatewayType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Índices
            entity.HasIndex(e => e.GatewayType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ExternalEventId);
            entity.HasIndex(e => e.SubscriptionId);

            // Relacionamentos
            entity.HasOne(e => e.GatewayConfig)
                .WithMany()
                .HasForeignKey(e => e.GatewayConfigId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ──────────────────────────────────────────────────────────────────
        // SAAS ADMIN PASSWORD RESET
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SaasAdminPasswordReset>(entity =>
        {
            entity.ToTable("saas_admin_password_resets");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.SaasAdminId);
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.SaasAdmin)
                .WithMany()
                .HasForeignKey(e => e.SaasAdminId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // ESTABLISHMENT PRICING SETTINGS
        // 
        modelBuilder.Entity<EstablishmentPricingSettings>(entity =>
        {
            entity.ToTable("establishment_pricing_settings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.EstablishmentId)
                .HasColumnName("establishment_id")
                .IsRequired();

            entity.Property(e => e.InflationRateMonthly)
                .HasColumnName("inflation_rate_monthly")
                .HasPrecision(5, 2)
                .HasDefaultValue(0.5m);

            entity.Property(e => e.SafetyMarginPercent)
                .HasColumnName("safety_margin_percent")
                .HasPrecision(5, 2)
                .HasDefaultValue(10m);

            entity.Property(e => e.DefaultProfitMargin)
                .HasColumnName("default_profit_margin")
                .HasPrecision(5, 2)
                .HasDefaultValue(100m);

            entity.Property(e => e.PriceValidityDays)
                .HasColumnName("price_validity_days")
                .HasDefaultValue(180);

            entity.Property(e => e.ManipulationFee)
                .HasColumnName("manipulation_fee")
                .HasPrecision(10, 2)
                .HasDefaultValue(25m);

            entity.Property(e => e.DefaultPackagingCost)
                .HasColumnName("default_packaging_cost")
                .HasPrecision(10, 2)
                .HasDefaultValue(5m);

            entity.Property(e => e.AlertOnEstimated)
                .HasColumnName("alert_on_estimated")
                .HasDefaultValue(true);

            entity.Property(e => e.BlockWithoutStock)
                .HasColumnName("block_without_stock")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            // Índice único por estabelecimento
            entity.HasIndex(e => e.EstablishmentId)
                .IsUnique()
                .HasDatabaseName("uq_establishment_pricing_settings");

            // Relacionamento com Establishment
            entity.HasOne(e => e.Establishment)
                .WithOne()
                .HasForeignKey<EstablishmentPricingSettings>(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // APLICAR CONFIGURAÇÕES VIA FLUENT API
        // ──────────────────────────────────────────────────────────────────

        ApplyEntityConfigurations(modelBuilder);

        // ──────────────────────────────────────────────────────────────────
        // CONFIGURAÇÕES ESPECÍFICAS
        // ──────────────────────────────────────────────────────────────────

        ConfigureManipulationWorkflow(modelBuilder);
        ConfigureManipulationEntities(modelBuilder);
        ConfigureLabelEntities(modelBuilder);
        ConfigureSubscriptionInvoices(modelBuilder);
        ConfigureFiscalInvoices(modelBuilder);
        ConfigureFiscalEntities(modelBuilder); // NOVO - Entidades fiscais complementares
        ConfigureSaasAdminEntities(modelBuilder);
        ConfigurePortalClienteEntities(modelBuilder);
        ConfigureCatalogoEntities(modelBuilder);
        ConfigureControladosEntities(modelBuilder);
        ConfigureMarketplaceEntities(modelBuilder);

        // ──────────────────────────────────────────────────────────────────
        // SEED DE DADOS INICIAIS
        // ──────────────────────────────────────────────────────────────────

        // Product Types (Global - sem EstablishmentId)
        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.ToTable("product_types");
            entity.HasKey(e => e.Id);
            // Colunas em PascalCase (como existem no banco)
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.PharmaceuticalForm).HasColumnName("PharmaceuticalForm");
            entity.Property(e => e.Category).HasColumnName("Category");
            entity.Property(e => e.DisplayOrder).HasColumnName("DisplayOrder");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");

            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<ProductSubType>(entity =>
        {
            entity.ToTable("product_sub_types");
            entity.HasKey(e => e.Id);
            // Colunas em PascalCase (como existem no banco)
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ProductTypeId).HasColumnName("ProductTypeId");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.Description).HasColumnName("Description");
            entity.Property(e => e.StandardUnit).HasColumnName("StandardUnit");
            entity.Property(e => e.StandardQuantity).HasColumnName("StandardQuantity");
            entity.Property(e => e.PriceModifier).HasColumnName("PriceModifier");
            entity.Property(e => e.ManipulationCostBase).HasColumnName("ManipulationCostBase");
            entity.Property(e => e.DisplayOrder).HasColumnName("DisplayOrder");
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");

            entity.HasIndex(e => e.ProductTypeId);
        });

        modelBuilder.Entity<CustomerFormula>(entity =>
        {
            entity.ToTable("customer_formulas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.EstablishmentId).HasColumnName("establishment_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerName).HasColumnName("customer_name");
            entity.Property(e => e.CustomerPhone).HasColumnName("customer_phone");
            entity.Property(e => e.CustomerEmail).HasColumnName("customer_email");
            entity.Property(e => e.ProductTypeId).HasColumnName("product_type_id");
            entity.Property(e => e.ProductSubTypeId).HasColumnName("product_sub_type_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.AdditionalIngredients).HasColumnName("additional_ingredients");
            entity.Property(e => e.CustomerNotes).HasColumnName("customer_notes");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.PharmacistId).HasColumnName("pharmacist_id");
            entity.Property(e => e.PharmaceuticalAnalysis).HasColumnName("pharmaceutical_analysis");
            entity.Property(e => e.AnalyzedAt).HasColumnName("analyzed_at");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.RejectedAt).HasColumnName("rejected_at");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.AdjustmentRequest).HasColumnName("adjustment_request");
            entity.Property(e => e.RequiresPrescription).HasColumnName("requires_prescription");
            entity.Property(e => e.IsControlledSubstance).HasColumnName("is_controlled_substance");
            entity.Property(e => e.HasIncompatibilities).HasColumnName("has_incompatibilities");
            entity.Property(e => e.IncompatibilityDetails).HasColumnName("incompatibility_details");
            entity.Property(e => e.EstimatedShelfLifeDays).HasColumnName("estimated_shelf_life_days");
            entity.Property(e => e.EstimatedPrice).HasColumnName("estimated_price");
            entity.Property(e => e.FinalPrice).HasColumnName("final_price");
            entity.Property(e => e.DiscountApplied).HasColumnName("discount_applied");
            entity.Property(e => e.PrescriptionQuoteId).HasColumnName("prescription_quote_id");
            entity.Property(e => e.ManipulationOrderId).HasColumnName("manipulation_order_id");
            entity.Property(e => e.OnlineOrderId).HasColumnName("online_order_id");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaidAmount).HasColumnName("paid_amount");
            entity.Property(e => e.RequiresRefund).HasColumnName("requires_refund");
            entity.Property(e => e.RefundedAt).HasColumnName("refunded_at");
            entity.Property(e => e.RefundAmount).HasColumnName("refund_amount");
            entity.Property(e => e.SessionToken).HasColumnName("session_token");
            entity.Property(e => e.SessionExpiresAt).HasColumnName("session_expires_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.CreatedByEmployeeId).HasColumnName("created_by_employee_id");

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Code);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("carts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.EstablishmentId).HasColumnName("establishment_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.CustomerId);
        });

        // CartItem (Carrinho baseado em sessão)
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SessionToken).HasColumnName("session_token");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.EstablishmentId).HasColumnName("establishment_id");
            entity.Property(e => e.ItemType).HasColumnName("item_type");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");
            entity.Property(e => e.RequiresPrescription).HasColumnName("requires_prescription");
            entity.Property(e => e.IsControlled).HasColumnName("is_controlled");
            entity.Property(e => e.IsCustomFormula).HasColumnName("is_custom_formula");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");

            entity.HasIndex(e => e.SessionToken);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.EstablishmentId);
        });

        // FormulaCartItem (Itens do carrinho de fórmulas)
        modelBuilder.Entity<FormulaCartItem>(entity =>
        {
            entity.ToTable("formula_cart_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CustomerFormulaId).HasColumnName("customer_formula_id");
            entity.Property(e => e.CatalogProductId).HasColumnName("catalog_product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.CartId);
            entity.HasIndex(e => e.CustomerFormulaId);

            entity.HasOne(e => e.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurar entidades de Formas Farmacêuticas e Precificação
        ConfigurePharmaceuticalFormsEntities(modelBuilder);

        SeedInitialData(modelBuilder);
    }

    // ════════════════════════════════════════════════════════════════════════
    // MÉTODOS DE CONFIGURAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aplica todas as configurações de entidades via IEntityTypeConfiguration
    /// </summary>
    private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new JobPositionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeJobHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSessionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeBenefitConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierContactConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierCertificateConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierEvaluationConfiguration());
    }





    /// <summary>
    /// Configura entidades de manipulação (Losses, Leftovers, Verification, Production)
    /// </summary>
    private void ConfigureManipulationEntities(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ActiveIngredient>(entity =>
        {
            entity.ToTable("active_ingredients");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.NormalizedName)
                .HasMaxLength(200);

            entity.Property(e => e.PricePerUnit)
                .HasColumnType("decimal(10,4)");

            entity.HasIndex(e => e.NormalizedName);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Popularity);
        });

        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION LOSS
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationLoss>(entity =>
        {
            entity.ToTable("manipulation_losses");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RawMaterial)
                .WithMany()
                .HasForeignKey(e => e.RawMaterialId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RegisteredByEmployee)
                .WithMany()
                .HasForeignKey(e => e.RegisteredByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION LEFTOVER
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationLeftover>(entity =>
        {
            entity.ToTable("manipulation_leftovers");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RawMaterial)
                .WithMany()
                .HasForeignKey(e => e.RawMaterialId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RegisteredByEmployee)
                .WithMany()
                .HasForeignKey(e => e.RegisteredByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // DUAL VERIFICATION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<DualVerification>(entity =>
        {
            entity.ToTable("dual_verifications");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FirstVerifier)
                .WithMany()
                .HasForeignKey(e => e.FirstVerifierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SecondVerifier)
                .WithMany()
                .HasForeignKey(e => e.SecondVerifierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // PRODUCTION RECORD
        // ──────────────────────────────────────────────────────────────────

        // ProductionRecord
        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.ToTable("production_records");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ManipulationOrderId).IsUnique();

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProducedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.ProducedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VerifiedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.VerifiedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApprovedByPharmacist)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByPharmacistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // STOCK MOVEMENT (Employee relationships)
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasOne(sm => sm.PerformedByEmployee)
                .WithMany(e => e.StockMovements)
                .HasForeignKey(sm => sm.PerformedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sm => sm.AuthorizedByEmployee)
                .WithMany()
                .HasForeignKey(sm => sm.AuthorizedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePortalClienteEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER AUTH
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerAuth>(entity =>
        {
            entity.ToTable("CustomerAuths");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.Cpf).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.IsVerified);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER SESSION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerSession>(entity =>
        {
            entity.ToTable("CustomerSessions");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.CustomerAuth)
                  .WithMany(a => a.Sessions)
                  .HasForeignKey(e => e.CustomerAuthId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CurrentEstablishment)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentEstablishmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastActivityAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        // ──────────────────────────────────────────────────────────────────
        // ESTABLISHMENT QR CODE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<EstablishmentQRCode>(entity =>
        {
            entity.ToTable("EstablishmentQRCodes");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.ScanCount)
                .HasDefaultValue(0);
        });
    }

    /// <summary>
    /// Configura entidades do Catálogo e E-commerce
    /// </summary>
    private void ConfigureCatalogoEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // CATALOG CATEGORY
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CatalogCategory>(entity =>
        {
            entity.ToTable("CatalogCategories");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CATALOG PRODUCT
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CatalogProduct>(entity =>
        {
            entity.ToTable("CatalogProducts");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER CART
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerCart>(entity =>
        {
            entity.ToTable("CustomerCarts");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.CustomerId, e.EstablishmentId }).IsUnique();

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER CART ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerCartItem>(entity =>
        {
            entity.ToTable("CustomerCartItems");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CartId);

            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(e => e.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // ONLINE ORDER
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<OnlineOrder>(entity =>
        {
            entity.ToTable("OnlineOrders");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // ONLINE ORDER ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<OnlineOrderItem>(entity =>
        {
            entity.ToTable("OnlineOrderItems");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderId);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configura workflow de manipulação (Steps, Photos)
    /// </summary>
    private void ConfigureManipulationWorkflow(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION STEP
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationStep>()
            .HasOne(s => s.ManipulationOrder)
            .WithMany()
            .HasForeignKey(s => s.ManipulationOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManipulationStep>()
            .HasOne(s => s.PerformedByEmployee)
            .WithMany()
            .HasForeignKey(s => s.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationStep>()
            .HasOne(s => s.CheckedByEmployee)
            .WithMany()
            .HasForeignKey(s => s.CheckedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Coluna JSON para StepData
        modelBuilder.Entity<ManipulationStep>()
            .Property(s => s.StepData)
            .HasColumnType("jsonb");

        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION PHOTO
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationPhoto>()
            .HasOne(p => p.ManipulationOrder)
            .WithMany()
            .HasForeignKey(p => p.ManipulationOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManipulationPhoto>()
            .HasOne(p => p.ManipulationStep)
            .WithMany(s => s.Photos)
            .HasForeignKey(p => p.ManipulationStepId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManipulationPhoto>()
            .HasOne(p => p.CapturedByEmployee)
            .WithMany()
            .HasForeignKey(p => p.CapturedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Configura entidades de rótulos ANVISA
    /// </summary>
    private void ConfigureLabelEntities(ModelBuilder modelBuilder)
    {
        // Implementação existente de labels...
        // (mantém a implementação atual se houver)
    }

    /// <summary>
    /// Configura SubscriptionInvoices (Faturas SaaS - Farmácia → Salvelle)
    /// </summary>
    private void ConfigureSubscriptionInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionInvoice>(entity =>
        {
            entity.ToTable("subscription_invoices");
            entity.HasKey(e => e.Id);

            entity.HasKey(e => e.Id);
            entity.ToTable("subscription_invoices");

            // ──────────────────────────────────────────────────────────────
            // RELACIONAMENTOS
            // ──────────────────────────────────────────────────────────────

            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ──────────────────────────────────────────────────────────────
            // ÍNDICES PARA PERFORMANCE
            // ──────────────────────────────────────────────────────────────

            entity.HasIndex(e => e.SubscriptionId)
                .HasDatabaseName("idx_subscription_invoices_subscription_id");

            entity.HasIndex(e => e.StripeInvoiceId)
                .HasDatabaseName("idx_subscription_invoices_stripe_invoice_id")
                .IsUnique()
                .HasFilter("stripe_invoice_id IS NOT NULL");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_subscription_invoices_status");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_subscription_invoices_created_at");

            entity.HasIndex(e => new { e.Status, e.DueDate })
                .HasDatabaseName("idx_subscription_invoices_status_due_date");

            // ──────────────────────────────────────────────────────────────
            // VALORES PADRÃO
            // ──────────────────────────────────────────────────────────────

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDING");
        });
    }

    /// <summary>
    /// Configura FiscalInvoices (Notas Fiscais - Cliente → Farmácia)
    /// </summary>
    private void ConfigureFiscalInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("fiscal_invoices");

            // ──────────────────────────────────────────────────────────────
            // RELACIONAMENTOS
            // ──────────────────────────────────────────────────────────────

            entity.HasOne(e => e.Sale)
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamento com itens
            entity.HasMany(e => e.Items)
                .WithOne(i => i.FiscalInvoice)
                .HasForeignKey(i => i.FiscalInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ──────────────────────────────────────────────────────────────
            // ÍNDICES PARA PERFORMANCE
            // ──────────────────────────────────────────────────────────────

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_invoices_establishment_id");

            entity.HasIndex(e => e.SaleId)
                .HasDatabaseName("idx_fiscal_invoices_sale_id");

            entity.HasIndex(e => e.InvoiceKey)
                .HasDatabaseName("idx_fiscal_invoices_invoice_key")
                .IsUnique()
                .HasFilter("invoice_key IS NOT NULL");

            entity.HasIndex(e => new { e.EstablishmentId, e.InvoiceNumber, e.Series })
                .HasDatabaseName("idx_fiscal_invoices_number_series")
                .IsUnique();

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_fiscal_invoices_status");

            entity.HasIndex(e => e.IssueDate)
                .HasDatabaseName("idx_fiscal_invoices_issue_date");

            entity.HasIndex(e => new { e.EstablishmentId, e.IssueDate })
                .HasDatabaseName("idx_fiscal_invoices_establishment_issue_date");

            // ──────────────────────────────────────────────────────────────
            // VALORES PADRÃO
            // ──────────────────────────────────────────────────────────────

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDENTE");

            entity.Property(e => e.InvoiceType)
                .HasDefaultValue("NFCE");
        });
    }

    /// <summary>
    /// Configura entidades fiscais complementares (Config, Items, Queue, Logs, Gaps)
    /// NOVO - Adicionado para módulo fiscal completo
    /// </summary>
    private void ConfigureFiscalEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // FISCAL INVOICE ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalInvoiceItem>(entity =>
        {
            entity.ToTable("fiscal_invoice_items");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.FiscalInvoiceId)
                .HasDatabaseName("idx_fiscal_invoice_items_invoice");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL CONFIG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalConfig>(entity =>
        {
            entity.ToTable("fiscal_configs");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .IsUnique()
                .HasDatabaseName("idx_fiscal_configs_establishment");

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.Environment)
                .HasDefaultValue("HOMOLOGACAO");

            entity.Property(e => e.TaxRegime)
                .HasDefaultValue("SIMPLES_NACIONAL");
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL QUEUE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalQueue>(entity =>
        {
            entity.ToTable("fiscal_queue");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_queue_establishment");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_fiscal_queue_status");

            entity.HasIndex(e => e.NextAttempt)
                .HasDatabaseName("idx_fiscal_queue_next");

            entity.HasOne(e => e.Sale)
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FiscalInvoice)
                .WithMany()
                .HasForeignKey(e => e.FiscalInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDENTE");

            entity.Property(e => e.Attempts)
                .HasDefaultValue(0);

            entity.Property(e => e.MaxAttempts)
                .HasDefaultValue(3);
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL LOG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalLog>(entity =>
        {
            entity.ToTable("fiscal_logs");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_logs_establishment");

            entity.HasIndex(e => e.FiscalInvoiceId)
                .HasDatabaseName("idx_fiscal_logs_invoice");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_fiscal_logs_date");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL NUMBER GAP (Inutilização)
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalNumberGap>(entity =>
        {
            entity.ToTable("fiscal_number_gaps");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_gaps_establishment");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDENTE");
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEED DE DADOS INICIAIS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Seed de dados iniciais (AccessLevels e Permissions)
    /// </summary>
    private void SeedInitialData(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // SEED DE NÍVEIS DE ACESSO
        // ──────────────────────────────────────────────────────────────────

        var ownerAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var managerAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var employeeAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var userAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000004");

        modelBuilder.Entity<AccessLevel>().HasData(
            new AccessLevel
            {
                Id = ownerAccessLevelId,
                Code = "owner",
                Name = "Proprietário",
                Description = "Acesso total ao sistema, incluindo configurações críticas e gestão financeira",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = managerAccessLevelId,
                Code = "manager",
                Name = "Gerente",
                Description = "Acesso de gerenciamento com permissões administrativas e de supervisão",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = employeeAccessLevelId,
                Code = "employee",
                Name = "Funcionário",
                Description = "Acesso básico para funcionários operacionais",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = userAccessLevelId,
                Code = "user",
                Name = "Usuário",
                Description = "Acesso limitado para usuários externos ou clientes",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // ──────────────────────────────────────────────────────────────────
        // SEED DE PERMISSÕES
        // ──────────────────────────────────────────────────────────────────

        SeedPermissions(modelBuilder);
    }

    /// <summary>
    /// Seed de permissões do sistema
    /// </summary>
    private void SeedPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new[]
        {
            // ══════════════════════════════════════════════════════════════
            // INVENTORY PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000101"),
                ResourceAction = "inventory.read",
                Resource = "inventory",
                Action = "read",
                Category = "Inventory",
                DisplayName = "Visualizar Estoque",
                Description = "Permite visualizar informações de estoque",
                Scope = "Own",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000102"),
                ResourceAction = "inventory.write",
                Resource = "inventory",
                Action = "write",
                Category = "Inventory",
                DisplayName = "Editar Estoque",
                Description = "Permite adicionar e editar itens de estoque",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ══════════════════════════════════════════════════════════════
            // SALES PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000201"),
                ResourceAction = "sales.create",
                Resource = "sales",
                Action = "create",
                Category = "Sales",
                DisplayName = "Criar Vendas",
                Description = "Permite criar novas vendas",
                Scope = "Own",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000202"),
                ResourceAction = "sales.read",
                Resource = "sales",
                Action = "read",
                Category = "Sales",
                DisplayName = "Visualizar Vendas",
                Description = "Permite visualizar vendas",
                Scope = "Own",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ══════════════════════════════════════════════════════════════
            // REPORTS PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000301"),
                ResourceAction = "reports.read",
                Resource = "reports",
                Action = "read",
                Category = "Reports",
                DisplayName = "Visualizar Relatórios",
                Description = "Permite visualizar relatórios",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000302"),
                ResourceAction = "reports.export",
                Resource = "reports",
                Action = "export",
                Category = "Reports",
                DisplayName = "Exportar Relatórios",
                Description = "Permite exportar relatórios",
                Scope = "Establishment",
                RiskLevel = "High",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ══════════════════════════════════════════════════════════════
            // SETTINGS PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000401"),
                ResourceAction = "settings.update",
                Resource = "settings",
                Action = "update",
                Category = "Settings",
                DisplayName = "Alterar Configurações",
                Description = "Permite alterar configurações do sistema",
                Scope = "Establishment",
                RiskLevel = "Critical",
                RequiresTwoFactor = true,
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }

    private void ConfigureSaasAdminEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // SAAS ADMIN
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SaasAdmin>(entity =>
        {
            entity.ToTable("saas_admins");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        // ──────────────────────────────────────────────────────────────────
        // SAAS ADMIN SESSION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SaasAdminSession>(entity =>
        {
            entity.ToTable("saas_admin_sessions");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.SaasAdminId);

            entity.HasOne(e => e.SaasAdmin)
                .WithMany(a => a.Sessions)
                .HasForeignKey(e => e.SaasAdminId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastActivityAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<SaasAdminPasswordReset>(entity =>
        {
            entity.ToTable("saas_admin_password_resets");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.SaasAdminId)
                .HasColumnName("saas_admin_id")
                .IsRequired();

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(500);

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at");

            entity.Property(e => e.IsUsed)
                .HasColumnName("is_used")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            // Relacionamento
            entity.HasOne(e => e.SaasAdmin)
                .WithMany()
                .HasForeignKey(e => e.SaasAdminId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.SaasAdminId);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }

    private void ConfigureControladosEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // PHARMACIST APPROVALS
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PharmacistApproval>(entity =>
        {
            entity.ToTable("pharmacist_approvals");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.ManipulationOrderId);
            entity.HasIndex(e => e.PharmacistEmployeeId);
            entity.HasIndex(e => e.ApprovalStatus);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CONTROLLED INVENTORY CHECK
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ControlledInventoryCheck>(entity =>
        {
            entity.ToTable("controlled_inventory_checks");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => new { e.ReferenceYear, e.ReferenceMonth });

            entity.HasMany(e => e.Items)
                .WithOne(i => i.InventoryCheck)
                .HasForeignKey(i => i.InventoryCheckId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CONTROLLED INVENTORY ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ControlledInventoryItem>(entity =>
        {
            entity.ToTable("controlled_inventory_items");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.InventoryCheckId);
            entity.HasIndex(e => e.RawMaterialId);
        });

        // ──────────────────────────────────────────────────────────────────
        // SUPPLIER CERTIFICATION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SupplierCertification>(entity =>
        {
            entity.ToTable("supplier_certifications");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.CertificationType);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // SUPPLIER QUALITY SCORE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SupplierQualityScore>(entity =>
        {
            entity.ToTable("supplier_quality_scores");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.SupplierId);
            entity.HasIndex(e => e.EstablishmentId);

            entity.HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // AUDITOR ACCESS REQUEST
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<AuditorAccessRequest>(entity =>
        {
            entity.ToTable("auditor_access_requests");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.AccessToken);

            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // AUDITOR ACCESS LOG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<AuditorAccessLog>(entity =>
        {
            entity.ToTable("auditor_access_logs");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.AuditorAccessId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.AuditorAccess)
                .WithMany()
                .HasForeignKey(e => e.AuditorAccessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    /// <summary>
    /// Configura entidades de Formas Farmacêuticas e Precificação
    /// </summary>
    private void ConfigurePharmaceuticalFormsEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // PHARMACEUTICAL FORM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PharmaceuticalForm>(entity =>
        {
            entity.ToTable("pharmaceutical_forms");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.EstablishmentId, e.Code })
                  .IsUnique();

            entity.HasIndex(e => new { e.EstablishmentId, e.IsActive });

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Subtypes)
                  .WithOne(s => s.PharmaceuticalForm)
                  .HasForeignKey(s => s.PharmaceuticalFormId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // PHARMACEUTICAL FORM SUBTYPE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PharmaceuticalFormSubtype>(entity =>
        {
            entity.ToTable("pharmaceutical_form_subtypes");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.EstablishmentId, e.PharmaceuticalFormId, e.Code })
                  .IsUnique();

            entity.HasIndex(e => e.PharmaceuticalFormId);
            entity.HasIndex(e => e.EstablishmentId);

            entity.HasOne(e => e.PharmaceuticalForm)
                  .WithMany(f => f.Subtypes)
                  .HasForeignKey(e => e.PharmaceuticalFormId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Compositions)
                  .WithOne(c => c.Subtype)
                  .HasForeignKey(c => c.SubtypeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // PHARMACEUTICAL FORM COMPOSITION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<PharmaceuticalFormComposition>(entity =>
        {
            entity.ToTable("pharmaceutical_form_compositions");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.SubtypeId, e.RawMaterialId })
                  .IsUnique();

            entity.HasIndex(e => e.SubtypeId);

            entity.HasOne(e => e.Subtype)
                  .WithMany(s => s.Compositions)
                  .HasForeignKey(e => e.SubtypeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RawMaterial)
                  .WithMany()
                  .HasForeignKey(e => e.RawMaterialId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // CAPSULE SIZE REFERENCE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CapsuleSizeReference>(entity =>
        {
            entity.ToTable("capsule_size_reference");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.EstablishmentId, e.SizeCode })
                  .IsUnique();

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // ESTABLISHMENT PRICING CONFIG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<EstablishmentPricingConfig>(entity =>
        {
            entity.ToTable("establishment_pricing_config");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                  .IsUnique();

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UpdatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ──────────────────────────────────────────────────────────────────
        // COUPON (Cupons de Desconto)
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("Coupons");

            entity.HasKey(e => e.Id);

            // Código único global
            entity.HasIndex(e => e.Code)
                  .IsUnique();

            // Índice por estabelecimento
            entity.HasIndex(e => e.EstablishmentId);

            // Índice para busca de cupons ativos
            entity.HasIndex(e => new { e.IsActive, e.ValidFrom, e.ValidUntil });

            entity.Property(e => e.Code)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(200);

            entity.Property(e => e.DiscountType)
                  .HasMaxLength(20)
                  .HasDefaultValue("PERCENTAGE");

            entity.Property(e => e.DiscountPercentage)
                  .HasPrecision(5, 2);

            entity.Property(e => e.DiscountValue)
                  .HasPrecision(10, 2);

            entity.Property(e => e.MinOrderValue)
                  .HasPrecision(10, 2);

            entity.Property(e => e.MaxDiscountValue)
                  .HasPrecision(10, 2);

            entity.Property(e => e.UsedCount)
                  .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relacionamentos
            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Usages)
                  .WithOne(u => u.Coupon)
                  .HasForeignKey(u => u.CouponId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // COUPON USAGE (Histórico de Uso de Cupons)
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CouponUsage>(entity =>
        {
            entity.ToTable("CouponUsages");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CouponId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.UsedAt);

            // Índice composto para verificar uso por cliente
            entity.HasIndex(e => new { e.CouponId, e.CustomerId });

            entity.Property(e => e.DiscountApplied)
                  .HasPrecision(10, 2);

            entity.Property(e => e.UsedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relacionamentos
            entity.HasOne(e => e.Coupon)
                  .WithMany(c => c.Usages)
                  .HasForeignKey(e => e.CouponId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureMarketplaceEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // PLATFORM COMMISSION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.PlatformCommission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EstablishmentId, e.WeekStartDate });
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // PLATFORM TRANSACTION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.PlatformTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrderId, e.Status });
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.StripePaymentIntentId);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ──────────────────────────────────────────────────────────────────
        // PHARMACY RATING
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.PharmacyRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.CustomerId);

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ──────────────────────────────────────────────────────────────────
        // PRODUCT RATING
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.ProductRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CatalogProductId);
            entity.HasIndex(e => e.CustomerId);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.CatalogProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ──────────────────────────────────────────────────────────────────
        // DELIVERY ESTIMATE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.DeliveryEstimate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER DEVICE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.CustomerDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.DeviceToken);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER ADDRESS
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // PHARMACY PAYOUT ACCOUNT
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.PharmacyPayoutAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EstablishmentId).IsUnique();
            entity.HasIndex(e => e.StripeConnectAccountId);

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ──────────────────────────────────────────────────────────────────
        // SEARCH HISTORY
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<Models.Marketplace.SearchHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SearchTerm);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PendingTwoFactorSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TempToken).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<RevokedJwt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JwtId).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        // ──────────────────────────────────────────────────────────────────
        // SUPORTE & MONITORAMENTO
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.DeduplicationKey);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Messages)
                  .WithOne(m => m.Ticket)
                  .HasForeignKey(m => m.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupportTicketMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TicketId);
        });

        modelBuilder.Entity<WhatsAppInstanceStatus>(entity =>
        {
            entity.HasKey(e => e.InstanceName);
        });
    }
}