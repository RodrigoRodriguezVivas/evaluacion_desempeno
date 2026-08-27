using Alianzagrafica.Evaluacion180.Web;
using Alianzagrafica.Evaluacion180.Web.Controllers;
using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuración ----
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection("WhatsApp"));

// ---- Base de datos ----
// Database:Provider = "SqlServer" (valor por defecto, usado en el despliegue real en el IIS
// de Alianzagrafica) o "Sqlite" (solo en el ambiente de DEMOSTRACIÓN — ver
// appsettings.Demo.json y README.md, sección "Versión demo"). La cadena de conexión se define
// en appsettings.json / appsettings.Production.json, o mejor aún, mediante variables de entorno
// o el almacén de configuración de IIS en producción, para no dejar credenciales en el control
// de versiones.
var proveedorBaseDatos = builder.Configuration["Database:Provider"] ?? "SqlServer";
var cadenaConexion = builder.Configuration.GetConnectionString("EvaluacionDesempeno180");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(proveedorBaseDatos, "Sqlite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(cadenaConexion);
    else
        options.UseSqlServer(cadenaConexion);
});

// ---- Servicios de negocio ----
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IAsignacionService, AsignacionService>();
builder.Services.AddScoped<IResultadoService, ResultadoService>();
builder.Services.AddScoped<INotificacionService, SmtpNotificacionService>();

// Módulo de envío de resultados (correo + WhatsApp) — ver Services/EnvioResultadoService.cs.
builder.Services.AddScoped<IResumenImagenService, ResumenImagenService>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppNotificacionService>();
builder.Services.AddScoped<IEnvioResultadoService, EnvioResultadoService>();

// ---- Autenticación por cookie ----
// En producción, cuando IIS tenga habilitada la Autenticación de Windows para el
// personal con Usuario.TipoAutenticacion = 'ActiveDirectory' (sección 8.5 del documento
// de diseño), este esquema de cookie sigue usándose igual: el inicio de sesión construye
// el ClaimsPrincipal a partir del usuario autenticado por IIS en vez de pedir una clave.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/IniciarSesion";
        options.LogoutPath = "/Cuenta/CerrarSesion";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Evaluacion180.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

// Confía en los encabezados X-Forwarded-* que agrega el proxy de borde delante de la
// aplicación (IIS los agrega vía ANCM en el despliegue real; Render los agrega en el
// ambiente de demostración, que termina HTTPS en su borde y reenvía tráfico HTTP simple
// al contenedor). Sin esto, la app vería siempre esquema "http" y forzaría redirecciones
// o marcaría la cookie de sesión como no segura de forma incorrecta.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// ---- Datos ficticios de demostración (solo cuando Demo:Habilitado=true — ver
// appsettings.Demo.json). Nunca se activa en el despliegue real (Producción/IIS). ----
if (app.Configuration.GetValue<bool>("Demo:Habilitado"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var asignaciones = scope.ServiceProvider.GetRequiredService<IAsignacionService>();
    var resultadosDemo = scope.ServiceProvider.GetRequiredService<IResultadoService>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeed.SembrarSiVacioAsync(db, hasher, asignaciones, resultadosDemo);
}

// ---- Pipeline HTTP ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
