# Salvelle — Mapa de telas

Cada linha = uma rota. "Mockup" aponta para o HTML; "Estados" lista variações que o dev precisa implementar (não estão no mockup).

## Públicas (sem auth)

| Rota | Mockup | Permissão | Estados a implementar |
|---|---|---|---|
| `/` | `index.html` | pública | — |
| `/precos` | `pricing.html` | pública | — |
| `/cadastrar` | `signup.html` | pública | validação por campo, e-mail já em uso, fraqueza de senha, sucesso → onboarding |
| `/entrar` | `login.html` | pública | senha errada, conta bloqueada, MFA challenge, "esqueci minha senha", redirect pós-login por papel |
| `/esqueci-senha` | (a desenhar) | pública | envio de e-mail, token expirado, sucesso |
| `/redefinir-senha` | (a desenhar) | pública (com token) | token válido, expirado, senha redefinida |

## Operacionais (auth obrigatória)

| Rota | Mockup | Permissão | Estados a implementar |
|---|---|---|---|
| `/dashboard` | `dashboard.html` | todos | empty (primeiro acesso), loading, erro de carregamento |
| `/pedidos` | `orders.html` | atendente+ | empty, filtro vazio, paginação, exportação |
| `/pedidos/:id` | (a desenhar) | atendente+ | edição, histórico, anexos, audit trail visível |
| `/pedidos/novo` | (parte de orders) | atendente+ | rascunho, validação, conflito de receita |
| `/manipulacao` | `manipulation.html` | manipulador+ | fila vazia, em andamento, em pausa, finalizado |
| `/manipulacao/:id` | (parte de manipulation) | manipulador+ | passo a passo, validação RT, assinatura digital |
| `/pesagem` | `weighing.html` | manipulador+ | balança offline, fora de tolerância, pesagem confirmada |
| `/pdv` | `pdv.html` | atendente+ | venda em andamento, contingência fiscal, troco, sangria, fechamento de caixa |
| `/ocr` | `ocr.html` | atendente+ | upload, processando, baixa confiança, validação manual |
| `/compras` | `purchases.html` | admin/RT | empty, em rascunho, enviado, recebido, divergência |
| `/equipe` | `employees.html` | admin | empty, RT inativo (alerta crítico), papéis, convite pendente |
| `/clientes` | (a desenhar) | atendente+ | busca, ficha completa, histórico de pedidos, LGPD (consentimentos, direitos) |
| `/formulas` | (a desenhar) | RT/admin | catálogo, edição, versionamento (mudança de fórmula gera nova versão) |
| `/estoque` | (a desenhar) | manipulador+ | matéria-prima, lote, validade, alerta de baixo |
| `/relatorios` | (a desenhar) | admin/RT | financeiro, operacional, regulatório (SNGPC) |
| `/configuracoes` | (a desenhar) | admin | farmácia, fiscal, integrações, billing |
| `/configuracoes/privacidade` | (a desenhar) | qualquer | exportar dados, excluir conta (LGPD) |

## Telas que **faltam desenhar** (alta prioridade antes do dev começar)

1. **Cliente / paciente** (CRUD + ficha + LGPD) — bloqueia muito do fluxo
2. **Fórmula** (catálogo + versionamento) — bloqueia manipulação
3. **Estoque + lote** (rastreabilidade Anvisa) — bloqueia compliance
4. **Detalhe do pedido** (edição completa) — bloqueia operação
5. **Esqueci/redefinir senha** — bloqueia recuperação
6. **Empty states** — todas as telas em primeiro acesso
7. **Onboarding** — primeiro pedido, primeira fórmula, configuração inicial

---

## Permissões — matriz simplificada

| | Atendente | Manipulador | RT | Admin |
|---|:---:|:---:|:---:|:---:|
| Ver dashboard | ✓ | ✓ | ✓ | ✓ |
| Criar pedido | ✓ | — | ✓ | ✓ |
| Editar fórmula | — | — | ✓ | ✓ |
| Manipular | — | ✓ | ✓ | — |
| Validar/assinar OM | — | — | ✓ | — |
| Dispensar controlado | — | — | ✓ | — |
| Ver financeiro | — | — | parcial | ✓ |
| Gerenciar equipe | — | — | — | ✓ |
| Configurações | — | — | parcial | ✓ |

---

## Acessibilidade (mínimo go-live)

- WCAG 2.1 AA em todas as rotas públicas e auth
- WCAG 2.1 AA "best effort" nas operacionais (PDV pode quebrar regras de denso/teclado pela natureza)
- Contraste petrol/branco: ✓ (4.7:1 em fundo bone)
- Focus visível em todos os elementos interativos (já no `tokens.css`)
- Skip link "Pular para conteúdo principal" — adicionar no shell
- Suporte teclado completo no PDV (operação por leitor de código de barras, atalhos)
- ARIA labels em ícones-ação (sidebar, topbar, tabela)
- Anúncio de mudanças assíncronas (toast, dispensa concluída) com `aria-live`

---

## Performance — orçamento por rota

| Métrica | Pública | Auth |
|---|---|---|
| LCP | < 2.5s | < 2.0s |
| INP | < 200ms | < 200ms |
| CLS | < 0.1 | < 0.1 |
| JS inicial | < 200KB gzip | < 350KB gzip |

PDV **deve** ser local-first: continuar operando sem rede por até 30min, sincronizar quando voltar (fila de eventos).

---

## i18n

MVP: pt-BR apenas. Estrutura preparada para en-US posteriormente (literais via `next-intl` ou `react-i18next`).
