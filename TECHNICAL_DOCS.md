# Salvelle — Documentação Técnica Completa

> Gerado em 2026-07-04. Atualizar a cada sprint ou mudança arquitetural relevante.

---

## Índice

1. [Visão Geral do Produto](#1-visão-geral-do-produto)
2. [Stack Tecnológica](#2-stack-tecnológica)
3. [Arquitetura do Sistema](#3-arquitetura-do-sistema)
4. [Estrutura de Diretórios](#4-estrutura-de-diretórios)
5. [Banco de Dados e Entidades](#5-banco-de-dados-e-entidades)
6. [Autenticação e Autorização](#6-autenticação-e-autorização)
7. [Multi-Tenancy](#7-multi-tenancy)
8. [API Surface](#8-api-surface)
9. [Serviços](#9-serviços)
10. [Jobs em Background](#10-jobs-em-background)
11. [Integrações Externas](#11-integrações-externas)
12. [Aplicativo Mobile (Salvelle)](#12-aplicativo-mobile-salvelle)
13. [Segurança](#13-segurança)
14. [Deploy e CI/CD](#14-deploy-e-cicd)
15. [Configuração de Ambiente](#15-configuração-de-ambiente)

---

## 1. Visão Geral do Produto

**Salvelle** é um sistema SaaS para farmácias de manipulação, com modelo de marketplace (similar ao iFood) onde clientes encontram farmácias, orçam fórmulas e fazem pedidos online.

### Camadas do produto

| Camada | URL | Usuários |
|--------|-----|---------|
| Plataforma SaaS Admin | `/admin/*` | Douglas (dono da plataforma) |
| Sistema de gestão da farmácia | `/` (raiz) | Funcionários e farmacêuticos |
| Portal do cliente (web) | `/cliente/*` | Clientes que foram a uma farmácia |
| API Mobile (marketplace) | `/api/mobile/v1/*` | Clientes do app Salvelle |
| App Mobile Salvelle | React Native (Expo) | Clientes do marketplace |

### Modelo de negócio

- Farmácias pagam assinatura SaaS mensal (Básico / Profissional / Enterprise)
- Plataforma cobra comissão escalonada nos pedidos do marketplace: 7% (pequenas) → 5% (médias) → 3% (grandes)
- Comissões são calculadas semanalmente e cobradas das farmácias

---

## 2. Stack Tecnológica

### Backend

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| .NET | 9.0 | Runtime principal |
| ASP.NET Core MVC | 9.0 | Views MVC + API Controllers no mesmo processo |
| Entity Framework Core | 9.0.10 | ORM |
| Npgsql (EF driver) | 9.* | Driver PostgreSQL |
| PostgreSQL | 14+ | Banco de dados principal |
| FluentValidation | 12.1 | Validação de requests |
| Argon2id (`Isopoh.Cryptography.Argon2`) | 2.0.0 | Hash de senhas |
| BCrypt.Net-Next | 4.0.3 | Hash legado (funcionários) |
| QuestPDF | 2025.12.1 | Geração de PDFs (etiquetas, fichas) |
| QRCoder | 1.5.1 | QR codes de estabelecimento |
| Stripe.net | 49.1.0 | Assinaturas SaaS + payouts marketplace |
| Swashbuckle (Swagger) | 9.0.6 | Documentação OpenAPI (dev only) |
| System.IO.Ports | 10.0.1 | Balanças e impressoras (serial) |

### Frontend (web)
- Razor Views / Bootstrap 5
- jQuery + Bootstrap JS (bundled)
- JavaScript vanilla para funcionalidades específicas

### Mobile (Salvelle App)

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| React Native | 0.76 | Framework mobile |
| Expo SDK | 52 | Build e runtime |
| React Navigation | 6 | Navegação entre telas |
| AsyncStorage | - | Persistência local (tokens) |
| expo-local-authentication | - | Biometria |
| expo-camera | - | Câmera para receitas |

---

## 3. Arquitetura do Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│                         Internet                                │
└────────┬─────────────────────────────────────────┬─────────────┘
         │                                         │
    ┌────▼────┐                              ┌─────▼──────┐
    │ Salvelle│                              │ Browser    │
    │ Mobile  │                              │ (farmácia/ │
    │  App    │                              │  cliente)  │
    └────┬────┘                              └─────┬──────┘
         │                                         │
         │    JWT Bearer                           │    Cookie/Session
         │    /api/mobile/v1/*                     │    /*  /cliente/*  /admin/*
         │                                         │
         └───────────────────┬─────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  ASP.NET Core   │
                    │  (único processo│
                    │  MVC + API)     │
                    │                 │
                    │  Middleware     │
                    │  pipeline:      │
                    │  1. HTTPS       │
                    │  2. CORS        │
                    │  3. RateLimiter │
                    │  4. Routing     │
                    │  5. Session     │
                    │  6. AuthN       │
                    │  7. JwtAuth     │
                    │  8. AdminAuth   │
                    │  9. EmployeeAuth│
                    │ 10. CustomerAuth│
                    │ 11. SubRequired │
                    │ 12. Authorization│
                    └────────┬────────┘
                             │
               ┌─────────────┴──────────────┐
               │                            │
        ┌──────▼──────┐             ┌───────▼──────┐
        │  Services   │             │  PostgreSQL  │
        │  (Scoped)   │◄────────────│  (via EF     │
        │             │             │  Core/Npgsql)│
        └──────┬──────┘             └──────────────┘
               │
    ┌──────────┴───────────────────────────┐
    │        Integrações Externas          │
    │  Stripe │ OpenAI │ AtentBot │ SEFAZ │
    └──────────────────────────────────────┘
```

### Pipeline de Middleware (ordem crítica)

```
UseHttpsRedirection
UseCors
UseRateLimiter              ← ASP.NET Core built-in (políticas: auth, signup, etc.)
UseMiddleware<RateLimitMiddleware>  ← Custom (mobile APIs por IP)
UseRouting
UseSession
UseAuthentication           ← Cookie scheme padrão
UseMiddleware<JwtAuthMiddleware>    ← JWT para /api/mobile/
UseAdminAuth                ← AdminAuthMiddleware para /admin/ e /api/admin/
UseEmployeeAuth             ← EmployeeAuthMiddleware (rotas de farmácia)
UseCustomerAuth             ← CustomerAuthMiddleware para /cliente/
UseSubscriptionLimits       ← Verifica limites do plano
UseSubscriptionRequired     ← Bloqueia se PAST_DUE > 7 dias
UseAuthorization
```

---

## 4. Estrutura de Diretórios

```
/mnt/e/salvelle/salvelle/
├── salvelle/                          # Projeto principal ASP.NET Core
│   ├── Controllers/
│   │   ├── Mobile/                    # API mobile (JWT)
│   │   │   ├── MobileAuthController.cs
│   │   │   ├── MobileCartController.cs
│   │   │   ├── MobileOrdersController.cs
│   │   │   ├── MobilePharmaciesController.cs
│   │   │   ├── MobileAddressesController.cs
│   │   │   ├── MobileRatingsController.cs
│   │   │   └── MobileSearchController.cs
│   │   ├── Pharmacy/                  # Portal da farmácia (marketplace)
│   │   │   ├── PharmacyCatalogController.cs
│   │   │   ├── PharmacyOrdersController.cs
│   │   │   ├── PharmacyFinancialController.cs
│   │   │   └── PharmacyMarketplaceSettingsController.cs
│   │   ├── Admin*/                    # Painel SaaS admin
│   │   ├── Cliente*/                  # Portal do cliente (web)
│   │   └── [outros ~70 controllers]   # Gestão interna da farmácia
│   ├── Data/
│   │   ├── AppDbContext.cs            # DbContext (~90 DbSets)
│   │   └── Seeds/                     # Seeds de dados iniciais
│   ├── Middleware/
│   │   ├── AdminAuthMiddleware.cs
│   │   ├── CustomerAuthMiddleware.cs
│   │   ├── EmployeeAuthMiddleware.cs
│   │   ├── JwtAuthMiddleware.cs
│   │   ├── RateLimitMiddleware.cs
│   │   └── SubscriptionLimitMiddleware.cs
│   ├── Models/                        # Entidades EF Core
│   │   ├── Marketplace/               # Modelos do marketplace
│   │   ├── Pharmacy/                  # Modelos da farmácia
│   │   ├── Employees/                 # RH e funcionários
│   │   ├── Auth/                      # Autenticação
│   │   ├── Security/                  # Audit, Permissions
│   │   ├── Cart/, Billing/, etc.
│   │   └── [80+ arquivos de modelo]
│   ├── Service/                       # Camada de serviço
│   │   ├── Marketplace/               # JwtTokenService, CommissionService
│   │   ├── Auth/                      # AuthService (employee login/2FA)
│   │   ├── Jobs/                      # Background jobs
│   │   ├── Prescriptions/             # Workflow de receitas
│   │   ├── CustomerFormulas/          # Fórmulas personalizadas
│   │   └── [50+ arquivos de serviço]
│   ├── Migrations/                    # EF Core migrations
│   ├── Extensions/                    # ServiceCollection extensions
│   ├── Filters/                       # Action filters (CurrentEmployeeFilter)
│   ├── DTOs/                          # Data Transfer Objects
│   ├── Validators/                    # FluentValidation validators
│   ├── Helpers/                       # Utilitários
│   ├── wwwroot/                       # Arquivos estáticos
│   ├── Views/                         # Razor Views
│   ├── Program.cs                     # Entry point / DI / pipeline
│   ├── appsettings.json               # Config template (sem secrets)
│   ├── appsettings.Development.json   # Config dev local (no .gitignore)
│   └── Dockerfile                     # Imagem Docker
├── salvelle.Tests/                    # Projeto de testes
│   ├── CommissionServiceTests.cs
│   ├── JwtTokenServiceTests.cs
│   ├── MobileCartControllerTests.cs
│   ├── MobileOrdersControllerTests.cs
│   ├── OrderNotificationServiceTests.cs
│   ├── OrderStatusTransitionTests.cs
│   ├── OrderWorkflowTests.cs
│   └── PharmacyOrdersControllerTests.cs
├── app/                               # App mobile Salvelle (React Native/Expo)
│   ├── src/
│   │   ├── screens/                   # Telas do app
│   │   ├── navigation/                # AppNavigator
│   │   ├── services/api.js            # Cliente HTTP
│   │   ├── hooks/                     # useAuth, useBiometrics
│   │   └── constants/                 # config.js, theme.js
│   └── App.js
└── .github/workflows/deploy.yml      # CI/CD GitHub Actions
```

---

## 5. Banco de Dados e Entidades

**Banco:** PostgreSQL. Convenção de nomes: snake_case (via `EFCore.NamingConventions`).

### Domínios e entidades principais

#### 5.1 Core (multi-tenancy)
- **`Establishment`** — raiz do tenant. Campos chave: `IsActive`, `IsMarketplaceActive`, `AcceptingOrders`, `MaxEmployeesLimit`, `MaxOrdersLimit`, `AverageRating`, `MinOrderAmount`, `AverageDeliveryMinutes`.
- **`AccessLevel`** — níveis de acesso (FARM, etc.).

#### 5.2 SaaS Billing
- **`SubscriptionPlan`** — catálogo de planos.
- **`Subscription`** → `Establishment`. Status: `TRIALING | ACTIVE | PAST_DUE | CANCELLED`. FK Stripe: `StripeSubscriptionId`, `StripeCustomerId`.
- **`SubscriptionInvoice`** — índice único em `StripeInvoiceId`.
- **`PaymentGatewayConfig`** — tipo, ambiente (sandbox/prod), isDefault por establishment.
- **`SaasAdmin`** / **`SaasAdminSession`** / **`SaasAdminPasswordReset`** — admins da plataforma.

#### 5.3 Funcionários e RH
- **`Employee`** → `Establishment`. Status: `ATIVO | DEMITIDO`.
- **`EmployeeSession`** — tokens de sessão (único por token).
- **`JobPosition`** (códigos: `OWNER | MANAGER | PHARMACIST_RT | PHARMACIST | ATTENDANT`).
- **`EmployeeJobHistory`**, **`EmployeeBenefit`**, **`EmployeeDocument`**.

#### 5.4 Segurança e Audit
- **`Permission`** — RBAC seeds: `inventory.read/write`, `sales.create/read`, `reports.read/export`, `settings.update` (requer 2FA).
- **`AuditLog`** — log imutável de ações.
- **`LoginAttempt`**, **`PasswordResetToken`**, **`TwoFactorToken`**.

#### 5.5 Estoque e Matérias-Primas
- **`RawMaterial`** — por farmácia. **`RawMaterialCatalog`** — catálogo global.
- **`Batch`**, **`StockMovement`**, **`ControlledSubstanceMovement`**, **`ControlledSubstanceBalance`**.

#### 5.6 Fórmulas e Manipulação
- **`Formula`** → **`FormulaComponent`** (matérias + quantidades).
- **`ManipulationOrder`** → **`ManipulationStep`** (JSONB `StepData`) → **`ManipulationPhoto`**.
- **`DualVerification`** — dois farmacêuticos (FKs Restrict).
- **`ProductionRecord`** — registro de produção.

#### 5.7 Clientes e Receitas
- **`Customer`** — consumidor final (sem `EstablishmentId` = global marketplace).
- **`CustomerAuth`** — phone + CPF únicos. Hash Argon2id.
- **`CustomerSession`** — token único.
- **`Prescription`** → **`PrescriptionFile`**, **`SpecialPrescriptionControl`**.

#### 5.8 E-commerce / Marketplace
- **`CatalogProduct`** — por farmácia (`IsMarketplaceVisible`, `StockQuantity`).
- **`CustomerCart`** → **`CustomerCartItem`** — índice único `(CustomerId, EstablishmentId)`.
- **`OnlineOrder`** → **`OnlineOrderItem`** — campos: `PlatformCommissionRate`, `PlatformCommissionAmount`, `NetAmountToPharmacy`.
- **`PlatformTransaction`** — uma por pedido pago.
- **`PlatformCommission`** — comissão semanal por farmácia (status: `CALCULADO → COBRADO → PAGO`).
- **`PharmacyRating`**, **`ProductRating`**, **`DeliveryEstimate`**.
- **`CustomerAddress`**, **`CustomerDevice`** (push tokens).
- **`PharmacyPayoutAccount`** — Stripe Connect ID.

#### 5.9 Fiscal
- **`FiscalInvoice`** → **`FiscalInvoiceItem`**. Índice único `(EstablishmentId, InvoiceNumber, Series)`.
- **`FiscalConfig`** — certificado SEFAZ, CSC, série. Índice único por `EstablishmentId`.
- **`FiscalQueue`** (contingência, max 3 retries), **`FiscalLog`**.

### Índices críticos
```sql
-- CustomerAuth
UNIQUE (phone)
UNIQUE (cpf)

-- CustomerSession  
UNIQUE (session_token)

-- OnlineOrder
UNIQUE (order_number)

-- SubscriptionInvoice
UNIQUE (stripe_invoice_id)

-- CustomerCart
UNIQUE (customer_id, establishment_id) WHERE status = 'ACTIVE'
```

---

## 6. Autenticação e Autorização

O sistema possui **3 mecanismos de autenticação paralelos** no mesmo pipeline:

### 6.1 JWT Bearer — Mobile API
- Middleware: `JwtAuthMiddleware` (aplicado em `/api/mobile/*`)
- Algoritmo: HMAC-SHA256
- Claims: `sub`, `email`, `jti`, `customer_id`, `name`
- Access token: **15 minutos** (zero clock skew)
- Refresh token: **64 bytes aleatórios** criptograficamente seguros, 30 dias, armazenado na tabela `CustomerSessions`
- Rotação de refresh token a cada uso
- Lockout: 5 tentativas falhas → 30 min de bloqueio

**Rotas mobile públicas (sem JWT):**
- `/api/mobile/v1/auth/*`
- `/api/mobile/v1/pharmacies/nearby`
- `/api/mobile/v1/pharmacies/{id}` (exceto `/orders`)
- `/api/mobile/v1/search`
- `/api/mobile/v1/categories`

### 6.2 Session-based — Funcionários da Farmácia
- Middleware: `EmployeeAuthMiddleware`
- Token: 32 bytes aleatórios armazenado em `EmployeeSessions`
- Lido de: header `X-Session-Token` ou cookie `X-SESSION-TOKEN`
- Expiração: 8h padrão, 30 dias com "lembrar de mim"
- O `EstablishmentId` é extraído da sessão → injetado em `context.Items`
- 2FA via WhatsApp obrigatório para roles: `OWNER`, `MANAGER`, `PHARMACIST_RT`, `PHARMACIST`

### 6.3 Session-based — SaaS Admin
- Middleware: `AdminAuthMiddleware`
- Cookie: `AdminSessionId`
- Tabela: `SaasAdminSessions`
- Protege `/admin/*` e `/api/admin/*`

### 6.4 Session-based — Portal do Cliente
- Middleware: `CustomerAuthMiddleware`
- Token: cookie `CustomerSessionId` ou header `Bearer`/`X-Session-Token`
- Login via telefone + OTP (sem senha)

### 6.5 Senhas

| Ator | Algoritmo |
|------|-----------|
| SaaS Admin | Argon2id |
| Funcionário | Argon2id (BCrypt para legado) |
| Cliente Mobile | Argon2id |
| Cliente Portal | OTP via SMS/WhatsApp |

---

## 7. Multi-Tenancy

**Padrão:** Banco compartilhado + discriminador por chave estrangeira (`EstablishmentId`).

**Isolamento por camada:**
1. **Middleware** — `EmployeeAuthMiddleware` garante que `EstablishmentId` em `context.Items` é o da sessão do funcionário logado
2. **Filter** — `CurrentEmployeeFilter` (global action filter) injeta contexto do funcionário em todas as actions
3. **Controller** — todo acesso a dados filtra por `EstablishmentId` obtido de `GetEstablishmentId()` / `context.Items`
4. **Subscription** — `SubscriptionLimitMiddleware` aplica limites por tenant

**Entidades globais (cross-tenant):**
- `RawMaterialCatalog` — catálogo global de matérias-primas
- `ActiveIngredient` — ingredientes ativos de referência
- `SubscriptionPlan` — planos disponíveis
- `Customer` — clientes do marketplace (sem vínculo de farmácia)

---

## 8. API Surface

### 8.1 Mobile API (JWT) — `/api/mobile/v1/`

| Método | Rota | Controller | Auth |
|--------|------|-----------|------|
| POST | `/auth/register` | MobileAuthController | Pública |
| POST | `/auth/login` | MobileAuthController | Pública |
| POST | `/auth/refresh-token` | MobileAuthController | Pública |
| GET | `/auth/profile` | MobileAuthController | JWT |
| GET | `/pharmacies/nearby` | MobilePharmaciesController | Pública |
| GET | `/pharmacies/{id}` | MobilePharmaciesController | Pública |
| GET | `/pharmacies/{id}/catalog` | MobilePharmaciesController | Pública |
| GET | `/cart` | MobileCartController | JWT |
| POST | `/cart/items` | MobileCartController | JWT |
| PUT | `/cart/items/{id}` | MobileCartController | JWT |
| DELETE | `/cart/items/{id}` | MobileCartController | JWT |
| GET | `/orders` | MobileOrdersController | JWT |
| POST | `/orders` | MobileOrdersController | JWT |
| GET | `/orders/{id}` | MobileOrdersController | JWT |
| GET | `/orders/{id}/track` | MobileOrdersController | JWT |
| GET/POST | `/addresses` | MobileAddressesController | JWT |
| POST | `/ratings` | MobileRatingsController | JWT |
| GET | `/search` | MobileSearchController | Pública |

### 8.2 Portal da Farmácia — `/api/pharmacy/marketplace/`

| Método | Rota | Controller | Auth |
|--------|------|-----------|------|
| GET | `/orders` | PharmacyOrdersController | Employee session |
| GET | `/orders/counts` | PharmacyOrdersController | Employee session |
| GET | `/orders/{id}` | PharmacyOrdersController | Employee session |
| PUT | `/orders/{id}/status` | PharmacyOrdersController | Employee session |
| GET/POST/PUT | `/catalog` | PharmacyCatalogController | Employee session |
| GET | `/financial` | PharmacyFinancialController | Employee session |
| GET/PUT | `/settings` | PharmacyMarketplaceSettingsController | Employee session |

### 8.3 Admin SaaS — `/api/admin/`

| Rota | Função |
|------|--------|
| `/admin/marketplace/dashboard` | KPIs do marketplace |
| `/admin/marketplace/pharmacies` | Listar/ativar farmácias |
| `/admin/marketplace/commissions` | Comissões semanais |
| `/admin/subscriptions/*` | Gestão de assinaturas |
| `/admin/establishments/*` | Gestão de farmácias |
| `/admin/plans/*` | Planos SaaS |
| `/admin/invoices/*` | Faturas |

### 8.4 Transições de Status de Pedido

```
PENDING → CONFIRMED (farmácia aceita)
PENDING → CANCELLED
CONFIRMED → PREPARING
CONFIRMED → CANCELLED
PREPARING → READY
PREPARING → CANCELLED
READY → DELIVERED
```

---

## 9. Serviços

### 9.1 CommissionService
Calcula comissão escalonada por farmácia:
```
R$ 0–10k/mês     → 7%
R$ 10k–50k/mês  → 5%
> R$ 50k/mês    → 3%
```
Registra `PlatformTransaction` por pedido. Agrega em `PlatformCommission` semanalmente.

### 9.2 JwtTokenService
- `GenerateAccessToken(customerId, email, name)` → JWT 15min
- `GenerateRefreshToken()` → 64 bytes aleatórios (Base64)
- `ValidateToken(token)` → `ClaimsPrincipal?`
- `GetCustomerIdFromToken(principal)` → `Guid?`

### 9.3 AuthService (Employee)
- Login com CPF/WhatsApp + senha
- 2FA via WhatsApp para roles privilegiadas
- Reset de senha com código numérico 6 dígitos (15 min TTL)
- Todas as sessões invalidadas no reset de senha

### 9.4 CustomerAuthService
- Registro via telefone + OTP (sem senha)
- Sessão por token aleatório em `CustomerSessions`

### 9.5 StripeService / StripePaymentService
- Checkout Session para novos assinantes
- Customer Portal para gestão de assinatura existente
- Webhook para `invoice.paid`, `customer.subscription.updated`, etc.

### 9.6 OpenAIPrescriptionParserService
- Recebe imagem de receita em Base64
- Envia para GPT-4o Vision API
- Retorna ingredientes extraídos para `IngredientMatcherService`

### 9.7 WeeklyCommissionJob
Executa toda segunda-feira às 03:00 UTC. Calcula `PlatformCommission` para a semana anterior.

### 9.8 AbandonedCartCleanupJob
A cada 6 horas. Marca como `ABANDONED` carrinhos sem atividade há mais de 48h.

---

## 10. Jobs em Background

| Job | Tipo | Frequência | Função |
|-----|------|------------|--------|
| `WeeklyCommissionJob` | BackgroundService | Seg 03:00 UTC | Comissões semanais |
| `AbandonedCartCleanupJob` | BackgroundService | 6h | Limpar carrinhos abandonados |
| `TrialExpirationJob` | BackgroundService | Polling | Expirar trials |
| `SubscriptionMaintenanceJob` | BackgroundService | Polling | Sync status de assinaturas |

---

## 11. Integrações Externas

| Integração | Mecanismo | Propósito |
|------------|-----------|-----------|
| **Stripe** | `Stripe.net` v49 | SaaS billing + Stripe Connect para payouts |
| **MercadoPago** | HTTP client | Gateway de pagamento alternativo |
| **AbacatePay** | HTTP client | Terceiro gateway de pagamento |
| **OpenAI GPT-4o** | HTTP client | OCR de receitas médicas (vision) |
| **AtentBot/WhatsApp** | HTTP client (15s timeout) | OTP, notificações, reset de senha |
| **SMTP (KingHost)** | System.Net.Mail | E-mails transacionais |
| **Cloudflare Turnstile** | API Keys | CAPTCHA em formulários |
| **SEFAZ** | FiscalService | Emissão NF-e / NFC-e |
| **ANVISA (SNGPC)** | SngpcService | Controle de substâncias |
| **Balanças** | System.IO.Ports (serial) | Leitura de balança laboratorial |
| **Impressoras Térmicas** | ThermalPrinterService (serial) | Impressão de etiquetas |

---

## 12. Aplicativo Mobile (Salvelle)

### Tecnologia
- React Native 0.76 + Expo SDK 52
- Target: Android (EAS Build) + iOS (planejado)

### Estrutura de telas
```
AppNavigator
├── Auth Stack (sem login)
│   ├── LoginScreen          ← telefone + senha
│   ├── RegisterScreen       ← cadastro completo
│   ├── ForgotPasswordScreen
│   ├── VerifyCodeScreen     ← OTP
│   └── ResetPasswordScreen
└── App Tabs (com login)
    ├── HomeScreen           ← farmácias próximas / destaque
    ├── CatalogScreen        ← prateleira de produtos da farmácia
    ├── FormulaScreen        ← solicitar fórmula personalizada
    ├── CartScreen           ← carrinho de compras
    ├── OrdersScreen         ← histórico de pedidos
    │   └── OrderDetailScreen
    ├── PrescriptionScreen   ← upload de receita
    └── ProfileScreen        ← dados do cliente
```

### Autenticação no app
- Tokens JWT armazenados via `AsyncStorage`
- Refresh token rotacionado a cada renovação
- Biometria opcional via `expo-local-authentication`
- Auto-logout em 401

### API base
- Dev: `http://localhost:5000/api`
- Prod: `https://salvelle.atentbot.com/api`
- Toda chamada: header `Authorization: Bearer <accessToken>`

---

## 13. Segurança

### Postura atual

#### Pontos fortes
- Argon2id para hashing de todas as senhas novas
- JWT com `ClockSkew = Zero` e validação completa
- Refresh token rotation implementada
- 2FA via WhatsApp para roles de admin/farmacêutico
- Rate limiting por IP em endpoints sensíveis (login, registro, reset)
- Lockout por tentativas: 5 falhas → 30 min bloqueio
- Security headers: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, CSP em produção
- CORS restrito em produção (apenas domínios autorizados)
- Session cookies: `HttpOnly`, `SecurePolicy=Always`, `SameSite=Strict`
- Sem SQL injection: apenas LINQ/EF Core parametrizado (zero `ExecuteSqlRaw`)
- Isolamento de tenant validado nos controllers (não apenas no middleware)
- IDOR prevenido: todo acesso filtra por `customerId`/`establishmentId` da sessão

#### Vulnerabilidades corrigidas nesta sessão

| # | Severidade | Arquivo | Issue | Correção |
|---|-----------|---------|-------|---------|
| 1 | **High** | `Program.cs:209` | Chave JWT tinha fallback fraco hardcoded (`default-dev-key-...`) | Throw `InvalidOperationException` se `Jwt:Key` não configurado |
| 2 | **Medium** | `Program.cs:372` | Senha admin seed tinha fallback `"Salvelle@2024"` | Em produção: throw. Em dev: avisa no console |
| 3 | **Low** | `RateLimitMiddleware.cs` | `ConcurrentDictionary` crescia indefinidamente (memory leak com IPs únicos) | Adicionado `MaxTrackedKeys = 50_000` + limpeza de chaves vazias |

#### Vulnerabilidades conhecidas (aceitas ou pendentes)

| # | Severidade | Issue | Recomendação |
|---|-----------|-------|-------------|
| 4 | Medium | `EnableSensitiveDataLogging()` em dev expõe dados nos logs | Aceitável em dev; verificar que não há dev mode em produção |
| 5 | Medium | Mobile registration sem verificação de email/telefone (`IsVerified = true`) | Implementar OTP de confirmação no registro mobile |
| 6 | Medium | Pedido criado sem cobrança de pagamento (stock decrementado na criação) | Implementar verificação de pagamento pré-criação de pedido para cartão |
| 7 | Low | CSP tem `'unsafe-inline'` e `'unsafe-eval'` (necessário para jQuery/Bootstrap) | Migrar para nonces ou remover inline JS |
| 8 | Low | Swagger disponível em development — não expor em staging/prod | Confirmar que ASPNETCORE_ENVIRONMENT=Production em todos os ambientes de prod |

### Variáveis de ambiente obrigatórias em produção

```bash
# Banco de dados
ConnectionStrings__DefaultConnection="Host=...;Password=..."

# JWT (>= 32 chars)
Jwt__Key="<chave-aleatória-forte-mínimo-32-chars>"

# Admin seed (necessário apenas no primeiro boot)
SeedAdmin__Password="<senha-forte>"
SeedAdmin__Email="admin@seudominio.com.br"

# Criptografia AES
Encryption__Key="<base64-aes-key>"

# Stripe
Stripe__SecretKey="sk_live_..."
Stripe__WebhookSecret="whsec_..."

# OpenAI
OpenAI__ApiKey="sk-proj-..."

# WhatsApp (AtentBot)
AtentBot__ApiKey="..."

# Email SMTP
EmailSettings__SmtpUser="..."
EmailSettings__SmtpPass="..."
```

---

## 14. Deploy e CI/CD

### GitHub Actions (`.github/workflows/deploy.yml`)

```
push main
    └── test               ← dotnet test (salvelle.Tests)
        └── build-and-push ← Docker build + push para atentbot/salvelle
            └── deploy     ← Portainer webhook redeploy
```

**Secrets necessários no GitHub:**
- `DOCKERHUB_USERNAME`
- `DOCKERHUB_TOKEN`
- `PORTAINER_WEBHOOK_URL`

**Docker:**
- Imagem: `atentbot/salvelle:latest` + `atentbot/salvelle:sha-<commit>`
- Build context: `./salvelle`
- Cache via GitHub Actions cache

**Observação:** Os secrets da aplicação (JWT, Stripe, etc.) são configurados no servidor via variáveis de ambiente do Portainer/Docker, não passam pelo GitHub Actions.

### Dockerfile
Localizado em `/mnt/e/salvelle/salvelle/salvelle/Dockerfile`.

### Banco de Dados em Produção
- Migrations automáticas na inicialização (`MigrateAsync()`)
- Em dev: `EnsureCreatedAsync()` (cria sem migration)
- Retry: 3 tentativas, 5s delay

---

## 15. Configuração de Ambiente

### Desenvolvimento local

1. Copie `appsettings.Example.json` → `appsettings.Development.json`
2. Configure a connection string do PostgreSQL local
3. Execute: `dotnet run --project salvelle/salvelle.csproj`
4. Acesse: `http://localhost:5000`
5. Swagger: `http://localhost:5000/swagger`

### Testes

```bash
cd salvelle.Tests
dotnet test --configuration Release
```

### App Mobile

```bash
cd app
npm install
npx expo start          # Emulador
npx expo run:android    # Build Android
```

**Para apontar para backend local:**
Edite `app/src/constants/config.js`:
```js
const DEV_API_URL = 'http://192.168.x.x:5000/api'; // IP da sua máquina
```

---

*Documentação gerada por análise automatizada do código-fonte em 2026-07-04.*
