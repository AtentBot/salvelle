# Salvelle — Segurança & LGPD

> **Não é opcional.** Sistema de farmácia trata dado de saúde — categoria sensível pela LGPD (art. 5º, II e art. 11). Vazamento gera multa **+ ANPD + Procon + ação coletiva**.

## 1. Classificação de dados

| Tipo | Exemplo no app | Sensibilidade | Onde aparece |
|---|---|---|---|
| Identificação | Nome, CPF, telefone, endereço | LGPD — pessoal | clientes, pedidos, employees |
| **Saúde** | Receita, fórmula, posologia, prescritor | **LGPD sensível (art. 11)** | orders, manipulation, ocr |
| Biométrico | Foto da receita digitalizada | LGPD sensível | ocr |
| Financeiro | Forma de pagamento, valor | LGPD pessoal + fiscal | pdv, orders |
| Profissional | CRF, CRM, escala, salário | LGPD pessoal | employees |
| Controlado | Quantidade dispensada de Lista A/B/C | **LGPD sensível + Anvisa** | manipulation, pdv |

**Regra:** todo campo classificado como sensível **exige** consentimento específico (art. 11, §1º) **e** justificativa de tratamento (art. 7º).

---

## 2. Autenticação

### Mínimo aceitável
- Senha: ≥12 caracteres, validação contra HIBP (haveibeenpwned k-anonymity API)
- **MFA obrigatório** para todos os usuários (TOTP + opção SMS de fallback)
- Bloqueio progressivo após 5 tentativas falhadas (1min → 5min → 15min)
- Sessão: idle timeout 30min, absolute timeout 12h
- Refresh token rotation; access token JWT de 15min

### Multi-tenancy (cada farmácia = um tenant)
- IDs de tenant em **toda** query (SELECT `WHERE tenant_id = ?` sem exceção)
- Row-level security no Postgres como segunda camada
- Isolamento de S3 por prefixo `tenants/{tenantId}/...`

### Recuperação de senha
- Token de uso único, expira em 30min, vinculado ao IP de solicitação
- E-mail + SMS (fator duplo) para reset

### Papéis (RBAC mínimo)
| Papel | Pode ver | Pode editar | Pode dispensar controlados |
|---|---|---|---|
| Atendente | pedidos, clientes, PDV | criar pedidos | não |
| Manipulador | manipulação, pesagem | atualizar status | não |
| **Farmacêutico RT** | tudo | tudo operacional | **sim, com biometria/PIN** |
| Admin | tudo + employees + billing | tudo | depende (se também for RT) |

Bloqueio físico de tela após 5min sem atividade no PDV (operador caixa).

---

## 3. Criptografia

- **Em trânsito:** TLS 1.3 obrigatório. HSTS preload. Rejeitar TLS <1.2.
- **Em repouso:**
  - Postgres: TDE ou EBS encryption (AWS KMS gerenciado pelo cliente quando possível)
  - S3 (receitas): SSE-KMS, bucket privado, signed URLs ≤5min
  - Backups: criptografados com chave separada, retenção 90 dias
- **Pseudonimização:** CPF, CNS, e-mail e telefone armazenados com hash separado para busca + valor cifrado para exibição (ver "tokenization layer")

---

## 4. Auditoria

Toda ação que toca dado sensível **deve** gerar registro imutável:

```
audit_log (
  id, tenant_id, actor_user_id, action, target_type, target_id,
  ip, user_agent, timestamp, payload_hash, prev_log_hash
)
```

Hash encadeado (`prev_log_hash` = SHA256 do registro anterior) impede alteração silenciosa. Append-only no nível do banco (REVOKE UPDATE, DELETE).

Eventos obrigatórios:
- Login / logout / falha de autenticação
- Criação / edição / exclusão de pedido
- Dispensa de controlado (com biometria/PIN do RT)
- Exportação de relatório com dados pessoais
- Mudança de papel de usuário
- Acesso à receita digitalizada (visualização inclui)

Retenção: **5 anos** (RDC 44/2009 — receituário) + **20 anos** para dispensas de controlados.

---

## 5. LGPD — direitos do titular

O app deve expor (em `/configuracoes/privacidade` para o cliente final, e via portal interno para o atendente):

- **Confirmação** de tratamento (art. 18, I)
- **Acesso** aos dados (art. 18, II) — exportação JSON/PDF
- **Correção** (art. 18, III)
- **Anonimização / eliminação** (art. 18, VI) — respeitando obrigações legais (RDC 44 prevalece sobre LGPD para receituário)
- **Portabilidade** (art. 18, V)
- **Revogação de consentimento** (art. 8º, §5º)

SLA de resposta: 15 dias (LGPD prevê "imediato"; ANPD interpreta como ≤15 dias úteis).

DPO obrigatório (art. 41) — registrar no rodapé e no `/privacidade`.

---

## 6. Backups & DR

- Backup full diário + incremental a cada 6h
- Retenção 90 dias quente, 1 ano frio (Glacier/equivalente)
- Restore drill **trimestral** documentado
- RPO ≤ 6h · RTO ≤ 4h

---

## 7. Hardening operacional

- Secrets em vault (AWS Secrets Manager, Doppler, Vault) — **nunca** em `.env` versionado
- Dependências: Snyk/Dependabot + revisão semanal
- WAF + rate limit por endpoint (login: 10/min/IP; API geral: 600/min/usuário)
- CSP estrita, SameSite=Strict em cookies de sessão
- Pen-test anual + bug bounty quando faturamento permitir
- SOC2 / ISO 27001: meta para ano 2

---

## 8. Plano de incidente

Documento separado, mas mínimo:
1. Detecção (alertas Sentry/CloudWatch)
2. Contenção (revogar tokens, isolar tenant)
3. **Notificação à ANPD em 72h** (LGPD art. 48) se houver vazamento
4. Notificação aos titulares afetados em linguagem clara
5. Post-mortem público em 30 dias

---

## 9. Itens que precisam virar tickets antes do go-live

- [ ] Auth provider escolhido + MFA configurado
- [ ] RLS no Postgres por tenant
- [ ] Audit log com hash chain
- [ ] DPO contratado e contato publicado
- [ ] Política de privacidade revisada por advogado
- [ ] Termo de uso revisado por advogado
- [ ] Inventário ROPA (Registro das Operações de Tratamento) preenchido
- [ ] Backup + restore drill testado
- [ ] Pen-test contratado
