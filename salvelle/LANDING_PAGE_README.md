# Landing Page - Salvelle

Landing page moderna criada para apresentar as vantagens da plataforma Salvelle e exibir os planos de assinatura com integração Stripe.

## Arquivos Criados

### View
- **[Views/Public/Landing.cshtml](Views/Public/Landing.cshtml)** - Landing page completa com seções de Hero, Features, Pricing e CTA

### Estilos
- **[wwwroot/css/landing.css](wwwroot/css/landing.css)** - Estilos customizados com design moderno usando Claude Frontend Designs

### JavaScript
- **[wwwroot/js/landing.js](wwwroot/js/landing.js)** - Lógica para carregar planos dinamicamente e alternar entre ciclos mensais/anuais

### Controller
- **[Controllers/PublicController.cs](Controllers/PublicController.cs)** - Adicionada rota `/landing`

### DTO
- **[DTOs/SubscriptionDtos.cs](DTOs/SubscriptionDtos.cs)** - Adicionado `SubscriptionPlanDto`

## Como Acessar

A landing page está disponível em:
```
https://seu-dominio.com/landing
```

## Funcionalidades

### 1. Hero Section
- Título chamativo com destaque para o nome da plataforma
- Subtítulo descritivo
- CTAs principais: "Começar Grátis" e "Ver Planos"
- Badges de benefícios (sem cartão, cancelamento livre)
- Imagem/logo da plataforma

### 2. Features Section
Apresenta 6 funcionalidades principais:
- ✅ Gestão de Produção
- ✅ Controle de Estoque
- ✅ Precificação Inteligente
- ✅ Prescrições Digitais
- ✅ Vendas & Orçamentos
- ✅ Relatórios & Analytics

### 3. Pricing Section
- **Toggle Mensal/Anual** com indicador de economia
- **Cards de Planos Dinâmicos** carregados da API
- **Destaque para plano mais popular**
- **Features detalhadas** por plano
- **Cálculo automático** de economia no plano anual
- **Links diretos** para signup com plano pré-selecionado

#### Informações exibidas em cada card:
- Nome do plano
- Descrição
- Preço mensal (calculado automaticamente para planos anuais)
- Preço total anual (quando aplicável)
- Economia em reais
- Limites (funcionários, pedidos/mês)
- Features do plano
- Botão de ação

### 4. CTA Section
- Seção de conversão final com gradiente chamativo
- CTA para "Começar Agora"

### 5. Footer
- Links para páginas importantes
- Links legais (Termos de Uso, Política de Privacidade)
- Copyright

## Integração com Stripe

A landing page está 100% integrada com o sistema de assinaturas Stripe:

1. **Carregamento de Planos**: Via API `/api/subscriptionplans?activeOnly=true`
2. **Preços Dinâmicos**: Busca os preços mensais e anuais diretamente do banco
3. **Link para Signup**: Redireciona para `/Signup?planId={id}&cycle={monthly|yearly}`

### Fluxo de Conversão

```
Landing Page → Seleção de Plano → Signup → Stripe Checkout → Assinatura Ativa
```

## API Endpoint

### GET /api/subscriptionplans

Retorna todos os planos ativos:

```json
[
  {
    "id": "guid",
    "name": "Básico",
    "description": "Plano ideal para farmácias pequenas",
    "priceMonthly": 199.00,
    "priceYearly": 1990.00,
    "maxEmployees": 5,
    "maxMonthlyOrders": 100,
    "features": {
      "production_management": true,
      "inventory_control": true,
      "automatic_pricing": true,
      "digital_prescriptions": true,
      "reports_dashboard": true,
      "email_support": true
    },
    "isActive": true
  }
]
```

## Customização

### Adicionar novas features aos planos

1. Adicione a feature no campo `Features` (JSON) do plano no banco de dados:
```json
{
  "production_management": true,
  "inventory_control": true,
  "custom_feature": true
}
```

2. Adicione o mapeamento de nome em [wwwroot/js/landing.js](wwwroot/js/landing.js#L91):
```javascript
const featureNames = {
    // ... outras features
    'custom_feature': 'Nome Amigável da Feature'
};
```

### Alterar cores e estilos

Edite as variáveis CSS em [wwwroot/css/landing.css](wwwroot/css/landing.css#L2):
```css
:root {
    --primary-color: #0d6efd;
    --secondary-color: #6c757d;
    --success-color: #198754;
    --gradient-primary: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}
```

### Alterar período de trial

O período de trial padrão é de 14 dias. Para alterar:

1. Edite [Service/StripeService.cs](Service/StripeService.cs#L152):
```csharp
TrialPeriodDays = 14, // Altere aqui
```

## Design System

A landing page utiliza:
- **Bootstrap 5.3.2** para grid e componentes
- **Bootstrap Icons 1.11.2** para ícones
- **CSS Customizado** com gradientes e animações modernas
- **Design Responsivo** mobile-first

### Paleta de Cores

| Cor | Hex | Uso |
|-----|-----|-----|
| Primary | `#0d6efd` | CTAs principais, links |
| Success | `#198754` | Badges de economia, checkmarks |
| Gradient Primary | `#667eea → #764ba2` | Cards featured, CTAs |
| Gradient Hero | `#f5f7fa → #c3cfe2` | Background hero section |

## Responsividade

A landing page é totalmente responsiva com breakpoints:
- **Desktop**: `> 992px` - Layout completo em 3 colunas
- **Tablet**: `768px - 992px` - Layout em 2 colunas
- **Mobile**: `< 768px` - Layout em 1 coluna

## Performance

### Otimizações implementadas:
- ✅ Lazy loading de imagens
- ✅ CSS minificado em produção
- ✅ JavaScript otimizado com cache
- ✅ Chamada única à API
- ✅ Renderização client-side eficiente

## SEO

### Meta tags recomendadas (adicionar ao `<head>`):
```html
<meta name="description" content="Sistema completo de gestão para farmácias de manipulação. Controle produção, estoque, vendas e precificação. 14 dias grátis.">
<meta name="keywords" content="farmácia de manipulação, gestão farmacêutica, software farmácia, controle de estoque">
<meta property="og:title" content="Salvelle - Sistema de Gestão para Farmácias de Manipulação">
<meta property="og:description" content="Aumente a eficiência da sua farmácia com nosso sistema completo.">
<meta property="og:image" content="/logoFormulaClear.png">
```

## Testes

### Checklist de testes:
- [ ] Planos carregam corretamente da API
- [ ] Toggle mensal/anual funciona
- [ ] Cálculo de economia está correto
- [ ] Links de signup incluem planId e cycle
- [ ] Layout responsivo em mobile/tablet/desktop
- [ ] Smooth scroll funciona nos anchor links
- [ ] Tratamento de erro quando API falha

## Próximos Passos

### Melhorias sugeridas:
1. Adicionar depoimentos de clientes
2. Adicionar seção de FAQ
3. Adicionar vídeo demo
4. Implementar A/B testing
5. Adicionar chat ao vivo
6. Implementar analytics (Google Analytics, Hotjar)
7. Adicionar botão de comparação de planos
8. Implementar modal de preview do dashboard

## Suporte

Para dúvidas ou problemas, verifique:
- Controller: [Controllers/PublicController.cs](Controllers/PublicController.cs)
- API: [Controllers/SubscriptionPlansController.cs](Controllers/SubscriptionPlansController.cs)
- Service: [Service/StripeService.cs](Service/StripeService.cs)
