FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["salvelle.csproj", "./"]
RUN dotnet restore "salvelle.csproj"
COPY . .
RUN dotnet build "salvelle.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "salvelle.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Instalar certificados e dependências se necessário
RUN apt-get update && apt-get install -y \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Variáveis de ambiente
# Default = Production (aplica migrations via MigrateAsync). O ambiente de
# desenvolvimento sobrescreve para Development em docker-compose.dev.yml.
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "salvelle.dll"]