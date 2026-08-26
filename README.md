# Evaluación de Desempeño 180° — Alianzagrafica

Repositorio con los dos entregables técnicos del proyecto:

- `app/` — la aplicación web ASP.NET Core 8 (MVC). Para el **despliegue real** en el IIS de
  Alianzagrafica contra SQL Server, sigue **`app/README.md`** — esa guía no cambia por nada
  de lo que hay en este archivo.
- `sql/` — el script T-SQL (`01_esquema_y_datos_ficticios.sql`) que crea el esquema completo
  en SQL Server, con una tabla de empleados ficticia mientras se conecta Novasoft.

Este archivo cubre exclusivamente la **versión demo**: cómo publicar la misma aplicación en
Render, con datos ficticios, para que cualquier persona pueda probarla desde un enlace público
sin necesitar IIS ni SQL Server. Es útil para mostrarla a Gestión Humana o a la Gerencia antes
de instalarla en el servidor real.

## Cómo funciona la versión demo

Es el **mismo código** que se despliega en IIS — ni un controlador, vista o regla de negocio
distinta — con tres diferencias, todas controladas por la variable de entorno
`ASPNETCORE_ENVIRONMENT=Demo` (que activa `app/src/Alianzagrafica.Evaluacion180.Web/appsettings.Demo.json`):

1. **Base de datos:** en vez de SQL Server usa SQLite (un solo archivo, sin necesidad de un
   servidor de base de datos aparte) — controlado por `Database:Provider`.
2. **Datos ficticios automáticos:** al arrancar por primera vez, `Data/DemoSeed.cs` siembra un
   organigrama ficticio de 9 personas (gerente, dos jefes, analista, dos operarios, dos
   auxiliares de planta y un conductor de despachos — los seis tipos de personal del modelo
   180°), con un periodo de evaluación ya abierto y sus asignaciones generadas, para que la demo
   tenga contenido navegable desde el primer segundo. Las competencias organizacionales y las de
   rol del Conductor se tomaron (nombre y definición, sin datos de personas reales) del formato
   de evaluación de desempeño que Alianzagrafica diligencia hoy en Excel (código interno
   GHU-FOR-007), para que la demo se sienta más cercana al proceso actual de la empresa.
3. **Aviso visible:** un banner amarillo en cada página ("Ambiente de demostración con datos
   ficticios") para que nadie confunda la demo con el sistema de producción.

**Importante:** el disco del contenedor en el plan gratuito de Render es efímero — cada
redespliegue o reinicio borra la base de datos SQLite y `DemoSeed` la vuelve a poblar desde
cero. Para una demo esto es una ventaja (siempre arranca limpia); si en algún momento quieres
que los datos persistan entre reinicios, la sección 4 explica cómo agregar un disco persistente
de Render.

## 1. Publicar el código en GitHub

Si ya tienes cuenta de GitHub, la forma más rápida es con el CLI oficial (`gh`). Si prefieres
la interfaz web, el paso equivalente está entre paréntesis en cada punto.

```bash
# Desde la carpeta que contiene este README (raíz del repositorio)
git init
git add .
git commit -m "Versión inicial: aplicación de evaluación 180° + script SQL"

# Crear el repositorio en GitHub y subir (reemplaza "alianzagrafica" por tu usuario u organización
# si el repo debe quedar privado, agrega --private)
gh repo create alianzagrafica/evaluacion180 --private --source=. --remote=origin --push
```

**Sin `gh` CLI (interfaz web):**
1. Entra a https://github.com/new, ponle un nombre al repositorio (por ejemplo
   `evaluacion180`), márcalo como **privado** (contiene el organigrama ficticio de la empresa;
   no hay necesidad de que sea público) y créalo **sin** README ni `.gitignore` (ya vienen en
   este proyecto).
2. GitHub te va a mostrar los comandos exactos para tu repositorio recién creado — son estos
   tres, ejecutados desde la carpeta raíz de este proyecto:
   ```bash
   git init
   git add .
   git commit -m "Versión inicial: aplicación de evaluación 180° + script SQL"
   git branch -M main
   git remote add origin https://github.com/<tu-usuario>/evaluacion180.git
   git push -u origin main
   ```

## 2. Crear una cuenta en Render

Entra a https://render.com y crea una cuenta (puedes registrarte directamente con tu cuenta de
GitHub, lo cual además simplifica el paso siguiente porque Render ya queda autorizado a leer
tus repositorios).

## 3. Crear el servicio web en Render

1. En el panel de Render, click en **New +** → **Web Service**.
2. Conecta tu cuenta de GitHub si no lo hiciste al registrarte, y selecciona el repositorio
   `evaluacion180` que subiste en el paso 1.
3. Render detecta automáticamente el `Dockerfile` en la raíz del repositorio y preselecciona
   **Environment: Docker** — déjalo así (no cambiar a "Node" ni ningún runtime nativo).
