# Guia de Deploy — Stack `salvelle` (Portainer / Docker Swarm)

Guia para o agente colega **criar do zero** o deploy da nova marca **Salvelle** no
Portainer. É a mesma plataforma do `orcpharm`, porém publicando a partir de um repo e
uma imagem próprios.

| Item | Valor |
|------|-------|
| Stack (Swarm) | `salvelle` → serviço `salvelle_salvelle` |
| Repositório | `https://github.com/AtentBot/salvelle` |
| Imagem Docker Hub | `atentbot/salvelle` (tags `:latest` e `:sha-<short>`) |
| Portainer | `https://portainer.atentbot.com` (Swarm, endpoint id **1**) |
| Domínio prod | `salvelle.atentbot.com` (ajuste se for usar `salvelle.com.br`) |
| Rede Traefik | `traefik_public` (external) |

> ⚠️ **Nunca** cole senhas neste arquivo — ele é versionado no Git. Todas as
> credenciais (Portainer admin, Docker Hub PAT, GitHub PAT) estão em texto puro em
> **`/mnt/e/AtentBot/doc.txt`**. Leia de lá em runtime.

> ℹ️ Já existe um stack legado `salvelle_orcpharm` (imagem antiga `atentbot/orcpharm`).
> **Não** é este. Estamos criando um stack novo cujo serviço será `salvelle_salvelle`
> apontando para `atentbot/salvelle`. Se o nome de stack `salvelle` já estiver ocupado
> pelo legado, remova-o antes (`docker stack rm salvelle`) ou escolha outro nome.

---

## 0. Pré-requisitos

- Imagem `atentbot/salvelle:latest` **já publicada** no Docker Hub. Se ainda não
  existir, publique antes (seção 4 — CI/CD, ou build manual). O Portainer não builda a
  imagem; ele só faz `pull` + `service update`.
- Acesso ao Portainer (`admin` / senha em `doc.txt`).
- Rede overlay `traefik_public` existente (é a mesma que o orcpharm usa — externa).

---

## 1. `docker-compose.yml` do stack

Adaptado do `orcpharm/docker-compose.yml`. Trocamos imagem, nome de serviço, volumes,
domínio e labels do Traefik para `salvelle`.

```yaml
version: '3.8'
services:
  salvelle:
    image: atentbot/salvelle:latest
    volumes:
      - salvelle-keys:/app/.keys
      - salvelle-uploads:/app/uploads
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_HTTPS_PORT=8443
      - ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
    deploy:
      replicas: 2
      update_config:
        parallelism: 1
        delay: 10s
        order: start-first
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
        window: 120s
      labels:
        - "traefik.enable=true"

        # Router HTTPS
        - "traefik.http.routers.salvelle.rule=Host(`salvelle.atentbot.com`)"
        - "traefik.http.routers.salvelle.entrypoints=websecure"
        - "traefik.http.routers.salvelle.tls=true"
        - "traefik.http.routers.salvelle.tls.certresolver=le"

        # Service
        - "traefik.http.services.salvelle.loadbalancer.server.port=8080"
        - "traefik.docker.network=traefik_public"

        # Health check
        - "traefik.http.services.salvelle.loadbalancer.healthcheck.path=/health"
        - "traefik.http.services.salvelle.loadbalancer.healthcheck.interval=10s"
        - "traefik.http.services.salvelle.loadbalancer.healthcheck.timeout=3s"
        - "traefik.http.services.salvelle.loadbalancer.healthcheck.scheme=http"

        # Sticky sessions (2 réplicas)
        - "traefik.http.services.salvelle.loadbalancer.sticky.cookie=true"
        - "traefik.http.services.salvelle.loadbalancer.sticky.cookie.name=salvelle_sticky"
        - "traefik.http.services.salvelle.loadbalancer.sticky.cookie.secure=true"
        - "traefik.http.services.salvelle.loadbalancer.sticky.cookie.httpOnly=true"

    networks:
      - traefik_public

volumes:
  salvelle-keys:
    driver: local
  salvelle-uploads:
    driver: local

networks:
  traefik_public:
    external: true
```

> **DNS:** aponte `salvelle.atentbot.com` (A/CNAME) para o host Hetzner
> `49.13.161.114` antes de subir, senão o Let's Encrypt (`certresolver=le`) não emite o
> certificado.

---

## 2. Criar o stack via UI do Portainer (recomendado para a 1ª vez)

1. Login em `https://portainer.atentbot.com` (`admin` / senha em `doc.txt`).
2. **Stacks → + Add stack**.
3. Nome: `salvelle` (minúsculas; vira prefixo do serviço → `salvelle_salvelle`).
4. Build method:
   - **Web editor**: cole o compose da seção 1. Simples, mas o conteúdo não fica
     sincronizado com o Git.
   - **Repository** (preferível p/ GitOps): URL
     `https://github.com/AtentBot/salvelle`, branch `main`, compose path
     `docker-compose.yml`. Repo privado → marque **Authentication** e informe o
     GitHub PAT (`doc.txt`). Deixe **GitOps updates** ligado se quiser polling
     automático.
5. **Deploy the stack**. O Portainer faz `pull` de `atentbot/salvelle:latest` e cria o
   serviço `salvelle_salvelle` com 2 réplicas.
6. Guarde o **stack id** que aparece na URL — será usado no webhook/API.

---

## 3. Criar o stack via API do Portainer (headless / automação)

