# Salvelle Marketplace - Guia do Projeto

## Visão Geral

Transformar o Salvelle de um SaaS de gestão farmacêutica em um **marketplace de farmácias** no modelo iFood, onde farmácias vendem produtos prontos e serviços de manipulação, e clientes compram via aplicativo mobile.

---

## 1. Modelo de Negócio

### Atores
| Ator | Acesso | Cobrança |
|------|--------|----------|
| **Admin da Plataforma** | Painel web administrativo | — |
| **Farmácia (Seller)** | Painel web de gestão | Assinatura mensal + comissão por venda |
| **Cliente (Buyer)** | App mobile (Android/iOS) | Gratuito |

### Receita da Plataforma
1. **Assinatura mensal** da farmácia (já existe no sistema atual)
2. **Comissão escalonada** sobre cada venda:

| Volume semanal | Comissão |
|----------------|----------|
| Até 20 produtos | 7% |
| 21–100 produtos | 5% |
| Acima de 100 produtos | 3% |

> A comissão é recalculada semanalmente com base no volume de vendas da farmácia.

---

## 2. O Que Já Temos (Aproveitamento de Features)

### Pode ser reaproveitado diretamente
- [x] **Autenticação multi-tenant** (Admin, Employee, Customer) — 3 middlewares prontos
- [x] **Gestão de assinaturas** (SubscriptionPlan, Subscription, trials)
- [x] **Integração Stripe** (checkout, webhooks, subscriptions)
- [x] **Cadastro de farmácias** (Establishment com CNPJ, endereço, horário)
- [x] **Cadastro de clientes** (Customer com CPF, alergias, LGPD)
- [x] **Catálogo de produtos** (CatalogProduct, CatalogCategory)
- [x] **Carrinho de compras** (CustomerCart, CartItem)
- [x] **Pedidos online** (OnlineOrder, OnlineOrderItem)
- [x] **Fórmulas e manipulação** (Formula, ManipulationOrder, workflow completo)
- [x] **Prescrições com OCR** (Prescription, PrescriptionQuote)
- [x] **Notificações WhatsApp/Email** (AtentBot, SMTP)
- [x] **Labels ANVISA** (QuestPDF)
- [x] **Controle de estoque** (RawMaterial, StockMovement, Batch)
- [x] **Dashboard admin** (AdminDashboardController)
- [x] **QR Codes** (EstablishmentQRCode)
- [x] **Segurança** (rate limiting, CORS, LGPD, audit logs)
- [x] **API REST com session tokens** (mobile-friendly)

### Precisa de adaptação
- [ ] **Customer Auth** → adaptar para fluxo mobile (JWT em vez de session cookies)
- [ ] **Catálogo** → adicionar geolocalização e busca por proximidade
- [ ] **Pagamentos** → split payment (cliente paga → plataforma retém comissão → repassa farmácia)
- [ ] **Pedidos** → adicionar previsão de entrega e status em tempo real
- [ ] **Dashboard farmácia** → métricas de marketplace (comissão, ranking, volume)

### Precisa ser criado do zero
- [ ] API mobile dedicada para o app do cliente
- [ ] Sistema de geolocalização (busca por raio/região)
- [ ] Motor de comissão escalonada
- [ ] Split payment / repasse financeiro
- [ ] Sistema de avaliações (farmácia e produto)
- [ ] Push notifications (Firebase FCM)
- [ ] App mobile (React Native ou Flutter)
- [ ] Painel de conciliação financeira

---

## 3. Desafios Técnicos

### 3.1 Split Payment e Comissão
**Desafio:** A plataforma precisa reter a comissão e repassar o restante à farmácia automaticamente.
**Solução:** Usar **Stripe Connect** (ou equivalente) com split payment. Cada farmácia terá uma conta conectada.
- Stripe Connect permite definir `application_fee_amount` por transação
- A comissão é calculada em tempo real com base no volume semanal
- Necessário cron job semanal para recalcular a faixa de comissão de cada farmácia

