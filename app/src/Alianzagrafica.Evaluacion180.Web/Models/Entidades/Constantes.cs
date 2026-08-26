namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Valores fijos usados en columnas de tipo "catálogo cerrado" (equivalentes a los
/// CHECK CONSTRAINT del script SQL). Centralizarlos aquí evita cadenas de texto sueltas
/// repetidas por controladores y servicios.
/// </summary>
public static class Constantes
{
    // Empleado.Estado
    public const string EstadoEmpleadoActivo = "Activo";
    public const string EstadoEmpleadoInactivo = "Inactivo";

    // Usuario.TipoAutenticacion
    public const string AutenticacionActiveDirectory = "ActiveDirectory";
    public const string AutenticacionLocal = "Local";

    // PeriodoEvaluacion.Estado
    public const string PeriodoProgramado = "Programado";
    public const string PeriodoAbierto = "Abierto";
    public const string PeriodoCerrado = "Cerrado";

    // AsignacionEvaluacion.TipoRelacion / FormularioEvaluacion.TipoRelacion
    public const string RelacionAutoevaluacion = "Autoevaluacion";
    public const string RelacionJefe = "Jefe";
    public const string RelacionAscendente = "Ascendente";
    public static readonly string[] TiposRelacion = { RelacionAutoevaluacion, RelacionJefe, RelacionAscendente };

    // AsignacionEvaluacion.Estado
    public const string AsignacionProgramada = "Programada";
    public const string AsignacionNotificada = "Notificada";
    public const string AsignacionEnProceso = "EnProceso";
    public const string AsignacionCompletada = "Completada";

    // RespuestaEvaluacion.Estado
    public const string RespuestaBorrador = "Borrador";
    public const string RespuestaEnviada = "Enviada";

    // Nombres de tipos de personal (deben coincidir exactamente con la tabla TipoPersonal)
    public const string TipoDirectivo = "Directivo";
    public const string TipoMandoMedio = "Mando medio";
    public const string TipoAdministrativo = "Administrativo";
    public const string TipoOperario = "Operario";
    public const string TipoAuxiliarPlanta = "Auxiliar de planta";
    public const string TipoConductor = "Conductor";

    // Roles funcionales del sistema (sección 4.2 del documento de diseño)
    public const string RolAdministradorSistema = "Administrador del sistema";
    public const string RolAdministradorGestionHumana = "Administrador de Gestión Humana";
    public const string RolJefeEvaluador = "Jefe / Evaluador";
    public const string RolColaboradorEvaluado = "Colaborador / Evaluado";
    public const string RolConsultaDirectiva = "Consulta directiva";
}
