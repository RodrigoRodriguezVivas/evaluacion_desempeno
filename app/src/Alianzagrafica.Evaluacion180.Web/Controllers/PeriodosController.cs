using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema)]
public class PeriodosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAsignacionService _asignaciones;
    private readonly INotificacionService _notificaciones;
    private readonly IAuditoriaService _auditoria;

    public PeriodosController(AppDbContext db, IAsignacionService asignaciones, INotificacionService notificaciones, IAuditoriaService auditoria)
    {
        _db = db;
        _asignaciones = asignaciones;
        _notificaciones = notificaciones;
        _auditoria = auditoria;
    }

    public async Task<IActionResult> Index()
    {
        var periodos = await _db.PeriodosEvaluacion.OrderByDescending(p => p.FechaApertura).ToListAsync();
        var formulariosPorPeriodo = await _db.FormulariosEvaluacion.GroupBy(f => f.IdPeriodo).Select(g => new { g.Key, Total = g.Count() }).ToListAsync();
        var asignacionesPorPeriodo = await _db.AsignacionesEvaluacion.GroupBy(a => a.IdPeriodo).Select(g => new { g.Key, Total = g.Count() }).ToListAsync();

        var modelo = periodos.Select(p => new PeriodoListaItemViewModel
        {
            IdPeriodo = p.IdPeriodo,
            Nombre = p.Nombre,
            FechaApertura = p.FechaApertura,
            FechaCierre = p.FechaCierre,
            Estado = p.Estado,
            TotalFormularios = formulariosPorPeriodo.FirstOrDefault(f => f.Key == p.IdPeriodo)?.Total ?? 0,
            TotalAsignaciones = asignacionesPorPeriodo.FirstOrDefault(a => a.Key == p.IdPeriodo)?.Total ?? 0,
        }).ToList();

        return View(modelo);
    }

    [HttpGet]
    public IActionResult Crear() => View(new CrearPeriodoViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearPeriodoViewModel modelo)
    {
        if (modelo.FechaCierre <= modelo.FechaApertura)
            ModelState.AddModelError(nameof(modelo.FechaCierre), "La fecha de cierre debe ser posterior a la de apertura.");

        if (!ModelState.IsValid) return View(modelo);

        var periodo = new PeriodoEvaluacion
        {
            Nombre = modelo.Nombre,
            FechaApertura = modelo.FechaApertura,
            FechaCierre = modelo.FechaCierre,
            Estado = Constantes.PeriodoProgramado,
        };
        _db.PeriodosEvaluacion.Add(periodo);
        await _db.SaveChangesAsync();

        await _asignaciones.GenerarFormulariosAsync(periodo.IdPeriodo);
        await _auditoria.RegistrarAsync(null, "CreacionPeriodo", $"Periodo '{periodo.Nombre}' (Id {periodo.IdPeriodo})", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["Mensaje"] = "Periodo creado junto con sus formularios de evaluación. Ahora puedes abrirlo y generar las asignaciones.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarAsignaciones(int id)
    {
        var creadas = await _asignaciones.GenerarAsignacionesAsync(id);
        await _auditoria.RegistrarAsync(null, "GeneracionAsignaciones", $"Periodo {id}: {creadas} asignaciones nuevas", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Mensaje"] = $"Se generaron {creadas} asignaciones nuevas a partir de la jerarquía vigente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Abrir(int id)
    {
        var periodo = await _db.PeriodosEvaluacion.FirstOrDefaultAsync(p => p.IdPeriodo == id);
        if (periodo is null) return NotFound();
        periodo.Estado = Constantes.PeriodoAbierto;
        await _db.SaveChangesAsync();

        var asignaciones = await _db.AsignacionesEvaluacion.Where(a => a.IdPeriodo == id).ToListAsync();
        var notificadas = await _notificaciones.NotificarAsignacionesAsync(asignaciones);

        await _auditoria.RegistrarAsync(null, "AperturaPeriodo", $"Periodo '{periodo.Nombre}' (Id {id}) — {notificadas} evaluadores notificados", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Mensaje"] = $"Periodo abierto. Se notificó a {notificadas} evaluador(es).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var periodo = await _db.PeriodosEvaluacion.FirstOrDefaultAsync(p => p.IdPeriodo == id);
        if (periodo is null) return NotFound();
        periodo.Estado = Constantes.PeriodoCerrado;
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(null, "CierrePeriodo", $"Periodo '{periodo.Nombre}' (Id {id})", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Mensaje"] = "Periodo cerrado. Los resultados quedan en modo de solo lectura.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarRecordatorios(int id)
    {
        var enviados = await _notificaciones.EnviarRecordatoriosAsync(id);
        await _auditoria.RegistrarAsync(null, "EnvioRecordatorios", $"Periodo {id}: {enviados} recordatorios", HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Mensaje"] = $"Se enviaron recordatorios a {enviados} evaluador(es) con evaluaciones pendientes.";
        return RedirectToAction(nameof(Index));
    }
}
