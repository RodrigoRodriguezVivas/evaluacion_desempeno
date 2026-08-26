# Sistema de Evaluación de Desempeño 180° — Alianzagrafica

Aplicación web ASP.NET Core 8 (MVC) que implementa el modelo de evaluación de
desempeño a 180° descrito en el documento de requerimientos y diseño
(`Documento_Requerimientos_Diseno_Evaluacion180_Alianzagrafica.docx`) y que usa
como base de datos el esquema creado por `sql/01_esquema_y_datos_ficticios.sql`.

Este README cubre exclusivamente la puesta en marcha técnica (requisitos,
compilación, despliegue en IIS y configuración) del **despliegue real**, contra
SQL Server. El diseño funcional completo —flujos, roles, reglas de negocio,
modelo de datos— está en el documento de requerimientos y diseño.

> ¿Buscas la **versión demo** (publicable en Render con datos ficticios, sin
> necesitar IIS ni SQL Server)? Está documentada en el `README.md` de la raíz
> del repositorio, no en este archivo.

## 1. Requisitos previos

En el servidor donde se va a instalar (o en el equipo de desarrollo que va a
compilar y publicar):

- **.NET 8 SDK** (para compilar/publicar) — https://dotnet.microsoft.com/download/dotnet/8.0
- En el servidor IIS de destino, el **ASP.NET Core Runtime 8.0 — Hosting Bundle**
  (incluye el módulo `ANCM` que permite a IIS alojar aplicaciones .NET). Sin este
  módulo, IIS no puede ejecutar la aplicación aunque el sitio esté bien configurado.
- **Microsoft SQL Server** (2016 o superior) con acceso de red desde el servidor IIS.
- El script `sql/01_esquema_y_datos_ficticios.sql` ya ejecutado sobre una base
  de datos vacía. Ese script incluye datos ficticios de ejemplo pensados solo
  para pruebas; en el ambiente real de Alianzagrafica, la tabla `Empleado` debe
  sincronizarse desde Novasoft (ver sección 5).

## 2. Estructura del proyecto

```
app/
  Alianzagrafica.Evaluacion180.sln
  src/
    Alianzagrafica.Evaluacion180.Web/     ← proyecto ASP.NET Core MVC (único proyecto de la solución)
sql/
  01_esquema_y_datos_ficticios.sql        ← script de creación de esquema + datos ficticios
```

## 3. Compilación

Este proyecto referencia el paquete NuGet oficial `Microsoft.EntityFrameworkCore.SqlServer`
(que a su vez trae `Microsoft.EntityFrameworkCore` y `Microsoft.EntityFrameworkCore.Relational`).
Para restaurarlo se necesita acceso de salida a `nuget.org` desde el equipo donde
se compile — algo que no estaba disponible en el entorno donde se generó este
proyecto (ver nota de transparencia en la sección 8), por lo que **la primera
compilación en un equipo con acceso normal a internet es el primer paso a
verificar**:

```bash
cd app
dotnet restore
dotnet build -c Release
```

Si `dotnet restore` falla por temas de proxy/firewall corporativo, configúrelo
según la política de red de Alianzagrafica (variable `HTTP_PROXY`/`HTTPS_PROXY`,
o un NuGet.Config con un feed interno que replique nuget.org).

## 4. Publicación y despliegue en IIS

### 4.1 Publicar

```bash
cd app/src/Alianzagrafica.Evaluacion180.Web
dotnet publish -c Release -o ./publicar
```

Esto genera en `./publicar` todo lo necesario para el sitio IIS (no requiere
copiar el código fuente ni el SDK al servidor, solo el contenido de esa carpeta).

### 4.2 Configurar el sitio en IIS

1. Instalar el **ASP.NET Core Runtime 8.0 — Hosting Bundle** en el servidor
   (si no estaba instalado, reiniciar IIS o el servidor después de instalarlo).
2. Copiar el contenido de `./publicar` a la carpeta del sitio en el servidor
   (por ejemplo `C:\inetpub\wwwroot\Evaluacion180`).
3. En el Administrador de IIS, crear un **Grupo de aplicaciones (Application
   Pool)** nuevo:
   - **.NET CLR version:** `No Managed Code` (el runtime de .NET 8 no usa el
     CLR clásico de IIS; esto es obligatorio, no opcional).
   - **Modelo de proceso → Identidad:** una cuenta con permisos de lectura
     sobre la carpeta del sitio y, si se va a usar Autenticación de Windows
     contra SQL Server, con permisos en la base de datos (ver 4.4).
4. Crear el **sitio** (o aplicación dentro de un sitio existente) apuntando a
   la carpeta copiada, usando el grupo de aplicaciones anterior.
