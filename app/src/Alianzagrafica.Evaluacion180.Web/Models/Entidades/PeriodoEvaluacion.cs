namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

public class PeriodoEvaluacion
{
    public int IdPeriodo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }
    public string Estado { get; set; } = Constantes.PeriodoProgramado;

    public ICollection<FormularioEvaluacion> Formularios { get; set; } = new List<FormularioEvaluacion>();
    public ICollection<AsignacionEvaluacion> Asignaciones { get; set; } = new List<AsignacionEvaluacion>();
}
