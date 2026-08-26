namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

public class ResultadoConsolidado
{
    public int CodigoEvaluado { get; set; }
    public int IdPeriodo { get; set; }
    public decimal? PromedioAutoevaluacion { get; set; }
    public decimal? PromedioJefe { get; set; }
    public decimal? PromedioAscendente { get; set; }
    public decimal? PromedioGeneral { get; set; }
    public DateTime? FechaConsolidacion { get; set; }

    public Empleado Evaluado { get; set; } = null!;
    public PeriodoEvaluacion Periodo { get; set; } = null!;
}

public class Auditoria
{
    public long IdEvento { get; set; }
    public int? IdUsuario { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime FechaHora { get; set; }
    public string? DireccionIP { get; set; }

    public Usuario? Usuario { get; set; }
}
