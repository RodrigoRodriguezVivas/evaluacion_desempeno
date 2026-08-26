namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Información maestra de empleados. En producción esta tabla se alimenta desde Novasoft
/// (vista de solo lectura o sincronización periódica — ver sección 8.4 del documento de diseño).
/// Mientras se confirma el esquema real de Novasoft, se puebla con la tabla ficticia del script
/// sql/01_esquema_y_datos_ficticios.sql, con el mismo contrato de columnas.
/// </summary>
public class Empleado
{
    public int CodigoEmpleado { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int IdTipoPersonal { get; set; }
    public int? CodigoJefeDirecto { get; set; }
    public string? CorreoElectronico { get; set; }
    public string Estado { get; set; } = Constantes.EstadoEmpleadoActivo;
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaSincronizacion { get; set; }

    public TipoPersonal TipoPersonal { get; set; } = null!;
    public Empleado? JefeDirecto { get; set; }
    public ICollection<Empleado> Colaboradores { get; set; } = new List<Empleado>();
    public Usuario? Usuario { get; set; }

    public bool EsActivo => Estado == Constantes.EstadoEmpleadoActivo;
}
