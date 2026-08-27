using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Services;

public class ResultadoService : IResultadoService
{
    private readonly AppDbContext _db;

    public ResultadoService(AppDbContext db) => _db = db;

    public async Task<bool> ConsolidarSiCompletoAsync(int codigoEvaluado, int idPeriodo)
    {
        var asignaciones = await _db.AsignacionesEvaluacion
            .Where(a => a.CodigoEvaluado == codigoEvaluado && a.IdPeriodo == idPeriodo)
            .ToListAsync();

        if (asignaciones.Count == 0 || asignaciones.Any(a => a.Estado != Constantes.AsignacionCompletada))
            return false;

        var idsAsignacion = asignaciones.Select(a => a.IdAsignacion).ToList();

        var respuestas = await _db.RespuestasEvaluacion
            .Where(r => idsAsignacion.Contains(r.IdAsignacion))
            .ToListAsync();

        var idsRespuesta = respuestas.Select(r => r.IdRespuesta).ToList();

        var detalles = await _db.RespuestaDetalles
            .Where(d => idsRespuesta.Contains(d.IdRespuesta))
            .ToListAsync();

        // Ponderación real de cada competencia dentro del formulario que le correspondió a cada
        // asignación (RF-06/RF-07 — macro-grupos "Organizacional"/"De Rol" al 50%/50%, o el
        // reparto parejo 100%/N si el formulario no usa categorías — ver
        // AsignacionService.GenerarFormulariosAsync). El promedio se calcula ponderado por esa
        // ponderación, no como promedio simple de calificaciones: de lo contrario una competencia
        // con más peso (ej. "Trabajo en equipo" del Conductor, ~16.7% de "De Rol") contaría igual
        // que una con menos peso, contradiciendo el modelo de ponderación configurado.
        var idsFormulario = asignaciones.Where(a => a.IdFormulario.HasValue).Select(a => a.IdFormulario!.Value).Distinct().ToList();
        var ponderaciones = await _db.FormularioCompetencias
            .Where(fc => idsFormulario.Contains(fc.IdFormulario))
            .ToDictionaryAsync(fc => (fc.IdFormulario, fc.IdCompetencia), fc => fc.Ponderacion);

        var idFormularioPorAsignacion = asignaciones.ToDictionary(a => a.IdAsignacion, a => a.IdFormulario);
        var idFormularioPorRespuesta = respuestas.ToDictionary(r => r.IdRespuesta, r => idFormularioPorAsignacion.GetValueOrDefault(r.IdAsignacion));

        decimal ObtenerPonderacion(RespuestaDetalle d)
        {
            var idFormulario = idFormularioPorRespuesta.GetValueOrDefault(d.IdRespuesta);
            if (idFormulario is int idF && ponderaciones.TryGetValue((idF, d.IdCompetencia), out var p))
                return p;
            return 1m; // sin ponderación configurada: cuenta como peso igual al resto (fallback defensivo)
        }

        decimal? PromedioPonderado(IEnumerable<RespuestaDetalle> items)
        {
            var lista = items.Select(d => (Calificacion: (decimal)d.Calificacion, Peso: ObtenerPonderacion(d))).ToList();
            var sumaPesos = lista.Sum(x => x.Peso);
            if (lista.Count == 0 || sumaPesos <= 0) return null;
            return Math.Round(lista.Sum(x => x.Calificacion * x.Peso) / sumaPesos, 2);
        }

        decimal? PromedioPorRelacion(string tipoRelacion)
        {
            var idsAsigRelacion = asignaciones.Where(a => a.TipoRelacion == tipoRelacion).Select(a => a.IdAsignacion).ToHashSet();
            var idsRespuestaRelacion = respuestas.Where(r => idsAsigRelacion.Contains(r.IdAsignacion)).Select(r => r.IdRespuesta).ToHashSet();
            return PromedioPonderado(detalles.Where(d => idsRespuestaRelacion.Contains(d.IdRespuesta)));
        }

        var promedioAuto = PromedioPorRelacion(Constantes.RelacionAutoevaluacion);
        var promedioJefe = PromedioPorRelacion(Constantes.RelacionJefe);
        var promedioAscendente = PromedioPorRelacion(Constantes.RelacionAscendente);

        var promedioGeneral = PromedioPonderado(detalles);

        var resultado = await _db.ResultadosConsolidados
            .FirstOrDefaultAsync(r => r.CodigoEvaluado == codigoEvaluado && r.IdPeriodo == idPeriodo);

        if (resultado is null)
        {
            resultado = new ResultadoConsolidado { CodigoEvaluado = codigoEvaluado, IdPeriodo = idPeriodo };
            _db.ResultadosConsolidados.Add(resultado);
        }

        resultado.PromedioAutoevaluacion = promedioAuto;
        resultado.PromedioJefe = promedioJefe;
        resultado.PromedioAscendente = promedioAscendente;
        resultado.PromedioGeneral = promedioGeneral;
        resultado.FechaConsolidacion = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
