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

/// <summary>Ítem de un comportamiento individual dentro de una competencia (Entregable 13 —
/// columna "COMPORTAMIENTOS" del Excel origen "EVALUACION DESEMPEÑO_Evaluaciones"). El evaluador
/// califica cada comportamiento por separado; ver <see cref="ItemCompetenciaViewModel.Calificacion"/>
/// para cómo se combinan.</summary>
public class ItemComportamientoViewModel
{
    public int IdComportamiento { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0% y 100%.")]
    public decimal? Calificacion { get; set; }
}

public class ItemCompetenciaViewModel
{
    public int IdCompetencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Categoria { get; set; }
    public decimal Ponderacion { get; set; }

    /// <summary>Comportamientos observables de esta competencia (Entregable 13). El evaluador
    /// califica cada uno; esta lista siempre viene poblada desde el catálogo
    /// (<see cref="Entidades.Comportamiento"/>) al mostrar el formulario.</summary>
    public List<ItemComportamientoViewModel> Comportamientos { get; set; } = new();

    /// <summary>"NOTA FINAL" de la competencia: el promedio de <see cref="Comportamientos"/> que
    /// el evaluador ya calificó. Desde el Entregable 13 este valor NO se diligencia directamente
    /// (no hay un &lt;input&gt; que lo edite) — se calcula en el servidor a partir de los
    /// comportamientos posteados (EvaluacionesController.Guardar) y aquí solo se usa para
    /// mostrarlo/recalcular subtotales, igual que antes.</summary>
    public decimal? Calificacion { get; set; }

    [StringLength(500)]
    public string? Comentario { get; set; }
}

/// <summary>Ítem del macro-grupo "Indicadores de Gestión" (Entregable 11 — formato real
/// "EVALUACION DESEMPEÑO Indicadores"). Se mide con una Meta y un Resultado del mes, ambos en
/// puntos porcentuales (ej. 90 = 90%) — la Meta es un valor FIJO del catálogo
/// (<see cref="Entidades.IndicadorGestion.Meta"/>, Entregable 12), de solo lectura en el
/// formulario; el evaluador solo diligencia el Resultado del mes.</summary>
public class ItemIndicadorViewModel
{
    public int IdIndicador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Formula { get; set; }
    public decimal Ponderacion { get; set; }

    /// <summary>Meta fija del indicador (solo lectura en el formulario) — poblada desde el
    /// catálogo (<see cref="Entidades.IndicadorGestion.Meta"/>), no editada por el evaluador.</summary>
    public decimal Meta { get; set; }

    [Range(0, 100, ErrorMessage = "El resultado del mes debe ser un valor en %, entre 0 y 100 (ej. 75 para 75%).")]
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
