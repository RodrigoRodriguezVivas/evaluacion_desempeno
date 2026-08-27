namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

public class RespuestaEvaluacion
{
    public int IdRespuesta { get; set; }
    public int IdAsignacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string Estado { get; set; } = Constantes.RespuestaBorrador;

    // Sección "COMPROMISOS" (Entregable 11 — formato real "EVALUACION DESEMPEÑO Indicadores"):
    // tres campos de texto libre diligenciados entre evaluador y evaluado, tal como están en el
    // Excel origen (ver Constantes o Diligenciar.cshtml para los textos de ayuda exactos).
    public string? OportunidadesMejora { get; set; }
    public string? Compromisos { get; set; }
    public string? RevisionCompromisos { get; set; }

    public AsignacionEvaluacion Asignacion { get; set; } = null!;
    public ICollection<RespuestaDetalle> Detalles { get; set; } = new List<RespuestaDetalle>();
    public ICollection<RespuestaIndicadorDetalle> DetallesIndicadores { get; set; } = new List<RespuestaIndicadorDetalle>();
}

public class RespuestaDetalle
{
    public int IdRespuesta { get; set; }
    public int IdCompetencia { get; set; }

    /// <summary>Calificación del evaluador, en puntos porcentuales, 0-100 (Entregable 12 — antes
    /// era una escala de 1 a 5; ver <see cref="Entidades.EscalaCalificacion"/> para la
    /// clasificación cualitativa Deficiente/Aceptable/Bueno/Sobresaliente equivalente).</summary>
    public decimal Calificacion { get; set; }
    public string? Comentario { get; set; }

    public RespuestaEvaluacion Respuesta { get; set; } = null!;
    public Competencia Competencia { get; set; } = null!;
}

/// <summary>Respuesta de un indicador de gestión dentro de una evaluación (Entregable 11),
/// análoga a <see cref="RespuestaDetalle"/> pero con Meta/Resultado del mes en vez de una
/// calificación directa — ver <see cref="Entidades.IndicadorGestion"/>.</summary>
public class RespuestaIndicadorDetalle
{
    public int IdRespuesta { get; set; }
    public int IdIndicador { get; set; }

    /// <summary>Copia (snapshot) de <see cref="IndicadorGestion.Meta"/> al momento de guardar la
    /// respuesta, en puntos porcentuales (90 = 90%) — informativa; no participa en el cálculo de
    /// la nota. Desde el Entregable 12 la Meta es un valor fijo del catálogo que el evaluador ya
    /// no escribe: el controlador la toma de <see cref="IndicadorGestion"/> al guardar.</summary>
    public decimal? Meta { get; set; }

    /// <summary>Resultado real del periodo, en puntos porcentuales (75 = 75%) — es el valor que
    /// se usa para calcular el aporte del indicador a la nota final (ver
    /// <see cref="Services.ResultadoService"/>).</summary>
    public decimal? ResultadoMes { get; set; }

    public RespuestaEvaluacion Respuesta { get; set; } = null!;
    public IndicadorGestion Indicador { get; set; } = null!;
}
