namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface IResultadoService
{
    /// <summary>
    /// Si todas las asignaciones de un evaluado en un periodo están en estado
    /// "Completada", calcula los promedios por tipo de relación y el promedio
    /// general, y actualiza (o crea) su ResultadoConsolidado (RF-15).
    /// Devuelve true si quedó consolidado en esta llamada.
    /// </summary>
    Task<bool> ConsolidarSiCompletoAsync(int codigoEvaluado, int idPeriodo);
}
