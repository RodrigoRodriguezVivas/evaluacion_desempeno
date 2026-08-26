using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

/// <summary>
/// Consulta de solo lectura de la información de empleados (sección 3.2 del documento
/// de diseño: el sistema de evaluación NUNCA edita datos maestros de empleados). En
/// producción esta lista se alimenta de Novasoft; hoy proviene de la tabla ficticia
/// dbo.Empleado (ver sql/01_esquema_y_datos_ficticios.sql).
///
/// La única excepción es el número de WhatsApp para el envío de resultados (RF-23):
/// se guarda en la tabla local ContactoNotificacion, separada de Empleado, precisamente
/// para no tocar el dato maestro que viene de Novasoft — ver ContactoNotificacion.cs.
/// </summary>
[Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema)]
public class EmpleadosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public EmpleadosController(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var consulta = _db.Empleados.Include(e => e.TipoPersonal).Include(e => e.JefeDirecto).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            consulta = consulta.Where(e => e.Nombre.Contains(q) || e.Cargo.Contains(q) || e.Area.Contains(q));
        }

        var empleados = await consulta.OrderBy(e => e.Nombre).ToListAsync();
        var contactos = await _db.ContactosNotificacion.ToDictionaryAsync(c => c.CodigoEmpleado, c => c.TelefonoWhatsApp);

        var modelo = empleados.Select(e => new EmpleadoListaItemViewModel
        {
            CodigoEmpleado = e.CodigoEmpleado,
            Nombre = e.Nombre,
            Cargo = e.Cargo,
            Area = e.Area,
            TipoPersonal = e.TipoPersonal.Nombre,
            JefeDirecto = e.JefeDirecto?.Nombre,
            Estado = e.Estado,
            TelefonoWhatsApp = contactos.TryGetValue(e.CodigoEmpleado, out var telefono) ? telefono : null,
        }).ToList();

        ViewBag.Busqueda = q;
        return View(modelo);
    }

    // POST /Empleados/ActualizarContacto — único dato editable de un empleado desde este
    // sistema: el número de WhatsApp usado para el envío de resultados (RF-23). Se guarda en
    // ContactoNotificacion (tabla local, ajena a Novasoft), nunca en Empleado.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarContacto(int codigoEmpleado, string? telefonoWhatsApp)
    {
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.CodigoEmpleado == codigoEmpleado);
        if (empleado is null) return NotFound();

        var telefonoLimpio = string.IsNullOrWhiteSpace(telefonoWhatsApp) ? null : telefonoWhatsApp.Trim();

        var contacto = await _db.ContactosNotificacion.FirstOrDefaultAsync(c => c.CodigoEmpleado == codigoEmpleado);
        if (contacto is null)
        {
            contacto = new ContactoNotificacion { CodigoEmpleado = codigoEmpleado };
            _db.ContactosNotificacion.Add(contacto);
        }
        contacto.TelefonoWhatsApp = telefonoLimpio;
        contacto.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(null, "ActualizacionContactoWhatsApp",
            $"Empleado {codigoEmpleado} — {(telefonoLimpio is null ? "número eliminado" : "número actualizado")}",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Mensaje"] = $"Número de WhatsApp de {empleado.Nombre} actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
