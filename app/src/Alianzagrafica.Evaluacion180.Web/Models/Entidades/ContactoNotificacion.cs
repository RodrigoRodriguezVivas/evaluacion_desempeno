namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Datos de contacto para notificaciones (hoy: número de WhatsApp) que el sistema de
/// evaluación mantiene por su cuenta, SEPARADOS de la tabla <see cref="Empleado"/>.
///
/// Esto es deliberado: Empleado es un espejo de solo lectura de Novasoft (sección 3.2 y 8.4
/// del documento de diseño — "el sistema de evaluación NUNCA edita datos maestros de
/// empleados"), y no hay ninguna garantía de que Novasoft tenga, o mantenga actualizado, un
/// número de WhatsApp del colaborador. Guardarlo aparte permite que Gestión Humana lo
/// administre desde este mismo sistema (pantalla de Empleados) sin tocar ni depender del
/// esquema de Novasoft, y sin que una resincronización de Empleado lo pueda borrar.
/// </summary>
public class ContactoNotificacion
{
    public int CodigoEmpleado { get; set; }

    /// <summary>Número de WhatsApp del colaborador. Se recomienda formato internacional
    /// (ej. +573001234567); si se guarda sin indicativo de país, se asume Colombia (+57) al
    /// momento de enviar — ver <c>WhatsAppNotificacionService.NormalizarNumero</c>.</summary>
    public string? TelefonoWhatsApp { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public Empleado Empleado { get; set; } = null!;
}
