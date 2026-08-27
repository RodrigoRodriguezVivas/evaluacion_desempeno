namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Comportamiento observable dentro de una <see cref="Competencia"/> (columna "COMPORTAMIENTOS"
/// del formato real "EVALUACION DESEMPEÑO_Evaluaciones" de Alianzagrafica — Entregable 13, a
/// partir del Excel adjunto por el usuario). El evaluador califica cada comportamiento
/// individualmente en % (0-100, columna "NOTA INDIVIDUAL" del Excel); la "NOTA FINAL" de la
/// competencia (<see cref="RespuestaDetalle.Calificacion"/>) es el promedio simple de los
/// comportamientos respondidos de esa competencia — calculado en el servidor
/// (EvaluacionesController.Guardar), nunca a partir de un total que venga directamente del
/// formulario posteado, siguiendo el mismo criterio de autoridad del servidor que
/// <see cref="IndicadorGestion.Meta"/> desde el Entregable 12.
/// </summary>
public class Comportamiento
{
    public int IdComportamiento { get; set; }
    public int IdCompetencia { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Orden de despliegue dentro de su competencia (tal como aparecen las filas en la
    /// columna "COMPORTAMIENTOS" del Excel origen).</summary>
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public Competencia Competencia { get; set; } = null!;
}
