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

        // Indicadores de gestión (Entregable 11 — macro-grupo "Indicadores de Gestión", formato
        // real "EVALUACION DESEMPEÑO Indicadores"). No se califican de 1 a 5 sino con un
        // Resultado del mes en % — para combinarlos en la misma nota final que las competencias
        // se convierten a un equivalente en escala 1-5 (ver EquivalenciaUnoACinco más abajo).
        var detallesIndicadores = await _db.RespuestaIndicadorDetalles
            .Where(d => idsRespuesta.Contains(d.IdRespuesta))
            .ToListAsync();

        // Ponderación real de cada competencia/indicador dentro del formulario que le
        // correspondió a cada asignación (RF-06/RF-07 — macro-grupos "Indicadores de Gestión"
        // 50% / "Organizacional" 20% / "De Rol" 30% desde el Entregable 11, o el reparto parejo
        // 100%/N si el formulario no llega a tener los tres grupos — ver
        // AsignacionService.GenerarFormulariosAsync). El promedio se calcula ponderado por esa
        // ponderación, no como promedio simple: de lo contrario un ítem con más peso contaría
        // igual que uno con menos peso, contradiciendo el modelo de ponderación configurado.
        var idsFormulario = asignaciones.Where(a => a.IdFormulario.HasValue).Select(a => a.IdFormulario!.Value).Distinct().ToList();
        var ponderaciones = await _db.FormularioCompetencias
            .Where(fc => idsFormulario.Contains(fc.IdFormulario))
            .ToDictionaryAsync(fc => (fc.IdFormulario, fc.IdCompetencia), fc => fc.Ponderacion);
        var ponderacionesIndicadores = await _db.FormularioIndicadores
            .Where(fi => idsFormulario.Contains(fi.IdFormulario))
            .ToDictionaryAsync(fi => (fi.IdFormulario, fi.IdIndicador), fi => fi.Ponderacion);

        var idFormularioPorAsignacion = asignaciones.ToDictionary(a => a.IdAsignacion, a => a.IdFormulario);
        var idFormularioPorRespuesta = respuestas.ToDictionary(r => r.IdRespuesta, r => idFormularioPorAsignacion.GetValueOrDefault(r.IdAsignacion));

        decimal ObtenerPonderacion(RespuestaDetalle d)
        {
            var idFormulario = idFormularioPorRespuesta.GetValueOrDefault(d.IdRespuesta);
            if (idFormulario is int idF && ponderaciones.TryGetValue((idF, d.IdCompetencia), out var p))
                return p;
            return 1m; // sin ponderación configurada: cuenta como peso igual al resto (fallback defensivo)
        }

        decimal ObtenerPonderacionIndicador(RespuestaIndicadorDetalle d)
        {
            var idFormulario = idFormularioPorRespuesta.GetValueOrDefault(d.IdRespuesta);
            if (idFormulario is int idF && ponderacionesIndicadores.TryGetValue((idF, d.IdIndicador), out var p))
                return p;
            return 1m; // sin ponderación configurada: cuenta como peso igual al resto (fallback defensivo)
        }

        // Convierte el % de cumplimiento de un indicador de gestión a un equivalente en la
        // escala 1-5 que usan las competencias (Entregable 11, decisión de diseño confirmada con
        // el usuario): 0% -> 1, 100% -> 5, de forma lineal, limitado a [1,5] para no romper la
        // escala del resto de la nota aunque el indicador supere el 100%.
        decimal EquivalenciaUnoACinco(decimal resultadoMesPuntos) =>
            Math.Clamp(1m + 4m * (resultadoMesPuntos / 100m), 1m, 5m);

        decimal? PromedioPonderado(IEnumerable<RespuestaDetalle> items, IEnumerable<RespuestaIndicadorDetalle> itemsIndicadores)
        {
            var listaCompetencias = items.Select(d => (Valor: (decimal)d.Calificacion, Peso: ObtenerPonderacion(d)));
            var listaIndicadores = itemsIndicadores
                .Where(d => d.ResultadoMes.HasValue)
                .Select(d => (Valor: EquivalenciaUnoACinco(d.ResultadoMes!.Value), Peso: ObtenerPonderacionIndicador(d)));
            var lista = listaCompetencias.Concat(listaIndicadores).ToList();
            var sumaPesos = lista.Sum(x => x.Peso);
            if (lista.Count == 0 || sumaPesos <= 0) return null;
            return Math.Round(lista.Sum(x => x.Valor * x.Peso) / sumaPesos, 2);
        }

        decimal? PromedioPorRelacion(string tipoRelacion)
        {
            var idsAsigRelacion = asignaciones.Where(a => a.TipoRelacion == tipoRelacion).Select(a => a.IdAsignacion).ToHashSet();
            var idsRespuestaRelacion = respuestas.Where(r => idsAsigRelacion.Contains(r.IdAsignacion)).Select(r => r.IdRespuesta).ToHashSet();
            return PromedioPonderado(
                detalles.Where(d => idsRespuestaRelacion.Contains(d.IdRespuesta)),
                detallesIndicadores.Where(d => idsRespuestaRelacion.Contains(d.IdRespuesta)));
        }

        var promedioAuto = PromedioPorRelacion(Constantes.RelacionAutoevaluacion);
        var promedioJefe = PromedioPorRelacion(Constantes.RelacionJefe);
        var promedioAscendente = PromedioPorRelacion(Constantes.RelacionAscendente);

        var promedioGeneral = PromedioPonderado(detalles, detallesIndicadores);

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
