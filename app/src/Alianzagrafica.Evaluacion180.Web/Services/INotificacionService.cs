using Alianzagrafica.Evaluacion180.Web.Models.Entidades;

namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface INotificacionService
{
    /// <summary>Notifica por correo la apertura de evaluaciones pendientes (RF-10).</summary>
    Task<int> NotificarAsignacionesAsync(IEnumerable<AsignacionEvaluacion> asignaciones);

    /// <summary>Envía recordatorio a evaluadores con evaluaciones pendientes de un periodo (RF-13).</summary>
    Task<int> EnviarRecordatoriosAsync(int idPeriodo);

    /// <summary>Envía al colaborador evaluado el resultado de su evaluación por correo, con la
    /// imagen-resumen adjunta (RF-23 — módulo de envío de resultados). Devuelve false sin
    /// lanzar excepción si el empleado no tiene correo registrado.</summary>
    Task<bool> EnviarResultadoEvaluacionAsync(Empleado empleado, ResultadoConsolidado resultado, byte[] imagenResumenPng);
}
