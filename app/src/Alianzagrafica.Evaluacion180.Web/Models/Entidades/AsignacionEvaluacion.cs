namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>Asignación evaluador → evaluado para un periodo y tipo de relación (RF-09).</summary>
public class AsignacionEvaluacion
{
    public int IdAsignacion { get; set; }
    public int IdPeriodo { get; set; }
    public int CodigoEvaluador { get; set; }
    public int CodigoEvaluado { get; set; }
    public string TipoRelacion { get; set; } = string.Empty;
    public int? IdFormulario { get; set; }
    public string Estado { get; set; } = Constantes.AsignacionProgramada;

    public PeriodoEvaluacion Periodo { get; set; } = null!;
    public Empleado Evaluador { get; set; } = null!;
    public Empleado Evaluado { get; set; } = null!;
    public FormularioEvaluacion? Formulario { get; set; }
    public RespuestaEvaluacion? Respuesta { get; set; }

    public bool EstaCompletada => Estado == Constantes.AsignacionCompletada;
}