### 3.2 Geolocalização
**Desafio:** Clientes precisam encontrar farmácias próximas.
**Solução:**
- Adicionar colunas `Latitude` e `Longitude` na tabela `Establishments`
- Usar PostGIS (extensão do PostgreSQL) para consultas geoespaciais
- Índice espacial para performance em buscas por raio
- Geocoding do endereço da farmácia no cadastro (Google Maps API ou OpenStreetMap)

### 3.3 Autenticação Mobile
**Desafio:** O sistema atual usa cookies/session tokens, inadequado para apps mobile.
**Solução:**
- Implementar JWT (JSON Web Tokens) para APIs mobile
- Refresh tokens com rotação
- Login social (Google, Apple) para facilitar onboarding
- Manter session tokens para web (backward compatible)

### 3.4 Performance e Escala
**Desafio:** Marketplace pode crescer rapidamente em volume de requisições.
**Solução:**
- Cache Redis para catálogos, rankings e resultados de busca
- CDN para imagens de produtos
- Paginação e lazy loading em todas as listagens
- Filas (RabbitMQ ou Azure Service Bus) para processamento assíncrono (notificações, relatórios)

### 3.5 App Mobile
**Desafio:** Desenvolver apps nativos para Android e iOS.
**Solução:**
- **React Native** ou **Flutter** para código compartilhado
- API-first: todas as funcionalidades expostas via REST API
- Deep linking para compartilhamento de produtos/farmácias

### 3.6 Conciliação Financeira
**Desafio:** Garantir que comissões e repasses estejam corretos.
**Solução:**
- Tabela de transações com status (PENDENTE → PROCESSADO → REPASSADO)
- Relatório de conciliação diário/semanal
- Dashboard financeiro para admin e para farmácia

---

## 4. Modificações no Banco de Dados

### 4.1 Novas Tabelas

```
┌─────────────────────────────────────────────┐
│ PlatformCommissions                         │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ EstablishmentId (int, FK)                   │
│ WeekStartDate (DateTime)                    │
│ WeekEndDate (DateTime)                      │
│ TotalSalesCount (int)                       │
│ CommissionRate (decimal) — 0.03/0.05/0.07   │
│ TotalSalesAmount (decimal)                  │
│ TotalCommissionAmount (decimal)             │
│ Status (enum: CALCULADO, COBRADO, PAGO)     │
│ CreatedAt, UpdatedAt                        │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ PlatformTransactions                        │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ OrderId (int, FK → OnlineOrders)            │
│ EstablishmentId (int, FK)                   │
│ CustomerId (int, FK)                        │
│ GrossAmount (decimal)                       │
│ CommissionRate (decimal)                    │
│ CommissionAmount (decimal)                  │
│ NetAmountToPharmacy (decimal)               │
│ StripePaymentIntentId (string)              │
│ StripeTransferId (string)                   │
│ Status (enum: PENDENTE, CAPTURADO,          │
│         REPASSADO, ESTORNADO)               │
│ CreatedAt                                   │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ PharmacyRatings                             │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ EstablishmentId (int, FK)                   │
│ CustomerId (int, FK)                        │
│ OrderId (int, FK)                           │
│ Rating (int, 1-5)                           │
│ Comment (string, nullable)                  │
│ CreatedAt                                   │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ ProductRatings                              │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ CatalogProductId (int, FK)                  │
│ CustomerId (int, FK)                        │
│ OrderId (int, FK)                           │
│ Rating (int, 1-5)                           │
│ Comment (string, nullable)                  │
│ CreatedAt                                   │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ DeliveryEstimates                           │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ OrderId (int, FK)                           │
│ EstimatedMinutes (int)                      │
│ EstimatedDeliveryAt (DateTime)              │
│ ActualDeliveryAt (DateTime, nullable)       │
│ Status (enum: ESTIMADO, EM_PREPARO,         │
│         SAIU_ENTREGA, ENTREGUE)             │
│ CreatedAt, UpdatedAt                        │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ CustomerDevices (push notifications)        │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ CustomerId (int, FK)                        │
│ DeviceToken (string)                        │
│ Platform (enum: ANDROID, IOS)               │
│ IsActive (bool)                             │
│ CreatedAt, UpdatedAt                        │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ CustomerAddresses                           │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ CustomerId (int, FK)                        │
│ Label (string — "Casa", "Trabalho")         │
│ Street, Number, Complement, Neighborhood    │
│ City, State, ZipCode                        │
│ Latitude (double)                           │
│ Longitude (double)                          │
│ IsDefault (bool)                            │
│ CreatedAt                                   │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ PharmacyPayoutAccounts                      │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ EstablishmentId (int, FK)                   │
│ StripeConnectAccountId (string)             │
│ BankName, AgencyNumber, AccountNumber       │
│ PixKey (string, nullable)                   │
│ Status (enum: PENDENTE, VERIFICADO, ATIVO)  │
│ CreatedAt, UpdatedAt                        │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ SearchHistory (analytics)                   │
├─────────────────────────────────────────────┤
│ Id (int, PK)                                │
│ CustomerId (int, FK, nullable)              │
│ SearchTerm (string)                         │
│ Latitude, Longitude (double, nullable)      │
│ ResultCount (int)                           │
│ CreatedAt                                   │
└─────────────────────────────────────────────┘
```

