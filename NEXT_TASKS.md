# Próximas Tarefas — Salvelle

> Última atualização: 2026-07-04. Ordenadas por prioridade.

---

## 🔴 CRÍTICO — Segurança

> ✅ **Todos os itens de segurança foram resolvidos.** Ver seção de concluídos abaixo.

---

## ~~🔴 CRÍTICO~~ (resolvidos — ver seção concluídos)

### ~~SEC-1: Corrigir bypass de 2FA via X-Temp-Identifier~~
**Arquivo:** `Controllers/AuthController.cs:105`  
**Problema:** O endpoint `POST /api/auth/verify-2fa` recebe o identifier do funcionário via header `X-Temp-Identifier` controlado pelo cliente. Embora seja mitigado pela verificação do código ser por employee-ID, o design permite que um atacante tente impersonar outros funcionários trocando o header.  
**Correção:**
1. Criar tabela `PendingTwoFactorSession` com campos: `Token` (GUID opaco), `EmployeeId`, `ExpiresAt`, `IsUsed`
2. No `POST /api/auth/login`, ao detectar que requer 2FA, gravar registro e retornar `TempToken` opaco (não o identifier)
3. No `POST /api/auth/verify-2fa`, aceitar `TempToken` (não X-Temp-Identifier) e resolver o EmployeeId server-side
4. Remover completamente o header `X-Temp-Identifier`

### SEC-2: Adicionar rate limiting nos endpoints do portal do cliente
**Arquivo:** `Controllers/ClienteAuthApiController.cs`  
**Problema:** Login, register, verify, resend-code, request-reset não têm rate limiting (só proteção por `[AllowAnonymous]` e verificação manual).  
**Correção:** Adicionar atributos `[EnableRateLimiting]` nos métodos:
```csharp
[HttpPost("login"), EnableRateLimiting("auth")]
[HttpPost("register"), EnableRateLimiting("signup")]
[HttpPost("resend-code"), EnableRateLimiting("resend-code")]
[HttpPost("request-reset"), EnableRateLimiting("password-reset")]
```

### SEC-3: Corrigir rate limiting com proxy reverso (X-Forwarded-For)
**Arquivo:** `Program.cs`  
**Problema:** Rate limiting usa `RemoteIpAddress` que é o IP do proxy, não do cliente real.  
**Correção:** Adicionar suporte a Forwarded Headers:
```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12)); // Docker
});
app.UseForwardedHeaders();
```

### SEC-4: Corrigir onboarding code sem expiração e sem rate limit
**Arquivo:** `Controllers/EstablishmentsController.cs:121`  
**Problema:** O código de 6 dígitos de confirmação de onboarding não tem TTL nem rate limit — pode ser brute-forced em ~900k requests.  
**Correção:** 
1. Adicionar `ExpiresAt = DateTime.UtcNow.AddMinutes(30)` no `ClientOnboarding`
2. Verificar `co.ExpiresAt > DateTime.UtcNow` no endpoint `confirm`
3. Marcar como usado após confirmação (`co.IsUsed = true`)
4. Adicionar `[EnableRateLimiting("auth")]` ao endpoint confirm

### SEC-5: Verificação de email/telefone no cadastro mobile
**Arquivo:** `Controllers/Mobile/MobileAuthController.cs:76`  
**Problema:** `IsVerified = true` sem verificação real — qualquer email/telefone pode ser registrado.  
**Correção:**
1. Setar `IsVerified = false` no registro
2. Enviar OTP via email ou SMS
3. Criar endpoint `POST /api/mobile/v1/auth/verify-email` para confirmar
4. Bloquear pedidos de clientes não verificados

### SEC-6: Adicionar token revocation list para JWT mobile
**Arquivo:** `Middleware/JwtAuthMiddleware.cs`  
**Problema:** JWT não é invalidado no logout/reset de senha (acesso de 15 min persiste).  
**Correção:** Implementar uma tabela `RevokedJwt` (com índice em `JwtId` + `ExpiresAt`) ou Redis com TTL de 15 minutos. Verificar no middleware após validar o token.

---

## 🟠 ALTO — Funcionalidades (próximo sprint)

