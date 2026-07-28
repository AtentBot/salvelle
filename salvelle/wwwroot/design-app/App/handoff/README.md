# Salvelle — Pacote de Handoff

> **Status:** mockups de design em alta fidelidade. Não é um produto funcional.
> **Próximo passo:** transformar este pacote em um app real (backend + frontend + integrações).

Este diretório (`App/`) contém:

| Pasta / arquivo | Para quem | Conteúdo |
|---|---|---|
| `index.html`, `pricing.html`, `signup.html`, `login.html` | **Design + Dev** | Landing + auth |
| `dashboard.html`, `orders.html`, `manipulation.html`, `weighing.html`, `pdv.html`, `ocr.html`, `purchases.html`, `employees.html` | **Design + Dev** | Telas operacionais (admin) |
| `ds/tokens.css` | **Design + Dev** | Variáveis CSS — fonte da verdade visual |
| `ds/components.css` | **Design + Dev** | Estilos de botão, input, card, tabela, badge, sidebar, topbar |
| `ds/icons.jsx`, `ds/shell.jsx` | **Dev** | Componentes React (placeholder; reescrever em produção) |
| `handoff/tokens.json` | **Dev** | Tokens em JSON — alimenta Style Dictionary, Figma Tokens |
| `handoff/tailwind.config.js` | **Dev** | Config pronta para projetos Tailwind |
| `handoff/SECURITY.md` | **Dev + Compliance + Jurídico** | Requisitos LGPD, autenticação, criptografia |
| `handoff/COMPLIANCE.md` | **Compliance + Jurídico** | RDC 67/2007, RDC 44/2009, SNGPC, fiscal |
| `handoff/INTEGRATIONS.md` | **Dev + Produto** | Lista de integrações externas e APIs |
| `handoff/SCREENS.md` | **Dev + Produto + QA** | Mapa de telas, rotas, permissões, estados |
| `handoff/ROADMAP.md` | **Produto + Liderança** | Plano de implementação faseado |

---

## Como ler este handoff

### Se você é **designer**:
1. Abra `Salvelle Identity.html` (na raiz do projeto, fora de `App/`) — referência completa da identidade.
2. Tokens estão em `ds/tokens.css`. Toda revisão visual deve passar por aqui antes de tocar componentes.
3. Para apresentar variações, duplique a tela e altere apenas tokens — mantém consistência.

### Se você é **desenvolvedor**:
1. Os HTMLs **não são código de produção**. São specs visuais executáveis.
2. Use `tokens.json` ou `tailwind.config.js` em vez de copiar CSS bruto.
3. Antes de codar qualquer tela, leia:
   - `SECURITY.md` (define autenticação, criptografia, LGPD)
   - `COMPLIANCE.md` (define o que **não pode** ser construído sem alvará/RT)
   - `SCREENS.md` (define rotas e permissões)

### Se você é **compliance / jurídico**:
- `COMPLIANCE.md` lista todos os pontos regulatórios do app.
- `SECURITY.md` cobre LGPD operacional.
- Os mockups **não substituem** validação por farmacêutico RT e advogado.

---

## Stack sugerido para implementação

Não é prescrição — apenas o caminho mais curto considerando o material atual.

- **Frontend:** React + Vite + Tailwind (config já fornecida) ou Next.js
- **Backend:** Node/Express ou NestJS · Postgres · Redis (filas, sessões)
- **Auth:** Auth0 / Clerk / Supabase Auth (MFA obrigatório — ver SECURITY.md)
- **Storage:** S3 (receitas digitalizadas) com server-side encryption
- **Observabilidade:** Sentry + log estruturado (sem PII em logs)
- **Hospedagem:** infra brasileira ou multi-região com data residency Brasil (LGPD)

---

## O que **não** está nestes mockups

Coisas que aparentam funcionar mas precisam ser construídas do zero:

- Autenticação real (signup/login são forms estáticos)
- Autorização por papel (Atendente / Manipulador / RT / Admin)
- OCR de receita (mockup mostra a UX; o reconhecimento precisa de provider)
- Leitura de balança (Toledo, Filizola — protocolo serial)
- Emissão fiscal (NFC-e / NF-e)
- Envio ao SNGPC (Anvisa — envio mensal de controlados)
- Geração de rótulo Anvisa (PDF com faixa, posologia, RT, validade)
- Integração com gateway de pagamento

Todas essas dependências estão detalhadas em `INTEGRATIONS.md`.

---

## Versionamento

Este pacote é a versão **1.0.0** da identidade Salvelle.
Mudanças futuras de tokens devem incrementar a versão em `tokens.json` e em todos os artefatos derivados.