### 4.2 Alterações em Tabelas Existentes

```
Establishments (adicionar):
  + Latitude (double)
  + Longitude (double)
  + IsMarketplaceActive (bool, default false)
  + MarketplaceDescription (string)
  + LogoUrl (string)
  + BannerUrl (string)
  + AverageRating (decimal, computed)
  + TotalRatings (int)
  + DeliveryRadiusKm (decimal)
  + MinOrderAmount (decimal)
  + AverageDeliveryMinutes (int)
  + StripeConnectAccountId (string)

Customers (adicionar):
  + DefaultLatitude (double, nullable)
  + DefaultLongitude (double, nullable)
  + ProfileImageUrl (string, nullable)
  + FirebaseFcmToken (string, nullable)
  + PreferredPaymentMethod (string, nullable)
  + LoginProvider (enum: EMAIL, GOOGLE, APPLE)

OnlineOrders (adicionar):
  + PlatformCommissionRate (decimal)
  + PlatformCommissionAmount (decimal)
  + NetAmountToPharmacy (decimal)
  + EstimatedDeliveryAt (DateTime, nullable)
  + ActualDeliveryAt (DateTime, nullable)
  + DeliveryStatus (enum)
  + DeliveryLatitude, DeliveryLongitude (double)
  + StripePaymentIntentId (string)

CatalogProducts (adicionar):
  + AverageRating (decimal)
  + TotalRatings (int)
  + TotalSold (int)
  + IsMarketplaceVisible (bool)
  + SearchKeywords (string) — para busca full-text
```

---

## 5. Arquitetura de APIs Mobile

### 5.1 Endpoints Públicos (sem autenticação)
```
GET  /api/mobile/v1/pharmacies/nearby?lat={lat}&lng={lng}&radius={km}
GET  /api/mobile/v1/pharmacies/{id}
GET  /api/mobile/v1/pharmacies/{id}/products
GET  /api/mobile/v1/search?q={term}&lat={lat}&lng={lng}
GET  /api/mobile/v1/categories
```

### 5.2 Endpoints Autenticados (JWT)
```
POST /api/mobile/v1/auth/register
POST /api/mobile/v1/auth/login
POST /api/mobile/v1/auth/login/google
POST /api/mobile/v1/auth/login/apple
POST /api/mobile/v1/auth/refresh-token
POST /api/mobile/v1/auth/forgot-password

GET  /api/mobile/v1/profile
PUT  /api/mobile/v1/profile
GET  /api/mobile/v1/addresses
POST /api/mobile/v1/addresses
PUT  /api/mobile/v1/addresses/{id}

GET  /api/mobile/v1/cart
POST /api/mobile/v1/cart/items
PUT  /api/mobile/v1/cart/items/{id}
DELETE /api/mobile/v1/cart/items/{id}

POST /api/mobile/v1/orders
GET  /api/mobile/v1/orders
GET  /api/mobile/v1/orders/{id}
GET  /api/mobile/v1/orders/{id}/track

POST /api/mobile/v1/payments/create-intent
POST /api/mobile/v1/payments/confirm

POST /api/mobile/v1/ratings
GET  /api/mobile/v1/ratings/pharmacy/{id}

POST /api/mobile/v1/prescriptions/upload
GET  /api/mobile/v1/prescriptions
```

