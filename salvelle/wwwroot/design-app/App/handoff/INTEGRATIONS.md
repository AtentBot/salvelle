# Salvelle — Integrações externas

Lista do que o app **precisa conversar com o mundo de fora**. Cada integração tem alternativas — escolher na fase de RFC.

---

## 1. OCR de receita

**Mockup:** `ocr.html`
**O que faz no app:** lê foto da receita (manuscrita ou impressa) e extrai prescritor, paciente, fórmula, posologia.

**Opções:**
| Provider | Pontos fortes | Pontos fracos | Custo aprox. |
|---|---|---|---|
| **Google Document AI** | bom em manuscrito, suporta PT-BR | dado sai do Brasil (LGPD: avaliar transferência internacional) | $1.50 / 1.000 páginas |
| **Azure Form Recognizer** | região Brasil disponível | manuscrito mediano | $1.50 / 1.000 páginas |
| **AWS Textract** | região São Paulo | manuscrito fraco | $1.50 / 1.000 páginas |
| **Mindee** | API simples | dado fora do Brasil | $0.10 / página |
| Modelo próprio (fine-tune) | controle total | precisa dataset rotulado de receitas | alto inicial |

**Recomendação inicial:** Azure Form Recognizer (BR-South) + camada de validação humana obrigatória. OCR **nunca** dispensa conferência por farmacêutico.

---

## 2. Balança de precisão

**Mockup:** `weighing.html`
**O que faz no app:** lê peso real durante manipulação, valida contra prevista.

**Balanças mais comuns no mercado BR:**
- **Toledo** (linhas Prix, 9094) — protocolo serial RS-232, ASCII
- **Filizola** — protocolo proprietário documentado
- **Marte** — RS-232 ASCII

**Implementação:**
- Driver local em **agente desktop** (Windows/Linux) que escuta porta COM/USB e envia ao backend via WebSocket
- Não dá para ler balança direto do navegador (sandbox); WebSerial API resolve em Chrome moderno + HTTPS, mas não cobre todos os modelos antigos

**Sugestão:** binário Electron rodando localmente + endpoint `ws://localhost:9099/scale` que o frontend consome.

**Calibração:**
- Toda balança precisa de calibração anual por entidade acreditada (RBC/INMETRO)
- Registrar última calibração no app; alertar 30 dias antes do vencimento

---

## 3. Emissão fiscal (NFC-e / NF-e)

**Mockup:** `pdv.html`, `orders.html`
**Opções:**
| Provider | Pontos fortes | Modelo |
|---|---|---|
| **Focus NFe** | Brasil, robusto, contingência | API REST + webhook |
| **NFe.io** | bom DX | API REST |
| **eNotas** | bom suporte | API REST |
| **SEFAZ direto** | sem custo de provider | mais trabalho técnico, certificado A1, contingência manual |

**Itens obrigatórios:**
- Certificado digital A1 da farmácia (upload + criptografia em repouso)
- Modo contingência offline (gerar XML, validar quando voltar online)
- Cancelamento até 30min após emissão
- Inutilização de numeração (quando a NFC-e fica presa)

---

## 4. SNGPC (Anvisa)

**Mockup:** indireto, em todas as telas que tocam controlados (`manipulation.html`, `pdv.html`)
**O que faz:** envio mensal das movimentações de medicamentos controlados.

- Webservice SOAP Anvisa (URL atualizada periodicamente — checar antes de implementar)
- Migração para SNGPC 2.0 em curso — validar status na Anvisa
- Não há provider SaaS oficial; algumas casas oferecem (PharmaGed, Nimbi) mas é melhor implementar direto

---

## 5. Pagamento (PDV)

**Opções de gateway:**
| Provider | Cartão presente (TEF/POS) | PIX | Recorrência |
|---|---|---|---|
| **Stone** | sim (POS Stone) | sim | sim |
| **Cielo** | sim (LIO, Verifone) | sim | sim |
| **PagSeguro / PagBank** | sim | sim | sim |
| **Mercado Pago** | sim (Point) | sim | sim |
| **Pagar.me** | API forte, white-label | sim | sim |

**Para o PDV físico:** integração com TEF (Transferência Eletrônica de Fundos) é o padrão.
- TEF discado (legado, desencorajado)
- TEF dedicado (Stone, Cielo, Verifone) — recomendado

**PIX:** geração de QR estático/dinâmico via API do gateway. Webhook para confirmar liquidação antes de finalizar venda.

---

## 6. CRM / Notificação ao paciente

- WhatsApp Business API (provider: Zenvia, Twilio, Meta direct)
- E-mail transacional: Resend, SendGrid, AWS SES
- SMS: Twilio, Zenvia

Casos de uso:
- "Sua fórmula está pronta para retirada"
- "Receita vencendo em 3 dias — quer que renovemos?"
- Pesquisa de NPS após retirada

LGPD: opt-in explícito, opt-out em 1 clique.

---

## 7. Validação de profissional (CRF / CRM)

**Mockups:** `employees.html`, `ocr.html`
**O que faz:** confirma se CRF do RT está ativo, e se CRM do prescritor existe.

**Estado atual:**
- CFF não expõe API pública para consulta de CRF — fazer manual ou scraping (com cuidado jurídico)
- CRMs estaduais variam — alguns expõem consulta web; nenhum tem API REST pública confiável

**Mitigação no MVP:** validação manual no cadastro + alerta para revalidar anualmente.

---

## 8. Estoque & fornecedores

**Mockup:** `purchases.html`
- Catálogo de matérias-primas: começar com cadastro próprio + import CSV
- Fornecedores grandes (Galena, Fagron, Pharma Nostra): negociar EDI/API caso a caso
- Código DCB (Denominação Comum Brasileira) para princípios ativos — base pública Anvisa, atualização anual

---

## 9. Observabilidade & Suporte

- **Sentry** (frontend + backend) para erros
- **PostHog** ou **Mixpanel** para produto (com cuidado LGPD — anonimizar)
- **Crisp** ou **Intercom** para chat de suporte
- **Status page** pública (`status.salvelle.com`) — Better Stack, Statuspage

---

## 10. Resumo de dependências externas

| # | Integração | Bloqueia go-live? |
|---|---|---|
| 1 | OCR | Não (pode entrar fórmula manual) |
| 2 | Balança | Sim para manipulação |
| 3 | NFC-e | Sim para PDV |
| 4 | SNGPC | Sim se vender controlados |
| 5 | Pagamento | Sim para PDV |
| 6 | WhatsApp | Não |
| 7 | Validação CRF/CRM | Não (manual no MVP) |
| 8 | Fornecedores EDI | Não |
| 9 | Observabilidade | Sim (operacional) |
