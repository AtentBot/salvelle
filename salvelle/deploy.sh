#!/bin/bash

# Script de Deploy Completo - Salvelle
# Resolve: Loop de restart, erro de permissão, adiciona Swagger e Health Check

set -e  # Para na primeira erro

echo "================================================"
echo "🚀 Deploy Salvelle - Correção Completa"
echo "================================================"
echo ""

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 1. Verificar se estamos no diretório correto
echo "📁 Verificando diretório do projeto..."
if [ ! -f "Program.cs" ]; then
    echo -e "${RED}❌ Erro: Program.cs não encontrado!${NC}"
    echo "Execute este script no diretório raiz do projeto."
    exit 1
fi
echo -e "${GREEN}✅ Diretório correto${NC}"
echo ""

# 2. Backup do Program.cs antigo
echo "💾 Fazendo backup do Program.cs antigo..."
if [ -f "Program.cs" ]; then
    cp Program.cs "Program.cs.backup.$(date +%Y%m%d_%H%M%S)"
    echo -e "${GREEN}✅ Backup criado${NC}"
fi
echo ""

# 3. Remover stack antigo
echo "🗑️  Removendo stack antigo..."
docker stack rm salvelle 2>/dev/null || echo "Stack não existia"
echo "⏳ Aguardando 20 segundos para limpeza completa..."
sleep 20
echo -e "${GREEN}✅ Stack removido${NC}"
echo ""

# 4. Limpar volumes problemáticos
echo "🧹 Limpando volumes antigos..."
docker volume rm salvelle_dataprotection 2>/dev/null || echo "Volume dataprotection não existia"
echo -e "${GREEN}✅ Volumes limpos${NC}"
echo ""

# 5. Build da nova imagem
echo "🔨 Building nova imagem Docker..."
echo "Isso pode levar alguns minutos..."
docker build -t atentbot/salvelle:latest . || {
    echo -e "${RED}❌ Erro no build da imagem${NC}"
    exit 1
}
echo -e "${GREEN}✅ Imagem construída com sucesso${NC}"
echo ""

# 6. Push da imagem
echo "📤 Enviando imagem para registry..."
docker push atentbot/salvelle:latest || {
    echo -e "${RED}❌ Erro no push da imagem${NC}"
    exit 1
}
echo -e "${GREEN}✅ Imagem enviada com sucesso${NC}"
echo ""

# 7. Deploy do novo stack
echo "🚀 Fazendo deploy do novo stack..."
docker stack deploy -c docker-compose.yml salvelle || {
    echo -e "${RED}❌ Erro no deploy${NC}"
    exit 1
}
echo -e "${GREEN}✅ Stack deployado${NC}"
echo ""

# 8. Aguardar alguns segundos
echo "⏳ Aguardando serviço inicializar (15 segundos)..."
sleep 15
echo ""

# 9. Verificar status
echo "📊 Status dos serviços:"
docker service ls | grep salvelle
echo ""

# 10. Mostrar logs iniciais
echo "📝 Primeiros logs do serviço:"
echo "================================================"
docker service logs --tail 30 salvelle_salvelle
echo "================================================"
echo ""

# 11. Verificações
echo "🔍 Verificações:"
echo ""

echo "1. Verificando Health Check..."
sleep 5
if curl -f -s https://salvelle.atentbot.com/health > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Health check respondendo!${NC}"
else
    echo -e "${YELLOW}⚠️  Health check ainda não está respondendo (pode levar alguns segundos)${NC}"
fi
echo ""

echo "2. Verificando site principal..."
if curl -f -s https://salvelle.atentbot.com > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Site principal respondendo!${NC}"
else
    echo -e "${YELLOW}⚠️  Site ainda não está respondendo (pode levar alguns segundos)${NC}"
fi
echo ""

echo "3. Verificando Swagger..."
if curl -f -s https://salvelle.atentbot.com/swagger > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Swagger respondendo!${NC}"
else
    echo -e "${YELLOW}⚠️  Swagger ainda não está respondendo (pode levar alguns segundos)${NC}"
fi
echo ""

# 12. Instruções finais
echo "================================================"
echo "✅ Deploy Concluído!"
echo "================================================"
echo ""
echo "📌 URLs importantes:"
echo "   🌐 Site:    https://salvelle.atentbot.com"
echo "   📚 Swagger: https://salvelle.atentbot.com/swagger"
echo "   🏥 Health:  https://salvelle.atentbot.com/health"
echo ""
echo "📝 Comandos úteis:"
echo "   Ver logs:       docker service logs -f salvelle_salvelle"
echo "   Ver status:     docker service ps salvelle_salvelle"
echo "   Ver services:   docker service ls"
echo ""
echo "⚠️  Se ainda houver problemas:"
echo "   1. Aguarde 30-60 segundos para estabilizar"
echo "   2. Verifique os logs: docker service logs -f salvelle_salvelle"
echo "   3. Procure por erros tipo 'Permission denied' (não deve mais aparecer!)"
echo ""
echo "🎉 Pronto! Seu serviço deve estar funcionando agora."
echo ""