Útil para o agente rodar sem UI. Fluxo Swarm:

```bash
PORTAINER="https://portainer.atentbot.com"

# 1) Autenticar → JWT (usuário/senha em /mnt/e/AtentBot/doc.txt)
JWT=$(curl -s -X POST "$PORTAINER/api/auth" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"<SENHA_DO_DOC_TXT>"}' | jq -r .jwt)

# 2) Pegar o Swarm ID do endpoint 1 (necessário p/ criar stack Swarm)
SWARM_ID=$(curl -s "$PORTAINER/api/endpoints/1/docker/swarm" \
  -H "Authorization: Bearer $JWT" | jq -r .ID)

# 3) Criar o stack a partir de string (compose inline)
#    'type=1' = Swarm, 'method=string'
curl -s -X POST \
  "$PORTAINER/api/stacks/create/swarm/string?endpointId=1" \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d "$(jq -n --arg swarm "$SWARM_ID" --rawfile compose docker-compose.yml \
        '{Name:"salvelle", SwarmID:$swarm, StackFileContent:$compose}')"
```

> A imagem precisa estar acessível. Se o repo `atentbot/salvelle` for **privado** no
> Docker Hub, cadastre a credencial do registry no Portainer (**Registries**) antes, ou
> deixe público.

---

## 4. CI/CD — build automático + redeploy (GitHub Actions)

Espelhe o pipeline do orcpharm (`.github/workflows/deploy.yml`) no repo
`AtentBot/salvelle`. Mudanças: `IMAGE`, contexto do build e o webhook do novo stack.

```yaml
name: Build & Deploy

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  IMAGE: atentbot/salvelle

jobs:
  build-and-push:
    name: Build & push image
    runs-on: ubuntu-latest
    outputs:
      sha_short: ${{ steps.vars.outputs.sha_short }}
    steps:
      - uses: actions/checkout@v4
      - id: vars
        run: echo "sha_short=$(git rev-parse --short HEAD)" >> "$GITHUB_OUTPUT"
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: .              # ajuste se o Dockerfile estiver em subpasta
          push: true
          tags: |
            ${{ env.IMAGE }}:latest
            ${{ env.IMAGE }}:sha-${{ steps.vars.outputs.sha_short }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    name: Trigger Portainer redeploy
    needs: build-and-push
    runs-on: ubuntu-latest
    steps:
      - run: |
          curl --fail --silent --show-error --retry 3 --retry-delay 5 \
            -X POST "${{ secrets.PORTAINER_WEBHOOK_URL }}"
```

**Secrets a cadastrar** em `AtentBot/salvelle` → *Settings → Secrets → Actions*:

- `DOCKERHUB_USERNAME` = `atentbot`
- `DOCKERHUB_TOKEN` = PAT do Docker Hub (`doc.txt`)
- `PORTAINER_WEBHOOK_URL` = webhook do serviço (passo abaixo)

**Habilitar o webhook do serviço:** no Portainer, abra o serviço
`salvelle_salvelle` → **Service webhook** → *Create a webhook*. Copie a URL
(`https://portainer.atentbot.com/api/webhooks/<uuid>`) e cole no secret
`PORTAINER_WEBHOOK_URL`. Cada `POST` nessa URL força `pull` + `ForceUpdate`.

---

## 5. Verificação

```bash
docker service ls | grep salvelle
docker service ps salvelle_salvelle           # tasks em running no novo digest
docker service logs -f salvelle_salvelle
curl -f https://salvelle.atentbot.com/health  # deve responder 200
```

---

## 6. Fallback — forçar redeploy quando o webhook falha

O webhook do Portainer é intermitente a partir dos IPs do GitHub Actions
(`curl: (28) SSL connection timeout`). Se o job `deploy` falhar mas a imagem já subiu no
Docker Hub, force via API (mesmo procedimento validado para o orcpharm):

1. `POST /api/auth` → `jwt`.
2. Descubra o service id: `GET /api/endpoints/1/docker/services` → filtre por
   `Spec.Name == "salvelle_salvelle"`.
3. `GET /api/endpoints/1/docker/services/<ID>` → guarde `Spec` + `Version.Index`.
4. Ajuste `Spec.TaskTemplate.ContainerSpec.Image` para o novo digest
   (`atentbot/salvelle:latest@sha256:<novo>` — digest em
   `hub.docker.com/v2/repositories/atentbot/salvelle/tags/latest`) e **incremente**
   `Spec.TaskTemplate.ForceUpdate`.
5. `POST /api/endpoints/1/docker/services/<ID>/update?version=<Index>` com o `Spec` →
   HTTP 200 `{"Warnings":null}`.
6. Confirme convergência em `/api/endpoints/1/docker/tasks` (task nova em
   `running/started`, antigas em `shutdown`).

Ver o guia equivalente do orcpharm na memória `reference_manual_redeploy` e a infra em
`reference_atentbot_infra`.

---

## Checklist rápido

- [ ] DNS `salvelle.atentbot.com` → `49.13.161.114`
- [ ] Imagem `atentbot/salvelle:latest` publicada no Docker Hub
- [ ] Stack `salvelle` criado (serviço `salvelle_salvelle`, 2 réplicas)
- [ ] Webhook do serviço criado e salvo em `PORTAINER_WEBHOOK_URL`
- [ ] Secrets do Docker Hub no repo `AtentBot/salvelle`
- [ ] `/health` responde 200 via HTTPS com cert Let's Encrypt válido
