#!/bin/sh
# Render inyecta la variable PORT en tiempo de ejecución (no se conoce al construir la
# imagen) — Kestrel debe escuchar exactamente ahí, o Render no podrá enrutar tráfico
# al contenedor. Si PORT no está definida (por ejemplo al correr el contenedor localmente
# con `docker run`), se usa 8080 por defecto.
set -e
export ASPNETCORE_URLS="http://+:${PORT:-8080}"
exec dotnet Alianzagrafica.Evaluacion180.Web.dll