### 5.3 Endpoints da Farmácia (painel)
```
GET  /api/pharmacy/v1/orders
PUT  /api/pharmacy/v1/orders/{id}/accept
PUT  /api/pharmacy/v1/orders/{id}/reject
PUT  /api/pharmacy/v1/orders/{id}/delivery-estimate
PUT  /api/pharmacy/v1/orders/{id}/status

GET  /api/pharmacy/v1/financial/summary
GET  /api/pharmacy/v1/financial/commissions
GET  /api/pharmacy/v1/financial/payouts

GET  /api/pharmacy/v1/dashboard
GET  /api/pharmacy/v1/ratings
```

---

## 6. Plano de Trabalho (Fases)

### Fase 1 — Fundação (Semanas 1–3)
> Infraestrutura base para o marketplace

- [ ] Criar migrations para novas tabelas e colunas
- [ ] Implementar PostGIS e consultas geoespaciais
- [ ] Adicionar Latitude/Longitude no cadastro de Establishment
- [ ] Implementar JWT Authentication para APIs mobile
- [ ] Criar endpoint de busca por proximidade
- [ ] Configurar Stripe Connect para split payment
- [ ] Criar modelo PlatformCommission e serviço de cálculo

### Fase 2 — APIs Mobile Core (Semanas 4–6)
> APIs que o app mobile vai consumir

- [ ] API de registro/login de clientes (JWT + social login)
- [ ] API de busca de farmácias por região
- [ ] API de busca de produtos (full-text search)
- [ ] API de detalhes de farmácia e catálogo
- [ ] API de carrinho de compras
- [ ] API de criação de pedido com pagamento (Stripe)
- [ ] API de acompanhamento de pedido em tempo real
- [ ] API de endereços do cliente

### Fase 3 — Painel da Farmácia (Semanas 7–9)
> Dashboard marketplace para a farmácia

- [ ] Tela de pedidos recebidos (aceitar/rejeitar)
- [ ] Informar previsão de entrega
- [ ] Atualizar status do pedido
- [ ] Dashboard financeiro (vendas, comissões, repasses)
- [ ] Gestão de catálogo marketplace (ativar/desativar produtos)
- [ ] Configurações de entrega (raio, tempo médio, valor mínimo)

### Fase 4 — App Mobile (Semanas 10–16)
> Aplicativo do cliente

- [ ] Setup do projeto (React Native ou Flutter)
- [ ] Tela de onboarding e login (email + social)
- [ ] Tela home com farmácias próximas
- [ ] Busca por farmácia e por produto
- [ ] Tela de detalhes da farmácia com catálogo
- [ ] Carrinho e checkout com Stripe
- [ ] Acompanhamento de pedido
- [ ] Perfil e endereços
- [ ] Push notifications (Firebase FCM)
- [ ] Upload de prescrição via câmera
- [ ] Sistema de avaliações

### Fase 5 — Financeiro e Comissões (Semanas 17–19)
> Motor de comissão e conciliação

- [ ] Cron job semanal de cálculo de comissão
- [ ] Split payment automático via Stripe Connect
- [ ] Dashboard financeiro admin (receita total, comissões, repasses)
- [ ] Relatório de conciliação
- [ ] Extrato financeiro por farmácia
- [ ] Alertas de pagamentos pendentes

### Fase 6 — Polimento e Lançamento (Semanas 20–22)
> Qualidade, testes e go-live

- [ ] Testes de carga (APIs de busca e pagamento)
- [ ] Auditoria de segurança
- [ ] Publicação nas lojas (Google Play, App Store)
- [ ] Monitoramento (Sentry, Application Insights)
- [ ] Documentação de APIs (Swagger/OpenAPI)
- [ ] Onboarding de farmácias piloto

---

## 7. Checklist Completo

