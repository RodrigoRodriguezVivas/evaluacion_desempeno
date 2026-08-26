using System.Net;
using System.Net.Mail;
using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alianzagrafica.Evaluacion180.Web.Services;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public string CorreoRemitente { get; set; } = "evaluacion180@alianzagrafica.com";
    public string NombreRemitente { get; set; } = "Sistema de Evaluación de Desempeño — Alianzagrafica";
    public string UrlBase { get; set; } = "https://localhost";

    /// <summary>Si es true, los correos no se envían de verdad: solo quedan registrados
    /// en el log de la aplicación. Útil en desarrollo/pruebas sin servidor SMTP disponible.</summary>
    public bool ModoSimulado { get; set; } = true;
}

/// <summary>
/// Envío de notificaciones por correo (RF-10, RF-13) usando System.Net.Mail.SmtpClient,
/// que forma parte del framework base de .NET (sin dependencias NuGet adicionales) y se
/// conecta al servidor SMTP corporativo de Alianzagrafica (sección 8.2 del documento de
/// diseño). Si Smtp:ModoSimulado está activo en la configuración, los correos no se envían:
/// solo se registran en el log, para poder probar el flujo sin un servidor SMTP real.
/// </summary>
public class SmtpNotificacionService : INotificacionService
{
    private readonly AppDbContext _db;
    private readonly SmtpOptions _opciones;
    private readonly ILogger<SmtpNotificacionService> _logger;

    public SmtpNotificacionService(AppDbContext db, IOptions<SmtpOptions> opciones, ILogger<SmtpNotificacionService> logger)
    {
        _db = db;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<int> NotificarAsignacionesAsync(IEnumerable<AsignacionEvaluacion> asignaciones)
    {
        var porEvaluador = asignaciones.GroupBy(a => a.CodigoEvaluador);
        var enviados = 0;

        foreach (var grupo in porEvaluador)
        {
            var evaluador = await _db.Empleados.FirstOrDefaultAsync(e => e.CodigoEmpleado == grupo.Key);
            if (evaluador?.CorreoElectronico is null) continue;

            var asunto = "Evaluación de Desempeño 180° — Tienes evaluaciones pendientes";
            var cuerpo = $"Hola {evaluador.Nombre},\n\n" +
                         $"Tienes {grupo.Count()} evaluación(es) pendiente(s) por diligenciar en el sistema de Evaluación de Desempeño 180° de Alianzagrafica.\n\n" +
                         $"Ingresa a {_opciones.UrlBase}/Evaluaciones/Mis para diligenciarlas.\n\n" +
                         "Este es un mensaje automático, por favor no lo respondas.";

            await EnviarAsync(evaluador.CorreoElectronico, asunto, cuerpo);
            enviados++;
        }

        return enviados;
    }

    public async Task<int> EnviarRecordatoriosAsync(int idPeriodo)
    {
        var pendientes = await _db.AsignacionesEvaluacion
            .Where(a => a.IdPeriodo == idPeriodo && a.Estado != Constantes.AsignacionCompletada)
            .ToListAsync();

        return await NotificarAsignacionesAsync(pendientes);
    }

    // RF-23 — módulo de envío de resultados por correo (y, por separado, por WhatsApp — ver
    // IWhatsAppService/EnvioResultadoService). Aquí solo va el correo: la imagen-resumen ya
    // generada (ResumenImagenService) se adjunta al mensaje.
    public async Task<bool> EnviarResultadoEvaluacionAsync(Empleado empleado, ResultadoConsolidado resultado, byte[] imagenResumenPng)
    {
        if (string.IsNullOrWhiteSpace(empleado.CorreoElectronico))
        {
            _logger.LogWarning("El empleado {Codigo} no tiene correo registrado; no se pudo enviar el resultado.", empleado.CodigoEmpleado);
            return false;
        }

        var asunto = $"Tu resultado de la Evaluación de Desempeño 180° — {resultado.Periodo?.Nombre ?? "periodo actual"}";
        var promedio = resultado.PromedioGeneral?.ToString("0.00") ?? "—";
        var cuerpoHtml =
            $"<p>Hola {System.Net.WebUtility.HtmlEncode(empleado.Nombre)},</p>" +
            $"<p>Ya está disponible el resultado de tu Evaluación de Desempeño 180° correspondiente a " +
            $"<strong>{System.Net.WebUtility.HtmlEncode(resultado.Periodo?.Nombre ?? string.Empty)}</strong>.</p>" +
            $"<p>Tu promedio general fue de <strong>{promedio}</strong> (escala 1 a 5). En la imagen adjunta " +
            $"encuentras el detalle por competencia y por tipo de evaluación (autoevaluación, evaluación del " +
            $"jefe y, cuando aplica, evaluación ascendente).</p>" +
            $"<p>Puedes consultar también el histórico completo de tus resultados ingresando a " +
            $"{_opciones.UrlBase}/Resultados/Mios.</p>" +
            $"<p style=\"color:#6c757d;font-size:12px;\">Este es un mensaje automático — por favor no lo respondas. " +
            $"Información confidencial de uso exclusivo del colaborador.</p>";

        return await EnviarAsync(empleado.CorreoElectronico!, asunto, cuerpoHtml, esHtml: true,
            adjunto: (nombre: "resumen-evaluacion.png", contenido: imagenResumenPng, tipoMime: "image/png"));
    }

    private Task EnviarAsync(string destinatario, string asunto, string cuerpo) =>
        EnviarAsync(destinatario, asunto, cuerpo, esHtml: false, adjunto: null);

    private async Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpo, bool esHtml,
        (string nombre, byte[] contenido, string tipoMime)? adjunto)
    {
        if (_opciones.ModoSimulado || string.IsNullOrWhiteSpace(_opciones.Host))
        {
            _logger.LogInformation(
                "[Correo simulado] Para: {Destinatario} — Asunto: {Asunto} — Adjunto: {Adjunto}\n{Cuerpo}",
                destinatario, asunto, adjunto?.nombre ?? "(ninguno)", cuerpo);
            return true;
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_opciones.CorreoRemitente, _opciones.NombreRemitente),
            Subject = asunto,
            Body = cuerpo,
            IsBodyHtml = esHtml,
        };
        mensaje.To.Add(destinatario);

        Attachment? attachment = null;
        if (adjunto is { } a)
        {
            attachment = new Attachment(new MemoryStream(a.contenido), a.nombre, a.tipoMime);
            mensaje.Attachments.Add(attachment);
        }

        using var cliente = new SmtpClient(_opciones.Host, _opciones.Puerto)
        {
            EnableSsl = _opciones.UsarSsl,
        };
        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
        {
            cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Clave);
        }

        try
        {
            await cliente.SendMailAsync(mensaje);
            return true;
        }
        catch (Exception ex)
        {
            // Un fallo de correo nunca debe tumbar el flujo de negocio (ej. el envío de una evaluación).
            _logger.LogError(ex, "No se pudo enviar el correo a {Destinatario}", destinatario);
            return false;
        }
        finally
        {
            attachment?.Dispose();
        }
    }
}
