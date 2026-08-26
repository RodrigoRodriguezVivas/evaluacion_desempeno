using System.Diagnostics;
using System.Security.Claims;
using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var codigoEmpleado = User.CodigoEmpleado();
        var empleado = await _db.Empleados.Include(e => e.TipoPersonal).FirstOrDefaultAsync(e => e.CodigoEmpleado == codigoEmpleado);
        if (empleado is null) return RedirectToAction("IniciarSesion", "Cuenta");

        var periodoActual = await _db.PeriodosEvaluacion
            .Where(p => p.Estado == Constantes.PeriodoAbierto)
            .OrderByDescending(p => p.FechaApertura)
            .FirstOrDefaultAsync();

        var asignaciones = periodoActual is null
            ? new List<AsignacionEvaluacion>()
            : await _db.AsignacionesEvaluacion
                .Where(a => a.CodigoEvaluador == codigoEmpleado && a.IdPeriodo == periodoActual.IdPeriodo)
                .ToListAsync();

        var modelo = new DashboardViewModel
        {
            NombreEmpleado = empleado.Nombre,
            Cargo = empleado.Cargo,
            TipoPersonal = empleado.TipoPersonal.Nombre,
            Roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
            PeriodoActualNombre = periodoActual?.Nombre,
            EvaluacionesPendientes = asignaciones.Count(a => a.Estado != Constantes.AsignacionCompletada),
            EvaluacionesCompletadas = asignaciones.Count(a => a.Estado == Constantes.AsignacionCompletada),
            EsAdministrador = User.IsInRole(Constantes.RolAdministradorSistema) || User.IsInRole(Constantes.RolAdministradorGestionHumana),
        };

        return View(modelo);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
