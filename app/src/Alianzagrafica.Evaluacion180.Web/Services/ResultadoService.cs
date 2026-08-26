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

        decimal? PromedioPorRelacion(string tipoRelacion)
        {
            var idsAsigRelacion = asignaciones.Where(a => a.TipoRelacion == tipoRelacion).Select(a => a.IdAsignacion).ToHashSet();
            var idsRespuestaRelacion = respuestas.Where(r => idsAsigRelacion.Contains(r.IdAsignacion)).Select(r => r.IdRespuesta).ToHashSet();
            var calificaciones = detalles.Where(d => idsRespuestaRelacion.Contains(d.IdRespuesta)).Select(d => (decimal)d.Calificacion).ToList();
            return calificaciones.Count > 0 ? Math.Round(calificaciones.Average(), 2) : (decimal?)null;
        }

        var promedioAuto = PromedioPorRelacion(Constantes.RelacionAutoevaluacion);
        var promedioJefe = PromedioPorRelacion(Constantes.RelacionJefe);
        var promedioAscendente = PromedioPorRelacion(Constantes.RelacionAscendente);

        var todasLasCalificaciones = detalles.Select(d => (decimal)d.Calificacion).ToList();
        var promedioGeneral = todasLasCalificaciones.Count > 0 ? Math.Round(todasLasCalificaciones.Average(), 2) : (decimal?)null;

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
