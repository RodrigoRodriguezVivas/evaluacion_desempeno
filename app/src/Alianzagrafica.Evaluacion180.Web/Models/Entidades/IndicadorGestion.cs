namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Indicador de gestión (KPI) evaluable dentro del macro-grupo "Indicadores de Gestión"
/// (RF-07 ampliado en el Entregable 11 — formato real "EVALUACION DESEMPEÑO Indicadores" de
/// Alianzagrafica). A diferencia de una <see cref="Competencia"/> (que el evaluador califica de
/// 1 a 5), un indicador se mide con una Meta y un Resultado del mes, ambos en puntos porcentuales
/// (ej. 90 = 90%) — ver <see cref="FormularioIndicador"/> y <see cref="RespuestaIndicadorDetalle"/>.
/// IdTipoPersonal nulo = indicador genérico (aplica a todos los tipos de personal).
/// </summary>
public class IndicadorGestion
{
    public int IdIndicador { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción de la fórmula/definición del indicador (columna "FORMULA" del Excel origen).</summary>
    public string? Formula { get; set; }

    /// <summary>
    /// Peso del indicador DENTRO del grupo "Indicadores de Gestión" (no del total del
    /// formulario), en puntos porcentuales (ej. 33.33 = 33.33% del grupo). Tomado tal cual del
    /// Excel origen — la suma de los indicadores de un mismo tipo de personal puede no ser
    /// exactamente 100 (ver Entregable 11, advertencia de ponderación: en el Excel original, 4
    /// indicadores quedaron cada uno al 33.33%, sumando ~133%, y se dejó así a propósito). El
    /// peso absoluto de cada indicador dentro de un formulario =
    /// Constantes.PesoIndicadoresGestion × (Ponderacion / 100) — ver
    /// <see cref="Services.AsignacionService"/>.
    /// </summary>
    public decimal Ponderacion { get; set; }

    public int? IdTipoPersonal { get; set; }
    public bool Activa { get; set; } = true;

    public TipoPersonal? TipoPersonal { get; set; }
    public ICollection<FormularioIndicador> FormularioIndicadores { get; set; } = new List<FormularioIndicador>();

    public string GrupoDescripcion => IdTipoPersonal is null ? "Genérico (todos)" : TipoPersonal?.Nombre ?? string.Empty;
}
