using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Data;
using Middleware;
using Models.Pharmacy;
using Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using Service.Purchasing;
using Service.Auth;
using Service.Notifications;
using Service;
using Service.Formulas;
using Service.Prescriptions;
using Validators.Formulas;
using Service.BatchQuality;
using Microsoft.AspNetCore.Authentication.Cookies;
using Extensions;
using Isopoh.Cryptography.Argon2;
using Filters; 
using Service.CustomerFormulas;
using Service.PharmaceuticalForms;
using Service.Marketplace;
using Service.Catalog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;



using System.Threading.RateLimiting;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// Forwarded headers — necessário para rate limiting e HTTPS funcionar atrás de proxy reverso
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Aceitar apenas proxies confiáveis (Docker internal network 172.16.0.0/12)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
        System.Net.IPAddress.Parse("172.16.0.0"), 12));
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
        System.Net.IPAddress.Parse("10.0.0.0"), 8));
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
        System.Net.IPAddress.Parse("192.168.0.0"), 16));
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Rate limit por IP para endpoints sensíveis
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.AddPolicy("signup", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.AddPolicy("resend-code", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.AddPolicy("password-reset", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(30),
                QueueLimit = 0
            }));

    // OCR: 3 uploads por minuto por IP — chamadas externas à OpenAI são caras
    options.AddPolicy("ocr-upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Kestrel — limites globais para mitigar flood e body bombing
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(120);
});

// IMemoryCache — usado pelo BruteForceMiddleware para rastrear falhas por IP
builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddScoped<CurrentEmployeeFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<CurrentEmployeeFilter>(); // ← ADICIONADO
});

builder.Services.AddControllers();
builder.Services.AddScoped<FormulaService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<PrescriptionService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<SngpcService>();
builder.Services.AddScoped<LabelService>();
builder.Services.AddManipulationServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<QuoteEmailService>();
builder.Services.AddScoped<PrescriptionWorkflowService>();
builder.Services.AddScoped<CashRegisterService>();
builder.Services.AddScoped<CustomerAuthService>();
// Program.cs
builder.Services.AddScoped<CustomFormulaService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<PharmaceuticalAnalysisService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<CapsuleCalculationService>();

builder.Services.AddScoped<AuditService>();
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddScoped<StripePaymentService>();
builder.Services.AddScoped<MercadoPagoPaymentService>();
builder.Services.AddScoped<AbacatepayPaymentService>();
builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

builder.Services.Configure<Configuration.EmailSettings>(
builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<Service.IEmailService, Service.EmailService>();

// Background Jobs
builder.Services.AddHostedService<Services.Jobs.TrialExpirationJob>();
builder.Services.AddHostedService<Services.Jobs.SubscriptionMaintenanceJob>();

// ADICIONAR DbContext para PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );

        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.MigrationsAssembly("salvelle");
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }

    options.ConfigureWarnings(w =>
        w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// HttpClient — timeout obrigatório pra evitar thread pool starvation se AtentBot pendurar
builder.Services.AddHttpClient<WhatsAppService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
});

// Services
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BatchQualityService>();
// AddManipulationServices() ja registrado na linha 94
builder.Services.AddScoped<Service.Prescriptions.PrescriptionQuoteService>();
// HttpClient com timeout — evita thread pool starvation se OpenAI ou AtentBot pendurar
builder.Services.AddHttpClient<Service.Prescriptions.OpenAIPrescriptionParserService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<Service.Prescriptions.QuoteWhatsAppService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<Service.Prescriptions.PrescriptionWorkflowService>();
builder.Services.AddScoped<StripeService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<SignupService>();
builder.Services.AddScoped<Service.IngredientMatcherService>();
builder.Services.AddScoped<CatalogSeederService>();

// Marketplace Services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<CommissionService>();
builder.Services.AddScoped<Service.Marketplace.OrderNotificationService>();

// Marketplace Background Jobs
builder.Services.AddHostedService<Services.Jobs.WeeklyCommissionJob>();
builder.Services.AddHostedService<Services.Jobs.AbandonedCartCleanupJob>();

// Support Module
builder.Services.AddScoped<Service.Support.SupportTicketService>();
builder.Services.AddHostedService<Services.Jobs.WhatsAppMonitorJob>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Authentication (Cookie + JWT)
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer("MobileJwt", options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == "__SET_VIA_ENV_VAR__")
            throw new InvalidOperationException(
                "JWT:Key não configurado. Defina a variável de ambiente JWT__KEY com uma chave forte (o placeholder do appsettings não é aceito).");
        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException(
                "JWT:Key fraca: use ao menos 32 bytes (256 bits) para HMAC-SHA256.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS - restrito a dominios autorizados
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://app.salvelle.com", "https://www.salvelle.com" };

        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "FormulaClear.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Salvelle API",
        Description = "Documentação da API do projeto Salvelle",
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "Cole aqui sua X-API-KEY"
    });

    c.AddSecurityDefinition("SessionToken", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-SESSION-TOKEN",
        Description = "Token de sessão retornado no login"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "SessionToken" }
            },
            Array.Empty<string>()
        }
    });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;
    });

    // Resolve conflitos de SchemaId para classes com mesmo nome em namespaces diferentes
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

var app = builder.Build();

