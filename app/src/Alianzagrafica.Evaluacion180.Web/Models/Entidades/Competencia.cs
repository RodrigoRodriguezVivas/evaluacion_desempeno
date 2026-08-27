namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>Competencia evaluable. IdTipoPersonal nulo = competencia genérica (aplica a todos).</summary>
public class Competencia
{
    public int IdCompetencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdTipoPersonal { get; set; }
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Macro-grupo de ponderación al que pertenece esta competencia dentro de un formulario
    /// (RF-07): <see cref="Constantes.CategoriaOrganizacional"/> o
    /// <see cref="Constantes.CategoriaDeRol"/>. Null = sin categoría (la competencia se pondera
    /// como un grupo propio de 1, comportamiento histórico de reparto parejo). El peso de cada
    /// categoría presente en un formulario se reparte 100%/N entre las categorías presentes, y
    /// dentro de cada una en partes iguales entre sus competencias — ver
    /// <see cref="Services.AsignacionService"/>. Basado en el formato real GHU-FOR-007 de
    /// Alianzagrafica: "EVALUACION DE COMPETENCIAS ORGANIZACIONALES" y "EVALUACION DE
    /// COMPETENCIAS DE ROL", cada una con peso del 50% (ver Entregable 5 / ajuste posterior).
    /// </summary>
    public string? Categoria { get; set; }

    public TipoPersonal? TipoPersonal { get; set; }
    public ICollection<FormularioCompetencia> FormularioCompetencias { get; set; } = new List<FormularioCompetencia>();

    /// <summary>Comportamientos observables que componen esta competencia (Entregable 13 —
    /// columna "COMPORTAMIENTOS" del Excel origen). La "NOTA FINAL" de la competencia es el
    /// promedio de estos comportamientos, no un valor calificado directamente — ver
    /// <see cref="Comportamiento"/> y <see cref="RespuestaDetalle.Calificacion"/>.</summary>
    public ICollection<Comportamiento> Comportamientos { get; set; } = new List<Comportamiento>();

    public string GrupoDescripcion => IdTipoPersonal is null ? "Genérica (todos)" : TipoPersonal?.Nombre ?? string.Empty;
}
