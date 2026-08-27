using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize]
public class EvaluacionesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IResultadoService _resultados;
    private readonly IAuditoriaService _auditoria;

    public EvaluacionesController(AppDbContext db, IResultadoService resultados, IAuditoriaService auditoria)
    {
        _db = db;
        _resultados = resultados;
        _auditoria = auditoria;
    }

    // GET /Evaluaciones/Mis
    public async Task<IActionResult> Mis()
    {
        var codigoEmpleado = User.CodigoEmpleado();

        var asignaciones = await _db.AsignacionesEvaluacion
            .Include(a => a.Evaluado)
            .Include(a => a.Periodo)
            .Where(a => a.CodigoEvaluador == codigoEmpleado)
            .OrderBy(a => a.Periodo.FechaCierre)
            .ToListAsync();

        var modelo = new MisEvaluacionesViewModel
        {
            PeriodoActualNombre = asignaciones.FirstOrDefault(a => a.Periodo.Estado == Constantes.PeriodoAbierto)?.Periodo.Nombre,
        };

        foreach (var a in asignaciones)
        {
            var item = new AsignacionResumenViewModel
            {
                IdAsignacion = a.IdAsignacion,
                NombreEvaluado = a.Evaluado.Nombre,
                CargoEvaluado = a.Evaluado.Cargo,
                TipoRelacion = a.TipoRelacion,
                Estado = a.Estado,
                PeriodoNombre = a.Periodo.Nombre,
                PeriodoFechaCierre = a.Periodo.FechaCierre,
            };
            if (a.EstaCompletada) modelo.Completadas.Add(item);
            else modelo.Pendientes.Add(item);
        }

        return View(modelo);
    }

    // GET /Evaluaciones/Diligenciar/5
    public async Task<IActionResult> Diligenciar(int id)
    {
        var codigoEmpleado = User.CodigoEmpleado();

        var asignacion = await _db.AsignacionesEvaluacion
            .Include(a => a.Evaluado)
            .Include(a => a.Periodo)
            .Include(a => a.Formulario)
            .FirstOrDefaultAsync(a => a.IdAsignacion == id);

        if (asignacion is null) return NotFound();
        if (asignacion.CodigoEvaluador != codigoEmpleado) return Forbid();

        var competenciasFormulario = asignacion.IdFormulario is int idFormulario
            ? await _db.FormularioCompetencias
                .Include(fc => fc.Competencia)
                    .ThenInclude(c => c.Comportamientos)
                .Where(fc => fc.IdFormulario == idFormulario)
                // Ordenar por categoría primero para que los macro-grupos (Organizacional / De
                // Rol) queden agrupados de forma contigua al mostrar el formulario.
                .OrderBy(fc => fc.Competencia.Categoria)
                .ThenBy(fc => fc.Competencia.Nombre)
                .ToListAsync()
            : new List<FormularioCompetencia>();

        // Indicadores de gestión del formulario (Entregable 11 — macro-grupo "Indicadores de
        // Gestión", se muestra siempre antes que las competencias, igual que en el Excel origen).
        var indicadoresFormulario = asignacion.IdFormulario is int idFormularioInd
            ? await _db.FormularioIndicadores
                .Include(fi => fi.Indicador)
                .Where(fi => fi.IdFormulario == idFormularioInd)
                .OrderBy(fi => fi.Indicador.Nombre)
                .ToListAsync()
            : new List<FormularioIndicador>();

        var respuesta = await _db.RespuestasEvaluacion
            .Include(r => r.Detalles)
            .Include(r => r.DetallesComportamientos)
            .Include(r => r.DetallesIndicadores)
            .FirstOrDefaultAsync(r => r.IdAsignacion == id);

        var detallesPorCompetencia = respuesta?.Detalles.ToDictionary(d => d.IdCompetencia) ?? new Dictionary<int, RespuestaDetalle>();
        var detallesPorComportamiento = respuesta?.DetallesComportamientos.ToDictionary(d => d.IdComportamiento) ?? new Dictionary<int, RespuestaComportamientoDetalle>();
        var detallesPorIndicador = respuesta?.DetallesIndicadores.ToDictionary(d => d.IdIndicador) ?? new Dictionary<int, RespuestaIndicadorDetalle>();

        var modelo = new DiligenciarEvaluacionViewModel
        {
            IdAsignacion = asignacion.IdAsignacion,
            NombreEvaluado = asignacion.Evaluado.Nombre,
            CargoEvaluado = asignacion.Evaluado.Cargo,
            TipoRelacionTexto = new AsignacionResumenViewModel { TipoRelacion = asignacion.TipoRelacion }.TipoRelacionTexto,
            NombreFormulario = asignacion.Formulario?.Nombre ?? "(sin formulario configurado)",
            PeriodoNombre = asignacion.Periodo.Nombre,
            SoloLectura = asignacion.EstaCompletada,
            Items = competenciasFormulario.Select(fc => new ItemCompetenciaViewModel
            {
                IdCompetencia = fc.IdCompetencia,
                Nombre = fc.Competencia.Nombre,
                Descripcion = fc.Competencia.Descripcion,
                Categoria = fc.Competencia.Categoria,
                Ponderacion = fc.Ponderacion,
                // "NOTA FINAL" de la competencia: valor ya calculado y guardado (promedio de sus
                // comportamientos) — ver EvaluacionesController.Guardar y
                // ItemCompetenciaViewModel.Calificacion.
                Calificacion = detallesPorCompetencia.TryGetValue(fc.IdCompetencia, out var d) ? d.Calificacion : null,
                Comentario = detallesPorCompetencia.TryGetValue(fc.IdCompetencia, out var d2) ? d2.Comentario : null,
                // Comportamientos de la competencia (Entregable 13), en el orden del catálogo,
                // con la calificación ya guardada de cada uno (si existe).
                Comportamientos = fc.Competencia.Comportamientos
                    .Where(cp => cp.Activo)
                    .OrderBy(cp => cp.Orden)
                    .Select(cp => new ItemComportamientoViewModel
                    {
                        IdComportamiento = cp.IdComportamiento,
                        Descripcion = cp.Descripcion,
                        Calificacion = detallesPorComportamiento.TryGetValue(cp.IdComportamiento, out var dc) ? dc.Calificacion : null,
                    }).ToList(),
            }).ToList(),
            Indicadores = indicadoresFormulario.Select(fi => new ItemIndicadorViewModel
            {
                IdIndicador = fi.IdIndicador,
                Nombre = fi.Indicador.Nombre,
                Formula = fi.Indicador.Formula,
                Ponderacion = fi.Ponderacion,
                // Meta fija del catálogo (Entregable 12) — de solo lectura, no la del historial de
                // la respuesta (que es solo una copia informativa, ver RespuestaIndicadorDetalle.Meta).
                Meta = fi.Indicador.Meta,
                ResultadoMes = detallesPorIndicador.TryGetValue(fi.IdIndicador, out var di2) ? di2.ResultadoMes : null,
            }).ToList(),
            OportunidadesMejora = respuesta?.OportunidadesMejora,
            Compromisos = respuesta?.Compromisos,
            RevisionCompromisos = respuesta?.RevisionCompromisos,
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarBorrador(DiligenciarEvaluacionViewModel modelo)
        => await Guardar(modelo, enviarDefinitivamente: false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(DiligenciarEvaluacionViewModel modelo)
    {
        // Desde el Entregable 13, la "NOTA FINAL" de cada competencia (Calificacion) ya no la
        // escribe el evaluador directamente: se calcula del promedio de sus comportamientos. Por
        // eso, para el envío definitivo, se exige que TODOS los comportamientos de TODAS las
        // competencias estén calificados (antes se exigía Calificacion de la competencia).
        if (modelo.Items.Any(i => i.Comportamientos.Any(c => c.Calificacion is null)))
        {
            ModelState.AddModelError(string.Empty, "Debes calificar todos los comportamientos de cada competencia antes de enviar la evaluación de forma definitiva.");
            modelo.SoloLectura = false;
            return View("Diligenciar", modelo);
        }
        if (modelo.Indicadores.Any(i => i.ResultadoMes is null))
        {
            ModelState.AddModelError(string.Empty, "Debes registrar el resultado del mes de todos los indicadores de gestión antes de enviar la evaluación de forma definitiva.");
            modelo.SoloLectura = false;
            return View("Diligenciar", modelo);
        }
        return await Guardar(modelo, enviarDefinitivamente: true);
    }

    private async Task<IActionResult> Guardar(DiligenciarEvaluacionViewModel modelo, bool enviarDefinitivamente)
    {
        var codigoEmpleado = User.CodigoEmpleado();

        var asignacion = await _db.AsignacionesEvaluacion
            .FirstOrDefaultAsync(a => a.IdAsignacion == modelo.IdAsignacion);

        if (asignacion is null) return NotFound();
        if (asignacion.CodigoEvaluador != codigoEmpleado) return Forbid();
        if (asignacion.EstaCompletada) return RedirectToAction(nameof(Mis));

        var respuesta = await _db.RespuestasEvaluacion
            .Include(r => r.Detalles)
            .Include(r => r.DetallesComportamientos)
            .Include(r => r.DetallesIndicadores)
            .FirstOrDefaultAsync(r => r.IdAsignacion == asignacion.IdAsignacion);

        if (respuesta is null)
        {
            respuesta = new RespuestaEvaluacion { IdAsignacion = asignacion.IdAsignacion, Estado = Constantes.RespuestaBorrador };
            _db.RespuestasEvaluacion.Add(respuesta);
            await _db.SaveChangesAsync();
        }

        var detallesExistentes = respuesta.Detalles.ToDictionary(d => d.IdCompetencia);
        var detallesComportamientoExistentes = respuesta.DetallesComportamientos.ToDictionary(d => d.IdComportamiento);

        foreach (var item in modelo.Items)
        {
            // Comportamientos (Entregable 13) — se guarda cada calificación individual recibida
            // (puede quedar incompleta en un borrador).
            var comportamientosConValor = item.Comportamientos.Where(c => c.Calificacion.HasValue).ToList();
            foreach (var comp in comportamientosConValor)
            {
                if (detallesComportamientoExistentes.TryGetValue(comp.IdComportamiento, out var detalleComp))
                {
                    detalleComp.Calificacion = comp.Calificacion!.Value;
                }
                else
                {
                    _db.RespuestaComportamientoDetalles.Add(new RespuestaComportamientoDetalle
                    {
                        IdRespuesta = respuesta.IdRespuesta,
                        IdComportamiento = comp.IdComportamiento,
                        Calificacion = comp.Calificacion!.Value,
                    });
                }
            }

            // "NOTA FINAL" de la competencia = promedio de los comportamientos YA calificados
            // (igual que =AVERAGE(...) en el Excel origen, que ignora las celdas sin diligenciar),
            // calculado siempre en el servidor a partir de lo que se acaba de guardar arriba —
            // nunca a partir de un total que venga directamente en el formulario posteado (mismo
            // criterio de autoridad del servidor que la Meta de los indicadores, Entregable 12).
            // Si todavía no se ha calificado ningún comportamiento de esta competencia, no se
            // guarda RespuestaDetalle (la competencia queda pendiente, como antes).
            if (comportamientosConValor.Count == 0) continue;

            var promedioCompetencia = Math.Round(comportamientosConValor.Average(c => c.Calificacion!.Value), 2);

            if (detallesExistentes.TryGetValue(item.IdCompetencia, out var detalle))
            {
                detalle.Calificacion = promedioCompetencia;
                detalle.Comentario = item.Comentario;
            }
            else
            {
                _db.RespuestaDetalles.Add(new RespuestaDetalle
                {
                    IdRespuesta = respuesta.IdRespuesta,
                    IdCompetencia = item.IdCompetencia,
                    Calificacion = promedioCompetencia,
                    Comentario = item.Comentario,
                });
            }
        }

        // Indicadores de gestión (Entregable 11) — se guarda el Resultado del mes igual que se
        // guarda Calificacion para las competencias, solo cuando el evaluador ya lo registró
        // (puede quedar sin diligenciar en un borrador). La Meta es fija por indicador desde el
        // Entregable 12 (ya no la escribe el evaluador): se toma del catálogo
        // (IndicadorGestion.Meta), NUNCA del valor que venga en el formulario posteado, para que
        // no dependa de lo que el navegador haya enviado.
        var idsIndicadoresConResultado = modelo.Indicadores.Where(i => i.ResultadoMes.HasValue).Select(i => i.IdIndicador).ToList();
        var metaPorIndicador = idsIndicadoresConResultado.Count > 0
            ? await _db.IndicadoresGestion
                .Where(i => idsIndicadoresConResultado.Contains(i.IdIndicador))
                .ToDictionaryAsync(i => i.IdIndicador, i => i.Meta)
            : new Dictionary<int, decimal>();

        var detallesIndicadoresExistentes = respuesta.DetallesIndicadores.ToDictionary(d => d.IdIndicador);

        foreach (var item in modelo.Indicadores)
        {
            if (item.ResultadoMes is null) continue;
            var metaCatalogo = metaPorIndicador.GetValueOrDefault(item.IdIndicador);

            if (detallesIndicadoresExistentes.TryGetValue(item.IdIndicador, out var detalleInd))
            {
                detalleInd.Meta = metaCatalogo;
                detalleInd.ResultadoMes = item.ResultadoMes;
            }
            else
            {
                _db.RespuestaIndicadorDetalles.Add(new RespuestaIndicadorDetalle
                {
                    IdRespuesta = respuesta.IdRespuesta,
                    IdIndicador = item.IdIndicador,
                    Meta = metaCatalogo,
                    ResultadoMes = item.ResultadoMes,
                });
            }
        }

        // Sección "COMPROMISOS" (Entregable 11) — texto libre, se guarda tal como se diligencia.
        respuesta.OportunidadesMejora = modelo.OportunidadesMejora;
        respuesta.Compromisos = modelo.Compromisos;
        respuesta.RevisionCompromisos = modelo.RevisionCompromisos;

        if (enviarDefinitivamente)
        {
            respuesta.Estado = Constantes.RespuestaEnviada;
            respuesta.FechaEnvio = DateTime.UtcNow;
            asignacion.Estado = Constantes.AsignacionCompletada;
        }

        await _db.SaveChangesAsync();

        if (enviarDefinitivamente)
        {
            await _auditoria.RegistrarAsync(null, "EnvioEvaluacion", $"Asignación {asignacion.IdAsignacion} (evaluador {codigoEmpleado} -> evaluado {asignacion.CodigoEvaluado})", HttpContext.Connection.RemoteIpAddress?.ToString());
            await _resultados.ConsolidarSiCompletoAsync(asignacion.CodigoEvaluado, asignacion.IdPeriodo);
            TempData["Mensaje"] = "Evaluación enviada correctamente.";
        }
        else
        {
            TempData["Mensaje"] = "Borrador guardado.";
        }

        return RedirectToAction(nameof(Mis));
    }
}
