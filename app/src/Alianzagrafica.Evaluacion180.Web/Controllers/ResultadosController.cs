using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize]
public class ResultadosController : Controller
{
    private readonly AppDbContext _db;

    public ResultadosController(AppDbContext db) => _db = db;

    // GET /Resultados/Mios — resultado consolidado propio (RF-16)
    public async Task<IActionResult> Mios()
    {
        var codigoEmpleado = User.CodigoEmpleado();
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.CodigoEmpleado == codigoEmpleado);
        if (empleado is null) return NotFound();

        var resultados = await _db.ResultadosConsolidados
            .Include(r => r.Periodo)
            .Where(r => r.CodigoEvaluado == codigoEmpleado)
            .OrderByDescending(r => r.Periodo.FechaApertura)
            .ToListAsync();

        var modelo = new MisResultadosViewModel
        {
            NombreEmpleado = empleado.Nombre,
            Historico = resultados.Select(r => new ResultadoPeriodoViewModel
            {
                PeriodoNombre = r.Periodo.Nombre,
                PromedioAutoevaluacion = r.PromedioAutoevaluacion,
                PromedioJefe = r.PromedioJefe,
                PromedioAscendente = r.PromedioAscendente,
                PromedioGeneral = r.PromedioGeneral,
                FechaConsolidacion = r.FechaConsolidacion,
            }).ToList(),
        };

        return View(modelo);
    }

    // GET /Resultados/Reportes — vista agregada por área (RF-17, RF-18, RF-19)
    [Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema + "," + Constantes.RolConsultaDirectiva)]
    public async Task<IActionResult> Reportes(int? idPeriodo)
    {
        var periodo = idPeriodo is int id
            ? await _db.PeriodosEvaluacion.FirstOrDefaultAsync(p => p.IdPeriodo == id)
            : await _db.PeriodosEvaluacion.OrderByDescending(p => p.FechaApertura).FirstOrDefaultAsync();

        var modelo = new ReportesViewModel { PeriodoNombre = periodo?.Nombre };
        if (periodo is null) return View(modelo);

        var asignaciones = await _db.AsignacionesEvaluacion
            .Include(a => a.Evaluado)
            .Where(a => a.IdPeriodo == periodo.IdPeriodo)
            .ToListAsync();

        modelo.AvancePorArea = asignaciones
            .GroupBy(a => a.Evaluado.Area)
            .Select(g => new ReporteAreaViewModel
            {
                Area = g.Key,
                TotalAsignadas = g.Count(),
                Completadas = g.Count(a => a.EstaCompletada),
                Pendientes = g.Count(a => !a.EstaCompletada),
            })
            .OrderBy(a => a.Area)
            .ToList();

        var resultados = await _db.ResultadosConsolidados
            .Include(r => r.Evaluado).ThenInclude(e => e.TipoPersonal)
            .Where(r => r.IdPeriodo == periodo.IdPeriodo)
            .ToListAsync();

        modelo.Resultados = resultados
            .OrderBy(r => r.PromedioGeneral)
            .Select(r => new ResultadoEmpleadoViewModel
            {
                NombreEmpleado = r.Evaluado.Nombre,
                Cargo = r.Evaluado.Cargo,
                TipoPersonal = r.Evaluado.TipoPersonal.Nombre,
                Area = r.Evaluado.Area,
                PromedioGeneral = r.PromedioGeneral,
            })
            .ToList();

        return View(modelo);
    }
}