### 7.1 Segurança
- [ ] JWT com expiração curta (15 min) + refresh token (30 dias)
- [ ] Rate limiting em endpoints de auth (já existe, adaptar para mobile)
- [ ] Validação de input em todas as APIs mobile (FluentValidation)
- [ ] HTTPS obrigatório em todas as comunicações
- [ ] Stripe Connect com verificação KYC das farmácias
- [ ] PCI DSS compliance (nunca armazenar dados de cartão — usar Stripe)
- [ ] Proteção contra replay attacks em pagamentos (idempotency keys)
- [ ] Sanitização de queries de busca (prevenir SQL injection via PostGIS)
- [ ] CORS restrito para domínios autorizados
- [ ] Audit log de todas as transações financeiras
- [ ] Encriptação de dados sensíveis em repouso (AesEncryptionService já existe)
- [ ] Proteção de endpoints admin com autenticação forte
- [ ] Validação de webhook Stripe (assinatura do evento)
- [ ] LGPD: consentimento do cliente, direito ao esquecimento
- [ ] Tokens de push notification com escopo limitado
- [ ] Proteção contra enumeração de usuários (respostas genéricas em auth)

### 7.2 Banco de Dados
- [ ] Migrations para todas as novas tabelas
- [ ] Extensão PostGIS habilitada no PostgreSQL
- [ ] Índice espacial (GiST) em Latitude/Longitude de Establishments
- [ ] Índice full-text (GIN) em CatalogProducts.SearchKeywords
- [ ] Índice composto em PlatformCommissions(EstablishmentId, WeekStartDate)
- [ ] Índice em PlatformTransactions(OrderId, Status)
- [ ] Índice em PharmacyRatings(EstablishmentId) e ProductRatings(CatalogProductId)
- [ ] Foreign keys com ON DELETE adequado (RESTRICT para financeiro, CASCADE para cart)
- [ ] Campos decimais com precisão adequada (18,2 para valores, 18,4 para taxas)
- [ ] Backup e recovery testados
- [ ] Seed data para planos de assinatura e categorias
- [ ] Views materializadas para rankings e médias de avaliação

### 7.3 Aproveitamento de Features Existentes
- [ ] Reusar CustomerAuthService para auth mobile (adicionar JWT)
- [ ] Reusar CatalogProduct/CatalogCategory para marketplace
- [ ] Reusar OnlineOrder/OnlineOrderItem (adicionar campos de comissão)
- [ ] Reusar CustomerCart/CartItem (validar multi-establishment)
- [ ] Reusar PrescriptionService para upload via mobile
- [ ] Reusar EmailService e WhatsAppService para notificações
- [ ] Reusar StripeService (evoluir para Stripe Connect)
- [ ] Reusar FormulaService para pedidos de manipulação
- [ ] Reusar AuditService para logs financeiros
- [ ] Reusar LabelService para pedidos manipulados
- [ ] Reusar Establishment model (adicionar campos geo/marketplace)

### 7.4 Usabilidade — App do Cliente
- [ ] Onboarding em no máximo 3 telas
- [ ] Login social (Google/Apple) em 1 toque
- [ ] Mapa interativo com farmácias próximas
- [ ] Busca com autocomplete e sugestões
- [ ] Filtros: distância, avaliação, preço, tipo (manipulação/pronto)
- [ ] Fotos de produtos com zoom
- [ ] Carrinho persistente (mesmo sem login)
- [ ] Checkout em no máximo 3 passos
- [ ] Acompanhamento de pedido com timeline visual
- [ ] Push notification em cada mudança de status
- [ ] Histórico de pedidos com opção de repetir
- [ ] Favoritar farmácias
- [ ] Compartilhar produto via deep link
- [ ] Modo escuro
- [ ] Acessibilidade (WCAG)
- [ ] Suporte a múltiplos endereços de entrega
- [ ] Chat ou canal de suporte in-app

### 7.5 Usabilidade — Painel da Farmácia
- [ ] Notificação sonora/visual de novo pedido
- [ ] Aceitar/rejeitar pedido em 1 clique
- [ ] Timer de tempo de resposta (SLA)
- [ ] Dashboard com métricas em tempo real (vendas, comissão, avaliação)
- [ ] Gestão de catálogo com drag-and-drop para ordenação
- [ ] Upload de fotos de produtos com crop automático
- [ ] Horário de funcionamento configurável
- [ ] Pausar recebimento de pedidos (modo férias/manutenção)
- [ ] Relatório financeiro exportável (PDF/Excel)
- [ ] Visualizar avaliações e responder