### FEAT-1: Integração de pagamento no checkout mobile
**Arquivo:** `Controllers/Mobile/MobileOrdersController.cs:CreateOrder`  
**Problema:** Pedidos são criados com `PaymentStatus = "PENDING"` mas nenhuma cobrança é feita via Stripe. O estoque é decrementado imediatamente sem pagamento confirmado.  
**Solução:**
1. Para pedidos com cartão: criar `Stripe.PaymentIntent` antes de criar o pedido
2. Só criar o pedido e decrementar estoque após `PaymentIntent.status == "succeeded"`
3. Para PIX/dinheiro: manter fluxo atual mas documentar claramente
4. Implementar webhook Stripe para confirmar pagamento assíncrono

### FEAT-2: Notificações push para app mobile
**Tabela:** `CustomerDevice` (já existe)  
**O que falta:**
- Integração com Firebase FCM ou Expo Push Notifications
- Enviar push quando pedido é confirmado/pronto/entregue
- `OrderNotificationService` existe mas sem implementação de push

### FEAT-3: Módulo de avaliações pós-pedido
**Modelos:** `PharmacyRating`, `ProductRating` (existem)  
**O que falta:**
- Calcular e atualizar `Establishment.AverageRating` após cada nova avaliação
- Endpoint para resposta da farmácia às avaliações
- Interface no dashboard da farmácia

### FEAT-4: Stripe Connect para pagamento direto às farmácias
**Modelo:** `PharmacyPayoutAccount.StripeConnectAccountId` (existe)  
**O que falta:**
- Onboarding de farmácias no Stripe Connect
- Roteamento de pagamentos: cliente paga plataforma, plataforma repassa farmácia - comissão
- Dashboard de repasses no portal da farmácia (`PharmacyFinancialController`)
- Jobs de liquidação semanal

### FEAT-5: Busca com geolocalização (farmácias próximas)
**Arquivo:** `Controllers/Mobile/MobilePharmaciesController.cs`  
**O que falta:**
- Implementar busca real por coordenadas (PostGIS ou fórmula Haversine via SQL)
- A field `DeliveryRadiusKm` existe no `Establishment`
- Ordenar resultados por distância

### FEAT-6: Voucher/cupom de desconto no checkout marketplace
**Modelo:** `Coupon` (existe)  
**O que falta:**
- Aplicar cupom no checkout do app mobile
- Escopo do cupom por farmácia (corrigir MED-9 da auditoria de segurança)
- UI no CartScreen

### FEAT-7: Cálculo de taxa de entrega
**Arquivo:** `Controllers/Mobile/MobileCartController.cs:67`  
**TODO:** `DeliveryFee = 0 // TODO: calcular taxa de entrega`  
Implementar cálculo baseado em distância km × taxa por km configurada na farmácia.

---

## 🟡 MÉDIO — Melhorias técnicas

### TECH-1: Migrar EmployeesController.Login para AuthService
**Problema:** Existe duplicação de lógica de autenticação entre `EmployeesController.Login` (legado) e `AuthController.Login` (novo). O legado usa Argon2 diretamente enquanto o novo usa `AuthService`.  
**Ação:** Deprecar endpoint legado e migrar clientes para o novo.

### TECH-2: Eliminar N+1 query no AdminMarketplaceController
**Arquivo:** `Controllers/AdminMarketplaceController.cs:148`  
```csharp
ProductCount = _db.CatalogProducts.Count(p => p.EstablishmentId == e.Id && p.IsActive)
```
Isso gera uma query por farmácia. Substituir por `GroupBy` join.

### TECH-3: Adicionar validação de MIME por magic bytes no upload de receitas
**Arquivo:** `Controllers/ClientePrescriptionApiController.cs`  
Validar primeiros bytes do base64 decodificado para confirmar que é JPEG/PNG, não arquivo malicioso.

### TECH-4: Implementar migrations em todos os ambientes
**Arquivo:** `Program.cs:322`  
Remover `EnsureCreatedAsync()` e usar sempre `MigrateAsync()`, inclusive em development, para garantir paridade de schema.

### TECH-5: Adicionar testes de integração para fluxo de pedido
**Diretório:** `salvelle.Tests/`  
Já existem testes unitários para `CommissionService`, `JwtTokenService`, `MobileCartController`, etc. Faltam testes de integração end-to-end para:
- Fluxo completo: registro → login → carrinho → pedido → confirmação
- Webhook Stripe de pagamento

