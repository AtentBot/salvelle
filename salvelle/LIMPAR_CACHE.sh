#!/bin/bash

echo "🧹 LIMPANDO CACHE COMPLETO..."
echo ""

# Fechar todos os processos dotnet
echo "1. Matando processos dotnet..."
pkill -9 dotnet 2>/dev/null || true

# Limpar bin e obj
echo "2. Removendo bin/ e obj/..."
find . -type d -name "bin" -o -name "obj" | xargs rm -rf

# Limpar NuGet cache
echo "3. Limpando NuGet cache..."
dotnet nuget locals all --clear

# Limpar cache temporário
echo "4. Limpando /tmp..."
rm -rf /tmp/NuGetScratch* 2>/dev/null || true
rm -rf ~/.nuget/packages/.tools 2>/dev/null || true

# Rebuild
echo ""
echo "🔨 REBUILD COMPLETO..."
dotnet restore --no-cache --force
dotnet clean
dotnet build --no-incremental

echo ""
echo "✅ PRONTO!"