4. Configuración recomendada para la demo:
   - **Name:** `evaluacion180-demo` (o el que prefieras — este nombre define la URL pública,
     algo como `https://evaluacion180-demo.onrender.com`).
   - **Region:** la más cercana a Colombia disponible (Oregon u Ohio son las más comunes en el
     plan gratuito).
   - **Branch:** `main`.
   - **Instance Type:** **Free** es suficiente para una demo (arranca más lento tras periodos de
     inactividad — ver nota abajo — pero no tiene costo).
   - **Auto-Deploy:** déjalo activado — así, cada `git push` a `main` vuelve a desplegar
     automáticamente la última versión.
5. No hace falta agregar variables de entorno manualmente: `ASPNETCORE_ENVIRONMENT=Demo` ya
   viene definida dentro del `Dockerfile`, y Render inyecta automáticamente la variable `PORT`
   que `docker-entrypoint.sh` usa para configurar Kestrel.
6. Click en **Create Web Service**. Render construye la imagen Docker (tarda unos 3-5 minutos
   la primera vez) y despliega el contenedor. Puedes seguir el progreso en la pestaña **Logs**;
   cuando veas una línea como `Now listening on: http://+:10000`, la aplicación ya está arriba.

## 4. Probar la demo

Abre la URL que Render te asignó (aparece en la parte superior del panel del servicio, algo
como `https://evaluacion180-demo.onrender.com`). Deberías ver la pantalla de inicio de sesión
con el aviso amarillo de "Ambiente de demostración" y una lista de usuarios de ejemplo.

Inicia sesión con cualquiera de estos usuarios y la clave `Demo2026*` (definida en
`appsettings.Demo.json`, sección `Auth:ClavePruebasLocal`):

| Usuario | Rol en la demo |
|---|---|
| `camila.torres@alianzagrafica-demo.com` | Gerente General — acceso de administrador completo (Periodos, Competencias, Auditoría) |
| `julian.restrepo@alianzagrafica-demo.com` | Jefe de Producción — evalúa a su equipo |
| `marcela.duque@alianzagrafica-demo.com` | Jefe Administrativa y Financiera — evalúa a su equipo |
| `andres.zapata@alianzagrafica-demo.com` | Operario Offset — colaborador evaluado |
| `diana.correa@alianzagrafica-demo.com` | Operario de Troquelado — colaborador evaluado |
| `luis.herrera@alianzagrafica-demo.com` | Auxiliar de Bodega — colaborador evaluado |
| `sandra.palacio@alianzagrafica-demo.com` | Analista de Nómina — colaboradora evaluada |
| `paola.giraldo@alianzagrafica-demo.com` | Auxiliar Administrativa — colaboradora evaluada |
| `diego.salazar@alianzagrafica-demo.com` | Conductor de Despachos — colaborador evaluado con competencias de rol propias (Orientación al cliente, Orientación al logro, Atención al detalle, Sentido de la urgencia, Escucha activa) |

Con `camila.torres` puedes recorrer todo: diligenciar su propia autoevaluación, crear/abrir/cerrar
periodos, generar asignaciones, administrar competencias y revisar la bitácora de auditoría. Con
cualquiera de los demás puedes probar el flujo de "diligenciar y enviar una evaluación" desde la
perspectiva de un colaborador o un jefe.

**Nota sobre el plan gratuito de Render:** un servicio web gratuito "se duerme" tras ~15 minutos
sin tráfico, y la primera solicitud después de eso tarda entre 30 segundos y un minuto en
responder mientras el contenedor arranca de nuevo (y `DemoSeed` vuelve a poblar los datos, ya
que el disco es efímero). Es normal — no es un error. Si vas a hacer una demostración en vivo,
abre el enlace un par de minutos antes para "despertarlo".

## 5. Actualizar la demo después de un cambio

Cualquier cambio que hagas en `app/` (o en este repositorio en general) se refleja en la demo
con:

```bash
git add .
git commit -m "Descripción del cambio"
git push
```

Render detecta el push a `main` y vuelve a construir y desplegar automáticamente (Auto-Deploy
activado en el paso 3). No hace falta tocar nada en el panel de Render para esto.

## 6. (Opcional) Persistir los datos entre reinicios

Si prefieres que los datos ficticios **no** se reinicien en cada redespliegue (por ejemplo, para
mostrar evaluaciones que alguien fue diligenciando a lo largo de varios días), Render ofrece
discos persistentes en sus planes pagos:

1. En el panel del servicio → pestaña **Disks** → **Add Disk**.
2. **Mount Path:** `/data` (la misma carpeta donde `appsettings.Demo.json` guarda
   `evaluacion180-demo.db`).
3. Tamaño: 1 GB es más que suficiente.

Con el disco montado, `DemoSeed` solo siembra datos la primera vez (ya es idempotente — revisa
si la base está vacía antes de sembrar), y las evaluaciones que se diligencien después sí
persisten entre reinicios y redespliegues.

## Sobre esta demo vs. el sistema real

Esta versión demo existe únicamente para mostrar la aplicación funcionando sin depender de la
infraestructura de Alianzagrafica. El despliegue que de verdad va a usar la empresa —conectado a
SQL Server, en el IIS corporativo, con la autenticación de Windows/AD y (más adelante) los datos
reales de Novasoft— se documenta por separado en **`app/README.md`**, y no depende de nada de lo
descrito aquí (Dockerfile, Render, SQLite): son dos formas de correr exactamente el mismo
código, seleccionadas con una sola variable de entorno.
