# Salvelle — Conformidade Regulatória

> **Aviso:** este documento é guia para implementação técnica. Não substitui validação por **farmacêutico responsável técnico (RT)** e **advogado** especialista em direito sanitário/farmacêutico.

## 1. Marco regulatório aplicável

| Norma | Órgão | Escopo no app |
|---|---|---|
| **RDC 67/2007** | Anvisa | Boas práticas de manipulação magistral — define rótulo, ordem de fabricação, controle de qualidade |
| **RDC 44/2009** | Anvisa | Boas práticas farmacêuticas — receituário, dispensação, atenção farmacêutica |
| **Portaria 344/1998** | Ministério da Saúde | Substâncias e medicamentos sujeitos a controle especial (Lista A1, A2, A3, B1, B2, C1, C2, C3, C4, C5) |
| **RDC 22/2014** | Anvisa | SNGPC — Sistema Nacional de Gerenciamento de Produtos Controlados |
| **RDC 471/2021** | Anvisa | Atualização da rotulagem de medicamentos manipulados |
| **LGPD 13.709/2018** | ANPD | Tratamento de dado pessoal de saúde (sensível) |
| **CFF Resolução 357/2001** | Conselho Federal de Farmácia | Boas práticas — atribui responsabilidade ao RT |
| **NF-e / NFC-e** | SEFAZ estadual | Emissão fiscal eletrônica |

---

## 2. RDC 67/2007 — Manipulação magistral

### Ordem de Manipulação (mockup: `manipulation.html`)

Toda fórmula manipulada **deve** gerar uma **Ordem de Manipulação** numerada, com:

- [ ] Número sequencial único por farmácia
- [ ] Data e hora de início
- [ ] Identificação do prescritor (nome + CRM/CRO/CRMV + UF)
- [ ] Identificação do paciente (nome completo)
- [ ] Identificação do(s) farmacêutico(s) responsável(eis)
- [ ] Forma farmacêutica e via de administração
- [ ] Composição qualitativa e quantitativa **completa** (princípio ativo + veículo + excipiente)
- [ ] Quantidade total manipulada
- [ ] Lote da matéria-prima utilizada (rastreável até o fornecedor)
- [ ] Pesagem real **vs.** prevista (com tolerância)
- [ ] Validação visual / organoléptica
- [ ] Prazo de validade da fórmula manipulada
- [ ] Assinatura digital do RT (certificado ICP-Brasil ou equivalente reconhecido pelo CFF)

**Conservação:** ordens devem ser arquivadas por **5 anos** (RDC 44/2009 art. 64).

### Rotulagem (RDC 471/2021)

Rótulo do manipulado **deve** conter:

- Nome e endereço completo da farmácia
- CNPJ
- Nome e CRF do farmacêutico responsável técnico
- Nome do paciente
- Composição qualitativa e quantitativa
- Posologia (transcrita da receita)
- Via de administração
- Quantidade total
- Data de manipulação
- Prazo de validade
- Número da ordem de manipulação
- **Faixa colorida** para controlados:
  - Vermelha — entorpecentes (Lista A1, A2, A3)
  - Preta — psicotrópicos (Lista B1, B2)
  - Sem faixa para Lista C, mas com escrita "VENDA SOB PRESCRIÇÃO MÉDICA"

Tokens de cor já definidos em `tokens.css`:
```css
--controlled-red: #B91C1C;    /* faixa entorpecentes */
--controlled-black: #111111;  /* faixa psicotrópicos */
```

Funcionalidade necessária: **gerar PDF de rótulo**. Provider sugerido: server-side com `pdfmake` ou `puppeteer`.

---

## 3. RDC 44/2009 — Dispensação e receituário

### Receituário simples
- Validade: 30 dias da prescrição
- Arquivamento: 5 anos
- Pode ser dispensado por atendente

### Receituário de controle especial (Lista C1, C4, C5)
- Validade: 30 dias
- 2 vias (uma fica retida na farmácia)
- Dispensação **somente** pelo farmacêutico