### TECH-6: Adicionar health checks detalhados
**Arquivo:** `Program.cs:246`  
`AddHealthChecks()` básico sem verificações específicas. Adicionar:
- PostgreSQL: `AddNpgSql(connectionString)`
- Stripe API: ping customizado
- AtentBot: ping customizado

### TECH-7: Adicionar SeedAdmin:Email e SeedAdmin:Password ao docker-compose/portainer
**Arquivo:** `Program.cs:379` + `docker-compose.yml`  
As variáveis `SeedAdmin__Email` e `SeedAdmin__Password` agora são obrigatórias em produção. Verificar que estão configuradas no Portainer antes do próximo deploy.

---

## 🟢 BAIXO — Qualidade / UX

### UX-1: Confirmação de e-mail na tela de cadastro do app
Tela `RegisterScreen.js` não tem feedback visual de verificação de conta.

### UX-2: Deep links para pedidos
Quando farmácia aceita pedido, o push notification deveria ter deep link direto para `OrderDetailScreen`.

### UX-3: Modo offline no app
App não tem fallback para quando não há conexão. Mostrar mensagem adequada.

### UX-4: Paginação na tela de pedidos
`OrdersScreen.js` pode usar FlatList com paginação — a API já suporta `page`/`pageSize`.

### DOCS-1: Swagger em staging
Habilitar Swagger com autenticação básica em staging para facilitar desenvolvimento. Atualmente só funciona em Development.

### DOCS-2: Postman Collection
Exportar Swagger como Postman Collection e manter atualizada no repositório.

---

## 📋 Backlog de Funcionalidades Futuras

| # | Feature | Estimativa | Dependências |
|---|---------|-----------|-------------|
| B1 | Teleatendimento farmacêutico (chat ao vivo) | 3 sprints | WebSocket, regras ANVISA |
| B2 | Assinatura de fórmulas recorrentes | 2 sprints | Stripe Subscriptions per formula |
| B3 | App para farmacêuticos (gestão mobile) | 4 sprints | Novo app ou portal web responsivo |
| B4 | NFC-e para PDV | 2 sprints | FiscalService já tem base |
| B5 | Multi-idioma (espanhol para expansão LATAM) | 2 sprints | i18n infrastructure |
| B6 | API pública para integrações de terceiros | 3 sprints | OAuth 2.0, developer portal |
| B7 | Analytics de comportamento do cliente | 1 sprint | SearchHistory já existe |
| B8 | Cotação de frete por API (Correios/transportadoras) | 1 sprint | HTTP client para APIs de frete |

---

## ✅ Concluído nesta sessão — Brute-force & IDOR (2026-07-04)

| Fix | Arquivo | Severidade |
|-----|---------|-----------|
| Lockout de conta para funcionários (5 falhas → 30 min bloqueio) | `Service/Auth/AuthService.cs` | Alto |
| BruteForceMiddleware: IP tracking + bloqueio (10 falhas/15min → 60min) | `Middleware/BruteForceMiddleware.cs`, `Program.cs` | Alto |
| IDOR em Cupons: Editar/Toggle/Excluir sem filtro de estabelecimento | `Controllers/CuponsDescontoController.cs` | Médio |

## ✅ Concluído nesta sessão — Auditoria #2 (2026-07-04)

| Fix | Arquivo | Severidade |
|-----|---------|-----------|
| Rate limit em `verify-2fa` + correção de bug `identifier` undefined | `Controllers/AuthController.cs` | Crítico |
| Rate limit em mobile login | `Controllers/Mobile/MobileAuthController.cs` | Médio |
| Política `ocr-upload` (3/min) no rate limiter | `Program.cs` | Crítico |
| Limites Kestrel (MaxRequestBodySize 10MB, headers timeout, keepalive) | `Program.cs` | Alto |
| `[EnableRateLimiting("ocr-upload")]` + `[RequestSizeLimit]` no endpoint OCR | `Controllers/ClientePrescriptionApiController.cs` | Crítico |
| `OpenAIPrescriptionParserService`: `new HttpClient()` → HttpClient injetado (60s timeout) | `Service/Prescriptions/OpenAIPrescriptionParserService.cs`, `Program.cs` | Alto |
| `QuoteWhatsAppService`: `new HttpClient()` → HttpClient injetado (30s timeout) | `Service/Prescriptions/QuoteWhatsAppService.cs`, `Program.cs` | Alto |
| Race condition de estoque: UPDATE atômico em transação (TOCTOU eliminado) | `Controllers/Mobile/MobileOrdersController.cs` | Crítico |
| Validação de quantidade no carrinho (1–99) em AddItem e UpdateItem | `Controllers/Mobile/MobileCartController.cs` | Médio |
| Validação de farmácia ativa/aceitando pedidos ao trocar carrinho | `Controllers/Mobile/MobileCartController.cs` | Alto |

