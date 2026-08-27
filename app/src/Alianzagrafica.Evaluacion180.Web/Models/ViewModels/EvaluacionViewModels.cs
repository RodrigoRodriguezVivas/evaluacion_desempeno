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
    public string? Categoria { get; set; }
    public decimal Ponderacion { get; set; }

    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    public byte? Calificacion { get; set; }

    [StringLength(500)]
    public string? Comentario { get; set; }
}

/// <summary>Ítem del macro-grupo "Indicadores de Gestión" (Entregable 11 — formato real
/// "EVALUACION DESEMPEÑO Indicadores"). A diferencia de <see cref="ItemCompetenciaViewModel"/>,
/// no se califica de 1 a 5: se captura una Meta y un Resultado del mes, ambos en puntos
/// porcentuales (ej. 90 = 90%).</summary>
public class ItemIndicadorViewModel
{
    public int IdIndicador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Formula { get; set; }
    public decimal Ponderacion { get; set; }

    [Range(0, 500, ErrorMessage = "La meta debe ser un valor en % (ej. 90 para 90%).")]
    public decimal? Meta { get; set; }

    [Range(0, 500, ErrorMessage = "El resultado del mes debe ser un valor en % (ej. 75 para 75%).")]
    public decimal? ResultadoMes { get; set; }
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
    public List<ItemIndicadorViewModel> Indicadores { get; set; } = new();

    // Sección "COMPROMISOS" (Entregable 11) — tres campos de texto libre, tal como están en el
    // Excel origen "EVALUACION DESEMPEÑO Indicadores".
    [StringLength(2000)]
    public string? OportunidadesMejora { get; set; }
    [StringLength(2000)]
    public string? Compromisos { get; set; }
    [StringLength(2000)]
    public string? RevisionCompromisos { get; set; }
}
