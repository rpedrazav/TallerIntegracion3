# ==============================================================================
# Dockerfile — TenantIdentityService (.NET 8)
# Multi-stage build: build separado del runtime para imagen final liviana
# ==============================================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el .csproj primero para aprovechar el cache de capas de Docker
COPY TenantIdentityService.csproj ./
RUN dotnet restore

# Copia el resto del código fuente y publica en modo Release
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copia solo los artefactos de build
COPY --from=build /app/publish .

# Puerto en el que escucha la app dentro del contenedor
EXPOSE 8080

# Variables de entorno de ASP.NET para producción en contenedor
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TenantIdentityService.dll"]
