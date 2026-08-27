namespace Alianzagrafica.Evaluacion180.Web.Models.ViewModels;

public class ResultadoPeriodoViewModel
{
    public string PeriodoNombre { get; set; } = string.Empty;
    public decimal? PromedioAutoevaluacion { get; set; }
    public decimal? PromedioJefe { get; set; }
    public decimal? PromedioAscendente { get; set; }
    public decimal? PromedioGeneral { get; set; }
    public DateTime? FechaConsolidacion { get; set; }
}

public class MisResultadosViewModel
{
    public string NombreEmpleado { get; set; } = string.Empty;
    public List<ResultadoPeriodoViewModel> Historico { get; set; } = new();
}

public class ReporteAreaViewModel
{
    public string Area { get; set; } = string.Empty;
    public int TotalAsignadas { get; set; }
    public int Completadas { get; set; }
    public int Pendientes { get; set; }
    public double PorcentajeAvance => TotalAsignadas == 0 ? 0 : Math.Round(100.0 * Completadas / TotalAsignadas, 1);
}

public class ResultadoEmpleadoViewModel
{
    public int CodigoEmpleado { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string TipoPersonal { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public decimal? PromedioGeneral { get; set; }
}

public class ReportesViewModel
{
    public int? IdPeriodo { get; set; }
    public string? PeriodoNombre { get; set; }
    public List<ReporteAreaViewModel> AvancePorArea { get; set; } = new();
    public List<ResultadoEmpleadoViewModel> Resultados { get; set; } = new();

    /// <summary>
    /// Umbral de alerta (RF-18) para resaltar en rojo a quien tenga un promedio general por
    /// debajo de este valor. Recalibrado en el Entregable 12: antes de la escala 1-5 (3.0),
    /// ahora en % (0-100), alineado con el límite inferior de la banda "Aceptable" de
    /// <see cref="Alianzagrafica.Evaluacion180.Web.Models.Entidades.EscalaCalificacion"/>: se
    /// resalta a quien esté en la banda "Deficiente" (por debajo del 60%).
    /// </summary>
    public decimal UmbralAlerta { get; set; } = 60m;
}
