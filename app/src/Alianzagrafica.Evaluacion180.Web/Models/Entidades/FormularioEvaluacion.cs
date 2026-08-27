namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

public class FormularioEvaluacion
{
    public int IdFormulario { get; set; }
    public int IdPeriodo { get; set; }
    public string TipoRelacion { get; set; } = string.Empty;
    public int IdTipoPersonal { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public PeriodoEvaluacion Periodo { get; set; } = null!;
    public TipoPersonal TipoPersonal { get; set; } = null!;
    public ICollection<FormularioCompetencia> FormularioCompetencias { get; set; } = new List<FormularioCompetencia>();
    public ICollection<FormularioIndicador> FormularioIndicadores { get; set; } = new List<FormularioIndicador>();
    public ICollection<AsignacionEvaluacion> Asignaciones { get; set; } = new List<AsignacionEvaluacion>();
}

public class FormularioCompetencia
{
    public int IdFormulario { get; set; }
    public int IdCompetencia { get; set; }
    public decimal Ponderacion { get; set; }

    public FormularioEvaluacion Formulario { get; set; } = null!;
    public Competencia Competencia { get; set; } = null!;
}

/// <summary>N:M entre FormularioEvaluacion e IndicadorGestion (Entregable 11), análoga a
/// <see cref="FormularioCompetencia"/> pero para el macro-grupo "Indicadores de Gestión".</summary>
public class FormularioIndicador
{
    public int IdFormulario { get; set; }
    public int IdIndicador { get; set; }

    /// <summary>Peso absoluto (0-100) del indicador dentro del formulario completo — ver
    /// <see cref="IndicadorGestion.Ponderacion"/> para cómo se calcula.</summary>
    public decimal Ponderacion { get; set; }

    public FormularioEvaluacion Formulario { get; set; } = null!;
    public IndicadorGestion Indicador { get; set; } = null!;
}
