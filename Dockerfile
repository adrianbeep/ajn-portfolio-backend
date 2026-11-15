# syntax=docker/dockerfile:1

# Imagen multi-stage para el backend .NET 10.
#   - build   : compila y publica unicamente el proyecto Api.
#   - runtime : imagen minima con el publish, curl y el entrypoint.
#
# La configuracion sensible (Jwt__Secret, Bootstrap__Token) NUNCA se hornea en la
# imagen: se inyecta por variable de entorno en tiempo de ejecucion (docker run,
# docker compose, Render, Kubernetes...).

ARG DOTNET_VERSION=10.0

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Se copian primero los .csproj para que la capa de restore quede cacheada
# y no se invalide cada vez que cambia el codigo fuente.
COPY Api/Api.csproj Api/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
RUN dotnet restore Api/Api.csproj

COPY . .

RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

# curl se necesita para el HEALTHCHECK y para depurar el contenedor.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/docker-entrypoint.sh /app/docker-entrypoint.sh

# /data es el directorio de la base SQLite y debe ser escribible por el
# usuario 'app' (UID 1654) de la imagen oficial de .NET.
RUN chmod +x /app/docker-entrypoint.sh \
    && mkdir -p /data \
    && chown -R app:app /data /app

EXPOSE 8080

# Valores por defecto sobrescribibles en tiempo de ejecucion.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    Database__AutoMigrate=true \
    ConnectionStrings__DefaultConnection="Data Source=/data/portfolio.db"

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["/app/docker-entrypoint.sh"]
CMD ["dotnet", "Api.dll"]
