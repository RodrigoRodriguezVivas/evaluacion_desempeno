namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface IWhatsAppService
{
    /// <summary>
    /// Envía una imagen (con una leyenda opcional) por WhatsApp al número indicado.
    /// </summary>
    /// <param name="numeroDestino">Número del colaborador, en cualquier formato razonable —
    /// se normaliza internamente (ver <c>WhatsAppNotificacionService.NormalizarNumero</c>).</param>
    /// <param name="urlImagenPublica">URL pública (accesible sin autenticación, de corta
    /// vigencia) desde la que el proveedor de WhatsApp descarga la imagen a enviar.</param>
    /// <param name="leyenda">Texto corto que acompaña la imagen en el mensaje de WhatsApp.</param>
    /// <returns>true si el mensaje se envió (o, en modo simulado, si se registró en el log)
    /// sin error; false si el proveedor devolvió un error.</returns>
    Task<bool> EnviarImagenAsync(string numeroDestino, string urlImagenPublica, string leyenda);
}
