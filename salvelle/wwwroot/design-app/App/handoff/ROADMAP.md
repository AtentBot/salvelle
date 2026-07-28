# Salvelle — Roadmap de implementação

> Estimativas em **semanas-equipe** (1 PM, 2 devs full-stack, 1 designer, 0.5 RT consultor). Multiplicar conforme realidade.

## Fase 0 — Fundação (4 semanas)

**Objetivo:** infra e fundamentos que travam tudo.

- Stack escolhido + repositório + CI/CD
- Auth + MFA + RBAC + multi-tenancy
- Audit log com hash chain
- Tokens visuais aplicados (CSS/Tailwind)
- Componentes base implementados (botão, input, card, tabela, badge, modal)
- LGPD básico: política, termos, DPO contratado
- Hospedagem em região BR (data residency)

**Saída:** signup → login → dashboard vazio funcionando, com auditoria.

---

## Fase 1 — Cadastros (3 semanas)

**Objetivo:** dados-mestre que alimentam tudo.

- CRUD Clientes (com consentimento LGPD)
- CRUD Fórmulas (com versionamento)
- CRUD Matérias-primas / lotes
- CRUD Equipe + papéis + RT ativo obrigatório
- Importação inicial via CSV (clientes, fórmulas)

**Saída:** farmácia consegue se "instalar" no app.

---

## Fase 2 — Fluxo magistral (5 semanas) ⭐ núcleo do produto

**Objetivo:** receita → manipulado pronto.

- OCR de receita (provider escolhido, com fallback manual)
- Criação de pedido (a partir de OCR ou manual)
- Detalhe de pedido + edição
- Ordem de Manipulação (RDC 67)
- Pesagem (integração com balança via agente local)
- Validação de RT + assinatura digital
- Geração de rótulo Anvisa (PDF)
- Status: orçamento → pendente → manipulando → pronto → entregue

**Saída:** primeiro manipulado real produzido pelo app.

**Bloqueador externo:** integração com balança (depende da farmácia-piloto).

---

## Fase 3 — Venda & fiscal (4 semanas)

**Objetivo:** receber dinheiro do cliente final.

- PDV completo (continuar pedido OU venda direta)
- TEF + PIX
- NFC-e (integração + contingência offline)
- Fechamento de caixa, sangria, suprimento
- Relatório fiscal mensal

**Saída:** PDV operacional em farmácia-piloto.

---

## Fase 4 — Controlados & SNGPC (3 semanas)

**Objetivo:** habilitar venda de Lista A/B/C.

- Captura de notificação de receita
- Validação de quantidade máxima por substância
- PIN/biometria do RT na dispensa
- Livro de registro de controlados (visualização)
- Geração mensal SNGPC + envio Anvisa
- Alertas de fluxo (estoque baixo, validade próxima)

**Saída:** farmácia pode vender controlados pelo app dentro da lei.

---

## Fase 5 — Compras & estoque avançado (3 semanas)

**Objetivo:** fechar o loop de inventário.

- Pedido de compra
- Cotação de fornecedores
- Recebimento + conferência + entrada em estoque
- Curva ABC, ponto de pedido automático
- Inventário cíclico

---

## Fase 6 — CRM & retenção (2 semanas)

- Notificação WhatsApp ("fórmula pronta")
- Lembrete de retorno (receita vencendo)
- NPS pós-retirada
- Relatório de fidelidade

---

## Fase 7 — Polimento & go-live (3 semanas)

- Onboarding guiado
- Empty states + estados de erro
- Performance audit + correções
- Pen-test
- Documentação ao usuário (help center)
- Treinamento da farmácia-piloto

---

## Total estimado: ~27 semanas (≈6 meses) com a equipe sugerida

Não inclui:
- Marketing
- Vendas
- Suporte ao cliente (SAC)
- Implantação em farmácias clientes (≈2 semanas/farmácia no início)
- Manutenção e novas features pós-launch

---

## Alternativas se o orçamento for menor

### MVP enxuto (10 semanas) — provar valor antes de investir tudo

- Fase 0 (4 sem)
- Fase 1 reduzida — só Clientes e Fórmulas (1 sem)
- Fase 2 reduzida — sem OCR, sem balança automática, manipulação como checklist (4 sem)
- Sem PDV, sem fiscal, sem SNGPC

**Use case:** farmácia faz orçamento e ordem de manipulação no app, mas continua usando ERP atual para fiscal/PDV/controlados. Vende como "agora você acompanha cada manipulação em tempo real e tem rastreabilidade real".

### MVP completo mas vertical (16 semanas) — uma única farmácia-piloto, tudo manual

- Fases 0, 1, 2 completas
- Fiscal manual (gerar dados, exportar para o ERP atual)
- Sem multi-tenant — instância dedicada para a piloto
- SNGPC manual (gera CSV, RT envia)

**Use case:** comprovar valor com 1 cliente antes de virar SaaS multi-tenant.

---

## Decisões que precisam acontecer ANTES da Fase 0

1. **Mercado-alvo:** SaaS multi-tenant ou software vertical para grupos de farmácias? Muda arquitetura.
2. **Provider de auth:** build ou buy? Recomendo buy (Auth0/Clerk).
3. **Provider de OCR:** define LGPD (transferência internacional).
4. **Provider de fiscal:** define velocidade (Focus NFe = rápido, SEFAZ direto = barato e lento).
5. **Farmácia-piloto:** sem ela, Fase 2 não fecha. Idealmente uma farmácia já operando, que aceita usar em paralelo por 60 dias.
6. **Farmacêutico RT consultor:** sem ele, COMPLIANCE.md fica no papel.
7. **Advogado sanitário:** validar termos, política, e arquivamento.