### 7.6 Infraestrutura e DevOps
- [ ] Redis para cache de buscas e sessões
- [ ] CDN para imagens (CloudFlare ou S3+CloudFront)
- [ ] Fila de mensagens para notificações assíncronas
- [ ] Health checks nos endpoints
- [ ] CI/CD pipeline com testes automatizados
- [ ] Staging environment para testes
- [ ] Monitoramento APM (Application Insights ou Sentry)
- [ ] Logs centralizados (Seq, ELK ou similar)
- [ ] Auto-scaling na camada de API
- [ ] Backups automatizados do banco
- [ ] Rate limiting granular por endpoint

### 7.7 Compliance e Legal
- [ ] Termos de uso para clientes
- [ ] Termos de uso para farmácias (contrato marketplace)
- [ ] Política de privacidade (LGPD)
- [ ] Política de cancelamento e reembolso
- [ ] Política de comissões e repasses
- [ ] Nota fiscal das comissões (plataforma → farmácia)
- [ ] Compliance com regulamentações da ANVISA para venda online de medicamentos
- [ ] Verificação de licenças sanitárias das farmácias
- [ ] Restrição de venda de controlados via marketplace

---

## 8. Stack Tecnológica Recomendada

| Componente | Tecnologia | Justificativa |
|------------|------------|---------------|
| Backend API | ASP.NET Core 9 | Já existe, manter |
| Banco de dados | PostgreSQL + PostGIS | Já existe, adicionar PostGIS |
| Cache | Redis | Performance em buscas |
| Fila de mensagens | RabbitMQ | Notificações assíncronas |
| Pagamentos | Stripe Connect | Split payment nativo |
| App mobile | React Native ou Flutter | Código compartilhado Android/iOS |
| Push notifications | Firebase FCM | Gratuito, confiável |
| Armazenamento de imagens | S3 + CloudFront ou CloudFlare R2 | CDN integrado |
| Monitoramento | Sentry + Application Insights | Erros + performance |
| Geolocalização | PostGIS + Google Maps SDK | Backend + frontend |
| Busca full-text | PostgreSQL GIN indexes | Simples, sem serviço extra |
| Auth mobile | JWT + Refresh Tokens | Padrão mobile |
| Social login | Google Sign-In, Apple Sign-In | Facilita onboarding |

---

## 9. Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Regulamentação ANVISA para venda online | Bloqueio legal | Consultar advogado farmacêutico antes do lançamento |
| Complexidade do split payment | Atrasos financeiros | Usar Stripe Connect (já resolvido pelo Stripe) |
| Baixa adesão de farmácias | Receita insuficiente | Plano freemium inicial, onboarding assistido |
| Performance em buscas geo | UX ruim | PostGIS com índices otimizados, cache Redis |
| Fraude em pagamentos | Perdas financeiras | Stripe Radar (anti-fraude), KYC de farmácias |
| Concorrência (iFood, Rappi) | Market share | Foco em nicho: farmácias de manipulação |
| Escala de operação | Infraestrutura | Auto-scaling, filas, cache desde o início |

---

## 10. KPIs do Marketplace

| Métrica | Descrição |
|---------|-----------|
| GMV (Gross Merchandise Value) | Volume total de vendas na plataforma |
| Take Rate | % médio de comissão efetiva |
| Farmácias ativas | Farmácias com pelo menos 1 venda/semana |
| Clientes ativos | Clientes com pelo menos 1 compra/mês |
| Ticket médio | Valor médio por pedido |
| Taxa de conversão | Visitantes → compradores |
| NPS | Net Promoter Score de clientes e farmácias |
| Tempo de resposta | Tempo médio para farmácia aceitar pedido |
| Taxa de cancelamento | % de pedidos cancelados |
| Churn rate | Farmácias que cancelam assinatura/mês |

---

*Documento criado em 2026-03-13 — Atualizar conforme o projeto evolui.*
