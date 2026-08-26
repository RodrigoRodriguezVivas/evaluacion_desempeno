namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface IAsignacionService
{
    /// <summary>
    /// Crea (si no existen) los formularios de evaluación de un periodo — uno de
    /// Autoevaluación y uno de Jefe por cada tipo de personal, más uno de Ascendente
    /// para los tipos que lo permiten — junto con su ponderación de competencias.
    /// Es idempotente: se puede volver a ejecutar sin duplicar formularios.
    /// </summary>
    Task<int> GenerarFormulariosAsync(int idPeriodo);

    /// <summary>
    /// Genera automáticamente las asignaciones evaluador-evaluado del periodo a partir
    /// de la jerarquía vigente en Empleado (RF-09): autoevaluación, jefe → colaborador
    /// y, cuando el tipo de personal del jefe lo permite, evaluación ascendente.
    /// Es idempotente: no duplica asignaciones ya existentes.
    /// </summary>
    Task<int> GenerarAsignacionesAsync(int idPeriodo);
}
