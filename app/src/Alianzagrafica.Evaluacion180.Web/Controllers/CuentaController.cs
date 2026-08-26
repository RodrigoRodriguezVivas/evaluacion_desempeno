using System.Security.Claims;
using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

public class CuentaController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditoriaService _auditoria;
    private readonly IOptions<AuthOptions> _authOptions;

    public CuentaController(AppDbContext db, IPasswordHasher hasher, IAuditoriaService auditoria, IOptions<AuthOptions> authOptions)
    {
        _db = db;
        _hasher = hasher;
        _auditoria = auditoria;
        _authOptions = authOptions;
    }

    [HttpGet]
    public IActionResult IniciarSesion(string? urlRetorno = null)
    {
        return View(new IniciarSesionViewModel { UrlRetorno = urlRetorno });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarSesion(IniciarSesionViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var usuario = await _db.Usuarios
            .Include(u => u.Empleado).ThenInclude(e => e.TipoPersonal)
            .Include(u => u.UsuarioRoles)
            .FirstOrDefaultAsync(u => u.NombreUsuario == modelo.NombreUsuario && u.Activo);

        var credencialesValidas = usuario is not null && ValidarClave(usuario, modelo.Clave);

        if (!credencialesValidas)
        {
            await _auditoria.RegistrarAsync(usuario?.IdUsuario, "LoginFallido", $"Usuario: {modelo.NombreUsuario}", HttpContext.Connection.RemoteIpAddress?.ToString());
            modelo.MensajeError = "Correo o contraseña incorrectos.";
            return View(modelo);
        }

        var roles = await _db.UsuarioRoles
            .Where(ur => ur.IdUsuario == usuario!.IdUsuario)
            .Include(ur => ur.Rol)
            .Select(ur => ur.Rol.NombreRol)
            .ToListAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario!.Empleado.CodigoEmpleado.ToString()),
            new Claim(ClaimTypes.Name, usuario.Empleado.Nombre),
            new Claim("NombreUsuario", usuario.NombreUsuario),
            new Claim("Cargo", usuario.Empleado.Cargo),
            new Claim("TipoPersonal", usuario.Empleado.TipoPersonal.Nombre),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        await _auditoria.RegistrarAsync(usuario.IdUsuario, "Login", null, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!string.IsNullOrEmpty(modelo.UrlRetorno) && Url.IsLocalUrl(modelo.UrlRetorno))
            return Redirect(modelo.UrlRetorno);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("IniciarSesion");
    }

    [HttpGet]
    public IActionResult AccesoDenegado() => View();

    private bool ValidarClave(Models.Entidades.Usuario usuario, string claveIngresada)
    {
        // Modo de pruebas: acepta la clave fija configurada para cualquier usuario activo,
        // sin importar su TipoAutenticacion. Debe quedar deshabilitado en producción.
        if (_authOptions.Value.ModoPruebasLocal && claveIngresada == _authOptions.Value.ClavePruebasLocal)
            return true;

        if (usuario.TipoAutenticacion == Models.Entidades.Constantes.AutenticacionLocal && usuario.ClaveHash is { Length: > 0 })
            return _hasher.Verify(claveIngresada, usuario.ClaveHash);

        // TipoAutenticacion = ActiveDirectory: en producción, IIS con Windows Authentication
        // ya autentica al usuario antes de llegar aquí; este formulario de clave no aplica.
        return false;
    }
}

public class AuthOptions
{
    public bool ModoPruebasLocal { get; set; }
    public string ClavePruebasLocal { get; set; } = string.Empty;
}
