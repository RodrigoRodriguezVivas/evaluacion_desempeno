namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

public class RespuestaEvaluacion
{
    public int IdRespuesta { get; set; }
    public int IdAsignacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string Estado { get; set; } = Constantes.RespuestaBorrador;

    public AsignacionEvaluacion Asignacion { get; set; } = null!;
    public ICollection<RespuestaDetalle> Detalles { get; set; } = new List<RespuestaDetalle>();
}

public class RespuestaDetalle
{
    public int IdRespuesta { get; set; }
    public int IdCompetencia { get; set; }
    public byte Calificacion { get; set; }
    public string? Comentario { get; set; }

    public RespuestaEvaluacion Respuesta { get; set; } = null!;
    public Competencia Competencia { get; set; } = null!;
}
