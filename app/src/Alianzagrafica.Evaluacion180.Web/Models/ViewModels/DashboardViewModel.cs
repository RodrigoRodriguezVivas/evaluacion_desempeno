namespace Alianzagrafica.Evaluacion180.Web.Models.ViewModels;

public class DashboardViewModel
{
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string TipoPersonal { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public int EvaluacionesPendientes { get; set; }
    public int EvaluacionesCompletadas { get; set; }
    public string? PeriodoActualNombre { get; set; }

    public bool EsAdministrador { get; set; }
}
