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