## ✅ Concluído nesta sessão (2026-07-04)

| Fix | Arquivo | Severidade |
|-----|---------|-----------|
| Chave JWT sem fallback fraco | `Program.cs` | High |
| Senha admin seed bloqueada em prod sem config | `Program.cs` | Medium |
| Memory leak no RateLimitMiddleware | `Middleware/RateLimitMiddleware.cs` | Low |
| Cookie `Secure = true` fixo (não depende de IsHttps) | `Controllers/EmployeesController.cs` | High |
| Cookie `SameSite=Strict` no portal do cliente | `Controllers/ClienteAuthApiController.cs` | Medium |
| Mass assignment em EstablishmentsController.Create | `Controllers/EstablishmentsController.cs` | Critical |
| PasswordHash exposto em List/Get de Establishments | `Controllers/EstablishmentsController.cs` | Critical |
| `new Random()` em NF-e chave numérica | `Service/FiscalService.cs` | Low |
| EF SQL logging silenciado em produção | `appsettings.json` | Medium |
| Rate limiting nos endpoints do portal do cliente | `Controllers/ClienteAuthApiController.cs` | High |
| ForwardedHeaders para X-Forwarded-For atrás de proxy | `Program.cs` | High |
| Código de onboarding sem expiração e sem rate limit | `Controllers/EstablishmentsController.cs`, `Models/ClientOnboarding.cs` | High |
| Bypass 2FA via X-Temp-Identifier header | `Controllers/AuthController.cs`, `Models/PendingTwoFactorSession.cs` | Critical |
| IsVerified=true imediato no registro mobile | `Controllers/Mobile/MobileAuthController.cs` | High |
| `[AllowAnonymous]` em nível de classe no carrinho | `Controllers/ClienteCartApiController.cs` | High |
| `[AllowAnonymous]` + sem validação MIME em receitas | `Controllers/ClientePrescriptionApiController.cs` | High |
| Sem verificação de role SUPER_ADMIN no admin marketplace | `Controllers/AdminMarketplaceController.cs` | High |
| Contatos internos da farmácia expostos na API pública | `DTOs/Mobile/MobilePharmacyDtos.cs`, `Controllers/Mobile/MobilePharmaciesController.cs` | Medium |
| JWT revocation list no logout mobile | `Middleware/JwtAuthMiddleware.cs`, `Controllers/Mobile/MobileAuthController.cs`, `Models/RevokedJwt.cs` | Medium |
| Cupom de outra farmácia aceito no checkout | `Controllers/ClienteApiController.cs` | Medium |
| `new Random()` em código de prescrição | `Controllers/ClientePrescriptionApiController.cs` | Low |
| OTP de verificação de email no registro mobile | `Controllers/Mobile/MobileAuthController.cs`, `DTOs/Mobile/MobileAuthDtos.cs` | High |
| Bloqueio de pedidos para clientes não verificados | `Controllers/Mobile/MobileOrdersController.cs` | High |

## ✅ Concluído nesta sessão — Últimas pendências de segurança (2026-07-05)

| Fix | Arquivo | Severidade |
|-----|---------|-----------|
| OTP hasheado com SHA-256+email-sal antes de gravar no banco | `Controllers/Mobile/MobileAuthController.cs`, `Models/CustomerAuth.cs` | Médio |
| Desconto de cupom calculado server-side (nunca confiado no cliente) | `Controllers/Mobile/MobileOrdersController.cs` | Alto |
| CartController legado: preço validado do banco (não do DTO do cliente) | `Controllers/CartController.cs` | Alto |
