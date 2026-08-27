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

    // Competencia.Categoria (macro-grupo de ponderación, RF-07 — ver GHU-FOR-007)
    public const string CategoriaOrganizacional = "Organizacional";
    public const string CategoriaDeRol = "DeRol";

    // Clave interna del macro-grupo "Indicadores de Gestión" (Entregable 11 — formato real
    // "EVALUACION DESEMPEÑO Indicadores" de Alianzagrafica). A diferencia de Organizacional/DeRol,
    // no vive en la columna Competencia.Categoria — es un grupo aparte, de IndicadorGestion, pero
    // se usa esta misma clave para identificarlo dentro de AsignacionService/Diligenciar.
    public const string CategoriaIndicadoresGestion = "IndicadoresGestion";

    // Pesos de macro-grupo (RF-07 ampliado en el Entregable 11). Antes de este entregable, los
    // dos grupos existentes (Organizacional/DeRol) se repartían el 100% en partes iguales
    // (50%/50%). Desde el Entregable 11, con el grupo "Indicadores de Gestión" incorporado, los
    // tres macro-grupos tienen pesos FIJOS (no un reparto parejo): Indicadores de Gestión 50%,
    // Organizacional 20%, De Rol 30% — tomados tal cual del formato real "EVALUACION DESEMPEÑO
    // Indicadores". Si un formulario no llega a tener los tres grupos presentes (ej. un tipo de
    // personal sin indicadores configurados todavía), se usa como respaldo el reparto parejo
    // histórico entre los grupos que sí estén presentes, para no dejar un formulario que nunca
    // llegue al 100% — ver AsignacionService.GenerarFormulariosAsync.
    public const decimal PesoIndicadoresGestion = 50m;
    public const decimal PesoOrganizacional = 20m;
    public const decimal PesoDeRol = 30m;

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
