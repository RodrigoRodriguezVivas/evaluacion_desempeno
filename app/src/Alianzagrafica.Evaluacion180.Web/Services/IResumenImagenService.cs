namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface IResumenImagenService
{
    /// <summary>
    /// Genera una imagen PNG con el resumen visual del resultado consolidado de un colaborador
    /// en un periodo (nombre, cargo, promedio general y su banda GHU-FOR-007, promedios por
    /// tipo de evaluación, y el detalle por competencia). Se usa tanto para adjuntar al correo
    /// de resultados como para el envío por WhatsApp.
    /// </summary>
    /// <returns>Los bytes del PNG, o null si el colaborador/periodo no tiene un resultado
    /// consolidado (<c>ResultadoConsolidado</c>) todavía.</returns>
    Task<byte[]?> GenerarAsync(int codigoEvaluado, int idPeriodo);
}
