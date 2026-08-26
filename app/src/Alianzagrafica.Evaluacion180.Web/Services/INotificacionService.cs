using Alianzagrafica.Evaluacion180.Web.Models.Entidades;

namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface INotificacionService
{
    /// <summary>Notifica por correo la apertura de evaluaciones pendientes (RF-10).</summary>
    Task<int> NotificarAsignacionesAsync(IEnumerable<AsignacionEvaluacion> asignaciones);

    /// <summary>Envía recordatorio a evaluadores con evaluaciones pendientes de un periodo (RF-13).</summary>
    Task<int> EnviarRecordatoriosAsync(int idPeriodo);
}
