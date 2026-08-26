namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>Catálogo de tipos de personal (Directivo, Mando medio, Administrativo, Operario, Auxiliar de planta).</summary>
public class TipoPersonal
{
    public int IdTipoPersonal { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool PermiteEvaluacionAscendente { get; set; }

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    public ICollection<Competencia> Competencias { get; set; } = new List<Competencia>();
    public ICollection<FormularioEvaluacion> Formularios { get; set; } = new List<FormularioEvaluacion>();
}