5. Confirmar que el binding HTTPS tenga un certificado válido asignado — la
   aplicación fuerza redirección HTTPS (`UseHttpsRedirection`) y usa cookies
   marcadas `HttpOnly`; sin HTTPS configurado en IIS, el navegador mostrará
   advertencias y las cookies de sesión no viajarán de forma segura.

### 4.3 Configurar la cadena de conexión

**No** edite `appsettings.json` directamente en el servidor de producción con
credenciales reales (para no dejarlas en el control de versiones si el
despliegue se hace desde el mismo repositorio). Las dos formas recomendadas:

**Opción A — variable de entorno del Application Pool** (recomendada):
En IIS Manager → sitio → Configuration Editor, o vía `appcmd`, definir la
variable de entorno del grupo de aplicaciones:

```
ConnectionStrings__EvaluacionDesempeno180 = Server=SQLALIANZA\PROD;Database=EvaluacionDesempeno180;Trusted_Connection=True;TrustServerCertificate=True;
```

**Opción B — `appsettings.Production.json`** en la carpeta publicada (se
carga automáticamente porque IIS ejecuta la app con
`ASPNETCORE_ENVIRONMENT=Production` por defecto):

```json
{
  "ConnectionStrings": {
    "EvaluacionDesempeno180": "Server=SQLALIANZA\\PROD;Database=EvaluacionDesempeno180;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 4.4 Autenticación de Windows contra SQL Server (opcional)

Si se usa `Trusted_Connection=True` (autenticación integrada de Windows) en
lugar de usuario/clave de SQL Server, la identidad del grupo de aplicaciones
de IIS (sección 4.2) debe tener una cuenta de dominio con permisos de
lectura/escritura sobre la base de datos `EvaluacionDesempeno180`. Coordinar
con el equipo de infraestructura de Alianzagrafica para crear o autorizar esa
cuenta de servicio.

## 5. Sobre la tabla `Empleado` y Novasoft

Mientras no se confirme y conecte el esquema real de Novasoft, la aplicación
usa la tabla `Empleado` creada por `sql/01_esquema_y_datos_ficticios.sql` como
sustituto temporal con el mismo contrato de columnas documentado en el diseño
(sección 8.4 del documento de requerimientos y diseño). La aplicación **nunca
escribe** en esa tabla — solo la lee — por lo que cuando se conecte la
sincronización real desde Novasoft (vista de solo lectura o proceso de
sincronización periódica hacia esa misma tabla o hacia una vista con la misma
forma), no hace falta cambiar código, solo la fuente de esos datos.

## 6. Modo de pruebas de autenticación — IMPORTANTE

`appsettings.json` incluye una sección `Auth` con un modo de pruebas
(`ModoPruebasLocal`) que permite iniciar sesión con una clave fija
(`ClavePruebasLocal`) para **cualquier** usuario activo, sin validar
contraseña real. Esto existe únicamente para poder probar la aplicación antes
de tener Autenticación de Windows o hashes de clave reales cargados.

Por seguridad, este repositorio incluye `appsettings.Production.json` con
`Auth:ModoPruebasLocal` en `false`, que IIS aplica automáticamente porque por
defecto ejecuta con `ASPNETCORE_ENVIRONMENT=Production`. **Antes de dar por
cerrado el despliegue, confirme explícitamente que:**

1. La variable de entorno `ASPNETCORE_ENVIRONMENT` del Application Pool en IIS
   **no** esté forzada a `Development` (si lo está, `appsettings.Production.json`
   no se aplicaría y el modo de pruebas seguiría activo).
2. El personal con `TipoAutenticacion = 'ActiveDirectory'` en la tabla
   `Usuario` inicie sesión mediante Autenticación de Windows habilitada en
   IIS (ver sección 8.5 del documento de diseño) — el formulario de clave de
   `/Cuenta/IniciarSesion` es solo para el personal con
   `TipoAutenticacion = 'Local'` (por ejemplo, cuentas de proveedores externos
   sin cuenta de dominio, si las hubiera).
3. Las claves reales de las cuentas `Local` estén cargadas como hash PBKDF2
   en `Usuario.ClaveHash` (la aplicación usa
   `Rfc2898DeriveBytes.Pbkdf2` con 100 000 iteraciones — no hay una pantalla
   de "olvidé mi clave" en esta primera versión; el restablecimiento de clave
   se hace actualizando `ClaveHash` directamente en la base de datos hasta que
   se priorice esa funcionalidad).

## 7. Notificaciones por correo (SMTP)

La sección `Smtp` de `appsettings.json` tiene `ModoSimulado: true` por
defecto: mientras esté así, los correos de notificación (RF-10, RF-13) no se
envían de verdad, solo quedan registrados en el log de la aplicación. Para
activar el envío real, configurar (por variable de entorno o
`appsettings.Production.json`, igual que la cadena de conexión):

```json
{
  "Smtp": {
    "ModoSimulado": false,
    "Host": "smtp.alianzagrafica.com",
    "Puerto": 587,
    "UsarSsl": true,
    "Usuario": "evaluacion180@alianzagrafica.com",
    "Clave": "CAMBIAR_EN_PRODUCCION",
    "CorreoRemitente": "evaluacion180@alianzagrafica.com",
    "UrlBase": "https://evaluacion180.alianzagrafica.com"
  }
}
```

## 8. Envío de resultados por correo y WhatsApp (RF-23)

Desde **Reportes** (sección de resultados consolidados), el personal de
Gestión Humana o de Sistemas puede pulsar "Enviar resultado" junto a un
colaborador para que el sistema le envíe su resultado consolidado por dos
canales, de forma independiente:

- **Correo electrónico**: un mensaje HTML con el promedio general y un enlace
  a "Mis resultados", con la imagen-resumen adjunta en PNG. Usa la misma
  configuración `Smtp` de la sección 7.
- **WhatsApp**: la misma imagen-resumen, enviada como mensaje de WhatsApp
  Business a través de la API de Twilio. Solo se envía si el colaborador
  tiene un número de WhatsApp registrado (ver más abajo); si no lo tiene, el
  sistema lo informa en pantalla y de todas formas envía el correo.

Ambos envíos quedan registrados en el módulo de auditoría (`EnvioResultadoCorreo`,
`EnvioResultadoWhatsApp`), igual que el resto de acciones del sistema.

### 8.1 Número de WhatsApp del colaborador

El número de WhatsApp **no** viene de Novasoft ni se guarda en la tabla
`Empleado` — este sistema nunca edita datos maestros de empleados (sección 5).
Se guarda en una tabla local nueva, `ContactoNotificacion`, editable desde
**Empleados** (columna "WhatsApp", con guardado en línea). Esto significa que
el número sobrevive a cada resincronización desde Novasoft sin tocar el dato
maestro.

### 8.2 Configuración de WhatsApp (Twilio)

La sección `WhatsApp` de `appsettings.json` tiene `ModoSimulado: true` por
defecto: mientras esté así, los mensajes de WhatsApp no se envían de verdad,
solo quedan registrados en el log de la aplicación (igual que el `ModoSimulado`
de SMTP). Para activar el envío real hace falta una cuenta de **WhatsApp
Business API** — este proyecto usa **Twilio** como proveedor (vía su API REST
directa, sin el SDK de Twilio, para no sumar dependencias NuGet nuevas más
allá de las de generación de imágenes — ver 8.4):

```json
{
  "WhatsApp": {
    "ModoSimulado": false,
    "Proveedor": "Twilio",
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "CAMBIAR_EN_PRODUCCION",
    "NumeroRemitente": "+14155238886",
    "IndicativoPaisPorDefecto": "57"
  }
}
```

`NumeroRemitente` es el número de WhatsApp Business aprobado en la cuenta de
Twilio (en modo sandbox de pruebas, Twilio asigna uno propio). Antes de poner
`ModoSimulado` en `false` en producción, Alianzagrafica debe tener una cuenta
de Twilio (o, como alternativa futura, la API de Meta directamente —
`IWhatsAppService` está diseñado para admitir otro proveedor sin tocar el
resto del sistema) con el número de envío ya aprobado por Meta para WhatsApp
Business, y aceptar las políticas de plantillas de mensajes de WhatsApp que
apliquen según el volumen de envío.

### 8.3 Cómo llega la imagen a WhatsApp

A diferencia del correo (donde la imagen va adjunta directamente), la API de
WhatsApp de Twilio descarga la imagen desde una URL pública en vez de recibir
el archivo binario. Por eso, al enviar por WhatsApp el sistema:

1. Genera la imagen-resumen una sola vez (una "fotografía" fija del resultado
   en ese momento).
2. La guarda temporalmente en la base de datos junto con un token aleatorio.
3. Construye un enlace público `.../Resultados/ImagenResumen/{token}` (usa
   `Smtp:UrlBase`, que por eso debe ser la URL real y accesible desde
   internet de la aplicación, nunca `localhost`).
4. Le pasa ese enlace a Twilio, que lo descarga y lo entrega dentro del
   mensaje de WhatsApp.

El enlace es de un solo uso conceptual y expira a los 30 minutos — vencido
ese plazo, el sistema responde "no encontrado" a cualquier solicitud con ese
token. **Pendiente de mejora**, documentado también en el código: no existe
todavía una tarea programada que borre filas ya expiradas de la tabla
`EnvioResultadoToken`; hoy simplemente dejan de ser accesibles, pero siguen
ocupando espacio hasta que alguien las purgue manualmente o se agregue esa
tarea.

### 8.4 Generación de la imagen-resumen

La imagen-resumen (nombre, cargo, promedio general con su banda de
calificación según el formato GHU-FOR-007, y el detalle por competencia) se
genera con **SixLabors.ImageSharp** y **SixLabors.ImageSharp.Drawing** —
librerías 100% administradas (no dependen de GDI+ ni de `libgdiplus`), a
propósito para que la imagen se vea igual tanto en el contenedor Docker Linux
del ambiente de demostración como en el IIS de Alianzagrafica sobre Windows.
El texto se dibuja con la fuente DejaVu Sans, incluida dentro del proyecto
(`Assets/Fonts/`, licencia Bitstream Vera — ver
`Assets/Fonts/LICENSE-DejaVu.txt`) para que el resultado no dependa de qué
fuentes tenga instaladas el servidor.

**Importante — este código no se pudo compilar ni ejecutar en este entorno de
desarrollo**, por la misma falta de acceso a `nuget.org` descrita en la
sección 9: los paquetes `SixLabors.ImageSharp`/`SixLabors.ImageSharp.Drawing`
son nuevos en este proyecto y no había forma de descargarlos aquí para
probarlos. Se escribió el código con cuidado contra la superficie de API
estable y conocida de esas librerías, pero **el primer
`dotnet restore && dotnet build` en un equipo con acceso normal a internet
debe usarse para confirmar que compila**, y conviene hacer al menos un envío
de prueba de extremo a extremo (con `ModoSimulado: true` en ambos canales
primero, revisando el log, y luego con un número/correo real de prueba)
antes de dar por buena esta funcionalidad en producción.

Lo que sí se pudo verificar sin depender de NuGet, por tratarse de lógica
pura sin dependencias externas, fueron las reglas de normalización del
número de WhatsApp (`WhatsAppNotificacionService.NormalizarNumero`) y de
clasificación de la banda de calificación (`ResumenImagenService.Banda`,
GHU-FOR-007: 1–2 ≈ Deficiente, 3 ≈ Aceptable, 4 ≈ Bueno, 5 ≈ Sobresaliente,
sobre la escala continua de promedios), ejecutando esa lógica de forma
aislada en un proyecto de consola sin ninguna referencia a paquete NuGet.
Las 13 pruebas ejecutadas pasaron correctamente.

## 9. Transparencia sobre cómo se verificó esta aplicación

Este proyecto se desarrolló en un entorno de trabajo sin salida de red hacia
`nuget.org` ni una instancia real de SQL Server disponible. Para poder
compilar y probar la lógica de negocio de extremo a extremo de todas formas
(inicio de sesión, diligenciar una evaluación completa, generar periodos y
asignaciones, activar/desactivar competencias, y confirmar que cada acción
queda en el registro de auditoría), se construyó temporalmente una réplica
mínima en memoria de la superficie de EF Core usada por el proyecto, y se
ejecutó la aplicación real contra esa réplica con datos ficticios de siembra.

Esa réplica **no forma parte de esta entrega**: el proyecto publicado usa
exclusivamente el paquete NuGet oficial `Microsoft.EntityFrameworkCore.SqlServer`
de Microsoft (sección 3), y todo el código específico de esa verificación
(proyecto stub, siembra de datos de prueba, referencias temporales) fue
retirado antes de este README. Lo que se mantiene sin cambios es exactamente
el código de negocio, controladores, vistas y mapeo de datos que se ejecutó y
verificó de esa forma.

En consecuencia, se recomienda que el primer `dotnet restore && dotnet build`
en un equipo con acceso normal a internet (sección 3) sea el paso de
verificación final antes de publicar a producción, ya que este entorno no
pudo confirmar la compilación contra los paquetes NuGet reales ni una
conexión a SQL Server real. Esto aplica en particular al módulo de envío de
resultados por correo y WhatsApp descrito en la sección 8, que agrega los
únicos paquetes NuGet nuevos de todo el proyecto.

## 10. Credenciales de prueba (solo con `ModoPruebasLocal: true`)

Con el script `sql/01_esquema_y_datos_ficticios.sql` ya ejecutado y el modo de
pruebas activo (nunca en producción — ver sección 6), cualquier usuario
ficticio de ese script puede iniciar sesión usando su `NombreUsuario` y la
clave fija configurada en `Auth:ClavePruebasLocal` de `appsettings.json`.
