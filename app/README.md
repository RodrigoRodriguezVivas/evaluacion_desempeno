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

## 8. Transparencia sobre cómo se verificó esta aplicación

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
conexión a SQL Server real.

## 9. Credenciales de prueba (solo con `ModoPruebasLocal: true`)

Con el script `sql/01_esquema_y_datos_ficticios.sql` ya ejecutado y el modo de
pruebas activo (nunca en producción — ver sección 6), cualquier usuario
ficticio de ese script puede iniciar sesión usando su `NombreUsuario` y la
clave fija configurada en `Auth:ClavePruebasLocal` de `appsettings.json`.
