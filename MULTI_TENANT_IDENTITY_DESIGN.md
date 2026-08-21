# Design — Identidade multi-tenant (um CPF, vários estabelecimentos)

> Status: **proposta / aguardando confirmação de decisões de produto**
> Objetivo: permitir que a mesma pessoa (CPF) esteja vinculada a mais de um estabelecimento e gerencie/troque de contexto (multi-tenant), sem duplicar credencial.

## 1. Problema atual

- Login do employee é por sessão: `sessão → employee → employee.EstablishmentId` (uma loja fixa).
  - `AuthService.LoginAsync` acha o employee por CPF/WhatsApp **global** (`FirstOrDefault`) — [Service/Auth/AuthService.cs:28](salvelle/Service/Auth/AuthService.cs#L28).
  - `EmployeeAuthMiddleware` seta `EstablishmentId` a partir de `session.Employee.EstablishmentId` — [Middleware/EmployeeAuthMiddleware.cs:259](salvelle/Middleware/EmployeeAuthMiddleware.cs#L259).
- CPF é tratado como **login global único** (checado em código no signup — [Service/SignupService.cs:317](salvelle/Service/SignupService.cs#L317)); não há constraint no banco.
- Credencial **duplicada**: senha gravada no `Establishment` e **copiada** pro employee — [Service/SignupService.cs:362](salvelle/Service/SignupService.cs#L362).
- Consequência: a mesma pessoa **não pode** ser dona/funcionária de duas lojas.

## 2. Alvo

- **Identidade** (credencial única por CPF) + **vínculos** (memberships) com N estabelecimentos.
- Login valida a identidade; se houver >1 loja, **seletor de loja**; a sessão guarda `CurrentEstablishmentId` **trocável** (mesmo padrão que o lado do **cliente** já usa: `CustomerSessions.CurrentEstablishmentId` / `SetCurrentEstablishmentAsync`).

## 3. Modelo de dados

### Nova tabela `identities`
| coluna | tipo | nota |
|---|---|---|
| Id | uuid PK | |
| Cpf | text **UNIQUE** | chave de login |
| WhatsApp | text | login alternativo / 2FA |
| Email | text | |
| PasswordHash / PasswordAlgorithm / PasswordCreatedAt | | credencial **única** |
| FailedLoginAttempts / LockedUntil | | lockout na identidade |
| TwoFactorEnabled / ... | | 2FA na identidade |
| CreatedAt / UpdatedAt | | |

### `employees` vira **vínculo** (membership)
- **Ganha** `IdentityId` (FK → identities).
- **Perde** (deprecar, não dropar de imediato): `PasswordHash`, `PasswordAlgorithm`, lockout.
- Mantém: `EstablishmentId`, `JobPositionId`, `FullName`, `Status`, contrato, etc.
- Regra de unicidade: **(IdentityId, EstablishmentId) único** (uma pessoa não repete na mesma loja).

### `employee_sessions`
- **Ganha** `CurrentEstablishmentId` (uuid) — a loja ativa da sessão.
- Passa a referenciar a **identidade** (via employee atual ou direto `IdentityId`).

## 4. Mudanças de auth

- **Login** ([AuthService.LoginAsync](salvelle/Service/Auth/AuthService.cs#L26)):
  1. Achar **identidade** por CPF/WhatsApp.
  2. Validar senha / lockout / 2FA **na identidade**.
  3. Carregar vínculos com estabelecimentos **ativos**.
  4. 0 → erro; 1 → sessão com aquela loja; >1 → retorna a lista, front mostra **seletor**; sessão criada com `CurrentEstablishmentId` escolhido.
- **Middleware** ([EmployeeAuthMiddleware](salvelle/Middleware/EmployeeAuthMiddleware.cs#L253)): `EstablishmentId` e `EmployeeId` passam a vir de `session.CurrentEstablishmentId` + o vínculo daquela loja.
- **Novo endpoint** `POST /api/auth/trocar-estabelecimento` (valida vínculo, atualiza `session.CurrentEstablishmentId`).
- **Password reset / 2FA**: repontar de employee → **identidade**.
- `LoginResponseDto`: incluir `Establishments[]` (lista de lojas do usuário) quando >1.

## 5. Mudanças no signup ([SignupService](salvelle/Service/SignupService.cs))

- `register`: **parar de gravar senha no Establishment**.
- `complete-profile`:
  - CPF **já tem identidade** → **vincular** (confirmar identidade com a senha existente) e criar o membership; **não** exigir senha nova.
  - CPF **novo** → criar identidade + senha + membership.
- Check de unicidade de CPF: de **global** para **(CPF, EstablishmentId)**.

## 6. Migração de dados (EF Migration + backfill)

1. Criar tabela `identities`.
2. Para cada **CPF distinto** em `employees`: criar 1 `identity` (mover PasswordHash/algoritmo/lockout do employee "canônico" — ex.: o mais antigo).
3. Preencher `employees.IdentityId`.
4. Backfill `employee_sessions.CurrentEstablishmentId = employee.EstablishmentId`.
5. Criar índice único `(IdentityId, EstablishmentId)`.
6. (Fase 2, depois de estável) dropar colunas de senha em `employees`.

## 7. Decisões de produto (PENDENTES — confirmar)

1. **Pessoa existente cadastra nova loja:** reusa a conta confirmando a senha atual? _(recomendado: **sim**)_
2. **Gestão de vínculos:** um owner pode adicionar um CPF já existente como funcionário de outra loja? _(recomendado: **sim**, com o consentimento/senha da pessoa no primeiro acesso)_

## 8. Sequência de implementação (stages)

1. **Modelo + migration** (tabela `identities`, FK em employees, `CurrentEstablishmentId`, backfill).
2. **Auth**: login por identidade + seletor + sessão com loja ativa + `trocar-estabelecimento`.
3. **Signup**: register sem senha no Establishment; complete-profile cria/vincula identidade.
4. **Password reset / 2FA** repontados.
5. **Testes** (login 1 loja, login N lojas + troca, signup nova pessoa, signup pessoa existente).

## 9. Ambiente / rollout (OBRIGATÓRIO)

- **Branch nova** `feature/multi-tenant-identity` (a atual tem mudanças soltas).
- **DB local isolado** (container `salvelle_test_db` ou DB novo) — **NUNCA** rodar a migração no `pg.atentbot.com/orcpharm` (compartilhado).
- Pré-requisito atual **não atendido**: Docker está fora / não há Postgres local em 5432/5433. Subir o Postgres local antes de qualquer `dotnet ef database update`.
- Deploy real só via git/produção, com a migração aplicada de forma controlada (ver [[salvelle-deploy-status]]).

## 10. Riscos

- Refactor no **coração da auth** — cobrir com testes antes de mexer em produção.
- Migração precisa lidar com CPFs já duplicados (demo/teste) escolhendo 1 credencial canônica.
- Sessões ativas durante o deploy (invalidar ou backfillar `CurrentEstablishmentId`).