### Notificação de receita (Lista A, B, C2, C3)
- Talonário numerado emitido pela Vigilância Sanitária estadual
- Notificação **A** (amarela) — entorpecentes
- Notificação **B** (azul) — psicotrópicos
- Notificação **B2** (azul, retinóide sistêmico) e **C** (branca) — outros
- Validade: 30 dias (15 para retinóides)
- Quantidade máxima por receita varia por substância
- **Retenção obrigatória** da notificação na farmácia

No app, ao dispensar controlado:
- Capturar imagem da notificação (anexada à venda)
- Validar limite de quantidade automaticamente
- Bloquear dispensa se receita expirada
- Exigir PIN/biometria do RT (`SECURITY.md` §2)

---

## 4. SNGPC (RDC 22/2014)

Sistema Nacional de Gerenciamento de Produtos Controlados.

**Obrigações do app:**
- Registrar **todas** as movimentações de controlados (entrada, saída, perda, transferência)
- Gerar arquivo XML mensal no formato definido pela Anvisa
- Transmitir até o dia 15 do mês subsequente
- Manter logs de envio e protocolo de recebimento

**Implementação:**
- Job mensal agendado (cron) gera o XML
- Validação contra o XSD da Anvisa antes de envio
- Endpoint Anvisa: webservice oficial (verificar URL atualizada — muda eventualmente)
- Em caso de falha de envio: alerta crítico ao RT

**Pendência regulatória:** Anvisa está migrando para o **SNGPC 2.0** (em transição). Verificar estado atual antes de implementar.

---

## 5. Fiscal

### NFC-e (Nota Fiscal de Consumidor Eletrônica)
- Emissão obrigatória em vendas presenciais (PDV)
- Certificado digital A1 ou A3 da farmácia
- Contingência offline obrigatória
- Provider sugerido: Focus NFe, NFe.io, ou integração direta com SEFAZ

### NF-e (Nota Fiscal Eletrônica)
- Vendas a CNPJ ou entrega
- Mesmas regras

### Tributação de manipulados
- ISS (município) sobre serviço de manipulação
- ICMS (estado) sobre venda de mercadoria
- Regime tributário define alíquotas (Simples / Lucro Presumido / Real)
- Consultar contador antes de configurar

---

## 6. Responsabilidade técnica (RT)

- Toda farmácia precisa ter **farmacêutico RT** registrado no CRF
- O app **não pode** funcionar sem RT cadastrado e ativo
- Trocas de RT: workflow de transição (RT atual valida saída + RT novo valida entrada)
- Horário de presença: registrar entrada/saída do RT (Lei Federal 13.021/2014)

Mockup `employees.html` deve permitir:
- Cadastro de farmacêutico com CRF + UF + validade do registro
- Marcação de RT principal e RTs substitutos
- Validação automática contra a base do CRF (manual ou via integração quando disponível)

---

## 7. Arquivamento e auditoria

| Documento | Prazo |
|---|---|
| Receita comum | 5 anos |
| Notificação de receita (controlados) | 5 anos |
| Ordem de Manipulação | 5 anos |
| Livro de registro de controlados | **20 anos** |
| Comprovantes fiscais | 5 anos (CTN art. 173) |
| Cadastro de paciente | enquanto vínculo + 5 anos |

---

## 8. Itens regulatórios pendentes (virar tickets)

- [ ] Validação por farmacêutico RT do fluxo de Ordem de Manipulação
- [ ] Validação jurídica do Termo de Uso e Política de Privacidade
- [ ] Geração de rótulo Anvisa (PDF) com todos os campos da RDC 471/2021
- [ ] Workflow de notificação de receita para controlados
- [ ] Geração e envio mensal SNGPC
- [ ] Integração NFC-e + contingência offline
- [ ] Bloqueio de operação sem RT ativo
- [ ] Plano de retenção de dados (LGPD vs. RDC 44 — RDC prevalece para receituário)
