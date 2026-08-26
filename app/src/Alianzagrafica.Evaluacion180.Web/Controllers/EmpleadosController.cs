using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

/// <summary>
/// Consulta de solo lectura de la información de empleados (sección 3.2 del documento
/// de diseño: el sistema de evaluación NUNCA edita datos maestros de empleados). En
/// producción esta lista se alimenta de Novasoft; hoy proviene de la tabla ficticia
/// dbo.Empleado (ver sql/01_esquema_y_datos_ficticios.sql).
/// </summary>
[Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema)]
public class EmpleadosController : Controller
{
    private readonly AppDbContext _db;

    public EmpleadosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var consulta = _db.Empleados.Include(e => e.TipoPersonal).Include(e => e.JefeDirecto).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            consulta = consulta.Where(e => e.Nombre.Contains(q) || e.Cargo.Contains(q) || e.Area.Contains(q));
        }

        var empleados = await consulta.OrderBy(e => e.Nombre).ToListAsync();

        var modelo = empleados.Select(e => new EmpleadoListaItemViewModel
        {
            CodigoEmpleado = e.CodigoEmpleado,
            Nombre = e.Nombre,
            Cargo = e.Cargo,
            Area = e.Area,
            TipoPersonal = e.TipoPersonal.Nombre,
            JefeDirecto = e.JefeDirecto?.Nombre,
            Estado = e.Estado,
        }).ToList();

        ViewBag.Busqueda = q;
        return View(modelo);
    }
}
