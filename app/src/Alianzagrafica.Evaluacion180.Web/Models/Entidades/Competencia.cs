namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>Competencia evaluable. IdTipoPersonal nulo = competencia genérica (aplica a todos).</summary>
public class Competencia
{
    public int IdCompetencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdTipoPersonal { get; set; }
    public bool Activa { get; set; } = true;

    public TipoPersonal? TipoPersonal { get; set; }
    public ICollection<FormularioCompetencia> FormularioCompetencias { get; set; } = new List<FormularioCompetencia>();

    public string GrupoDescripcion => IdTipoPersonal is null ? "Genérica (todos)" : TipoPersonal?.Nombre ?? string.Empty;
}
