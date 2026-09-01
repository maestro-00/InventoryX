FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore InventoryX.sln
RUN dotnet publish InventoryX.Presentation/InventoryX.Presentation.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && adduser --disabled-password --gecos "" appuser

COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:${PORT:-8080}/health/live || exit 1

ENTRYPOINT ["dotnet", "InventoryX.Presentation.dll"]
