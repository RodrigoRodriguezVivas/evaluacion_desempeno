namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Token de un solo propósito que expone TEMPORALMENTE, por una URL pública sin autenticar,
/// la imagen-resumen de un resultado de evaluación ya generada — necesario porque las APIs de
/// WhatsApp Business (Twilio, Meta Cloud API) descargan la imagen a enviar desde una URL
/// accesible por sus propios servidores; no aceptan un archivo adjunto directo desde nuestro
/// backend.
///
/// La imagen se genera UNA sola vez, en el momento de enviar (<see cref="ImagenPng"/> guarda
/// esos bytes tal cual), y el token expira rápido (<see cref="FechaExpiracion"/>, ver
/// <c>EnvioResultadoService</c>) — así la ventana de exposición pública es mínima y el
/// contenido no cambia aunque el resultado consolidado cambie después.
///
/// Pendiente de mejora para producción (documentado también en el README): esta tabla no se
/// purga automáticamente; los tokens vencidos simplemente dejan de servir contenido (el
/// endpoint valida <see cref="FechaExpiracion"/>) pero sus filas quedan en la base de datos.
/// Un job periódico de limpieza es una mejora razonable, no incluida en este primer alcance.
/// </summary>
public class EnvioResultadoToken
{
    public string Token { get; set; } = string.Empty;
    public int CodigoEvaluado { get; set; }
    public int IdPeriodo { get; set; }
    public byte[] ImagenPng { get; set; } = Array.Empty<byte>();
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
}