// Criar/atualizar banco de dados
try
{
    using (var migrationScope = app.Services.CreateScope())
    {
        var migrationDb = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (app.Environment.IsDevelopment())
        {
            // Em dev, EnsureCreated cria todas as tabelas do modelo atual
            // (necessário porque o Baseline migration é NO-OP)
            await migrationDb.Database.EnsureCreatedAsync();
            Console.WriteLine("✅ Banco de dados criado/verificado com sucesso");
        }
        else
        {
            await migrationDb.Database.MigrateAsync();
            Console.WriteLine("✅ Migrations aplicadas com sucesso");
        }
    }
}
catch (Exception ex)
{
    // Não derruba a aplicação (o schema já pode existir), mas registra o erro
    // COMPLETO para que drift de migrations não passe despercebido em produção.
    Console.WriteLine($"⚠️ Erro ao inicializar banco: {ex.Message}");
    Console.WriteLine(ex.ToString());
}

// Seed data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Database.CanConnectAsync())
        {
            // ═══════════════════════════════════════════════════════════════════════
            // SEED ACCESS LEVEL (para Establishments)
            // ═══════════════════════════════════════════════════════════════════════
            if (!await db.AccessLevels.AnyAsync())
            {
                db.AccessLevels.Add(new AccessLevel
                {
                    Id = Guid.NewGuid(),
                    Code = "FARM",
                    Name = "Farmácia",
                    Description = "Nível de acesso para farmácias de manipulação",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                Console.WriteLine("✅ AccessLevel 'FARM' criado");
            }

            // ═══════════════════════════════════════════════════════════════════════
            // SEED SAAS ADMIN (Primeiro Administrador)
            // ═══════════════════════════════════════════════════════════════════════
            if (!await db.SaasAdmins.AnyAsync())
            {
                var senhaAdmin = builder.Configuration["SeedAdmin:Password"];
                if (string.IsNullOrWhiteSpace(senhaAdmin))
                {
                    Console.WriteLine("⚠️  ATENÇÃO: SeedAdmin:Password não configurado. Use uma senha forte em produção!");
                    senhaAdmin = app.Environment.IsDevelopment()
                        ? "Salvelle@Dev2024"
                        : throw new InvalidOperationException(
                            "SeedAdmin:Password não configurado. Defina via env var SEEDADMIN__PASSWORD antes de iniciar em produção.");
                }

                var hashSenha = Argon2.Hash(senhaAdmin);

                db.SaasAdmins.Add(new SaasAdmin
                {
                    Id = Guid.NewGuid(),
                    FullName = "Douglas - Administrador",
                    Email = builder.Configuration["SeedAdmin:Email"] ?? "admin@salvelle.com",
                    PasswordHash = hashSenha,
                    PasswordAlgorithm = "argon2id-v1",
                    Role = "SUPER_ADMIN",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();

                Console.WriteLine("SAAS ADMIN criado com sucesso. Troque a senha apos o primeiro login em /admin/login");
            }

            // ═══════════════════════════════════════════════════════════════════════
            // SEED CATÁLOGO GLOBAL DE MATÉRIAS-PRIMAS
            // ═══════════════════════════════════════════════════════════════════════
            try
            {
                var catalogSeeder = scope.ServiceProvider.GetRequiredService<CatalogSeederService>();
                await catalogSeeder.SeedAsync();
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"⚠️ Erro ao seedar catálogo de matérias-primas: {seedEx.Message}");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Banco de dados não disponível. Continuando sem seed data.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erro ao criar seed data: {ex.Message}");
}

// Configure pipeline
app.UseForwardedHeaders(); // Deve ser o primeiro — atualiza RemoteIpAddress antes do rate limiting
app.UseMiddleware<BruteForceMiddleware>(); // Bloqueia IPs com excesso de falhas de autenticação

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();

// Health Check
app.MapHealthChecks("/health");

// Swagger - apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Salvelle API v1");
        options.RoutePrefix = "swagger";
    });
}

// Headers de seguranca
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdnjs.cloudflare.com fonts.googleapis.com; " +
            "font-src 'self' fonts.gstatic.com cdn.jsdelivr.net cdnjs.cloudflare.com; " +
            "img-src 'self' data: blob:; connect-src 'self' api.stripe.com;";
    }
    await next();
});

app.UseCors();
app.UseMiddleware<RateLimitMiddleware>(); // Rate limit para APIs mobile (sliding window)
app.UseRouting(); // Routing deve ser antes do UseRateLimiter para resolver endpoint metadata
app.UseRateLimiter(); // Usa [EnableRateLimiting] attributes — precisa do endpoint resolvido
app.UseSession();

app.UseAuthentication();
app.UseMiddleware<JwtAuthMiddleware>(); // JWT para rotas /api/mobile/
app.UseAdminAuth();
app.UseEmployeeAuth();
app.UseCustomerAuth();
app.UseSubscriptionLimits();
app.UseSubscriptionRequired();  // ← NOVO: Verifica se tem subscription válida
app.UseAuthorization();

app.MapStaticAssets();
// MapControllers() habilita endpoint routing para ApiControllers — necessário para [EnableRateLimiting]
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "manipulacoes",
    pattern: "Manipulacoes/{action=Index}/{id?}",
    defaults: new { controller = "Manipulacoes" });

app.Run();
