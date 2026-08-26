using System.ComponentModel.DataAnnotations;

namespace Alianzagrafica.Evaluacion180.Web.Models.ViewModels;

public class AsignacionResumenViewModel
{
    public int IdAsignacion { get; set; }
    public string NombreEvaluado { get; set; } = string.Empty;
    public string CargoEvaluado { get; set; } = string.Empty;
    public string TipoRelacion { get; set; } = string.Empty;
    public string TipoRelacionTexto => TipoRelacion switch
    {
        Entidades.Constantes.RelacionAutoevaluacion => "Autoevaluación",
        Entidades.Constantes.RelacionJefe => "Evaluación a colaborador",
        Entidades.Constantes.RelacionAscendente => "Evaluación ascendente (a mi jefe)",
        _ => TipoRelacion,
    };
    public string Estado { get; set; } = string.Empty;
    public string PeriodoNombre { get; set; } = string.Empty;
    public DateTime PeriodoFechaCierre { get; set; }
}

public class MisEvaluacionesViewModel
{
    public string? PeriodoActualNombre { get; set; }
    public List<AsignacionResumenViewModel> Pendientes { get; set; } = new();
    public List<AsignacionResumenViewModel> Completadas { get; set; } = new();
}

public class ItemCompetenciaViewModel
{
    public int IdCompetencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Ponderacion { get; set; }

    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    public byte? Calificacion { get; set; }

    [StringLength(500)]
    public string? Comentario { get; set; }
}

public class DiligenciarEvaluacionViewModel
{
    public int IdAsignacion { get; set; }
    public string NombreEvaluado { get; set; } = string.Empty;
    public string CargoEvaluado { get; set; } = string.Empty;
    public string TipoRelacionTexto { get; set; } = string.Empty;
    public string NombreFormulario { get; set; } = string.Empty;
    public string PeriodoNombre { get; set; } = string.Empty;
    public bool SoloLectura { get; set; }
    public List<ItemCompetenciaViewModel> Items { get; set; } = new();
}
