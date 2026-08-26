# Imagen para el ambiente de DEMOSTRACIÓN (Render). El despliegue real de Alianzagrafica
# es en IIS (ver app/README.md) y no usa este Dockerfile.

# ---- Etapa de compilación ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY app/src/Alianzagrafica.Evaluacion180.Web/Alianzagrafica.Evaluacion180.Web.csproj ./Alianzagrafica.Evaluacion180.Web/
RUN dotnet restore ./Alianzagrafica.Evaluacion180.Web/Alianzagrafica.Evaluacion180.Web.csproj

COPY app/src/Alianzagrafica.Evaluacion180.Web/ ./Alianzagrafica.Evaluacion180.Web/
WORKDIR /src/Alianzagrafica.Evaluacion180.Web
RUN dotnet publish -c Release -o /app/publicar --no-restore

# ---- Etapa de ejecución ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publicar .

# Carpeta para la base de datos SQLite del modo demo. Es efímera salvo que se monte
# un disco persistente de Render en /data (ver README, sección "Versión demo").
RUN mkdir -p /data

ENV ASPNETCORE_ENVIRONMENT=Demo
ENV DOTNET_gcServer=0
ENV DOTNET_EnableWriteXorExecute=0

COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

ENTRYPOINT ["/app/docker-entrypoint.sh"]
