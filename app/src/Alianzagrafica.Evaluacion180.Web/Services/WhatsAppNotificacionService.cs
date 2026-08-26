using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace Alianzagrafica.Evaluacion180.Web.Services;

public class WhatsAppOptions
{
    /// <summary>Si es true (valor por defecto), los mensajes de WhatsApp no se envían de
    /// verdad: solo quedan registrados en el log de la aplicación — igual que
    /// <see cref="SmtpOptions.ModoSimulado"/>. Actívalo en false solo cuando Alianzagrafica
    /// tenga una cuenta de WhatsApp Business API aprobada y configurada abajo.</summary>
    public bool ModoSimulado { get; set; } = true;

    /// <summary>Único proveedor implementado en este primer alcance. La interfaz
    /// <see cref="IWhatsAppService"/> permite agregar otro (p. ej. Meta Cloud API directo) sin
    /// tocar el resto del sistema — ver nota en la clase de implementación.</summary>
    public string Proveedor { get; set; } = "Twilio";

    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Número remitente de WhatsApp aprobado en Twilio, en formato
    /// "whatsapp:+14155238886" (incluye el prefijo "whatsapp:").</summary>
    public string NumeroRemitente { get; set; } = string.Empty;

    /// <summary>Indicativo de país que se antepone cuando un número se guarda sin "+" (ver
    /// <see cref="WhatsAppNotificacionService.NormalizarNumero"/>). Colombia = "57".</summary>
    public string IndicativoPaisPorDefecto { get; set; } = "57";
}

// Nota: la URL base pública de la aplicación (para construir el enlace temporal de la imagen
// que Twilio descarga) se toma de Smtp:UrlBase — ya existe con ese propósito general (ver
// SmtpOptions.UrlBase) y no tenía sentido duplicarla en una segunda sección de configuración.

/// <summary>
/// Envío de la imagen-resumen del resultado de evaluación por WhatsApp (nuevo módulo pedido
/// por el usuario: "adicionar un módulo para... enviar... una imagen con el resumen... al
/// WhatsApp [del empleado]").
///
/// Implementado contra la API REST de Twilio para WhatsApp (twilio.com/whatsapp), llamada
/// directamente por HTTP — sin el SDK NuGet de Twilio — porque la operación que se necesita
/// (enviar un mensaje con una imagen) es una sola llamada POST simple, y evitar el SDK reduce
/// el número de dependencias nuevas del proyecto a solo SixLabors.ImageSharp (ver
/// ResumenImagenService.cs y el .csproj). Si Alianzagrafica prefiere la Meta Cloud API oficial
/// de WhatsApp Business en vez de Twilio, se puede escribir OTRA implementación de
/// <see cref="IWhatsAppService"/> y cambiar un único registro de DI en Program.cs — el resto
/// del sistema (EnvioResultadoService, ResultadosController) no cambia.
///
/// IMPORTANTE — supuesto pendiente de validar con Alianzagrafica: usar WhatsApp Business API en
/// producción requiere una cuenta de WhatsApp Business API real (Twilio o Meta), un número
/// remitente aprobado, y — fuera del sandbox de pruebas — que cada colaborador le haya escrito
/// primero al número remitente o haya aceptado una plantilla de mensaje ("template") aprobada
/// por Meta, según las políticas de WhatsApp Business (esto no es una limitación de este
/// código, es una regla del propio WhatsApp). Mientras esa cuenta no exista, <c>ModoSimulado</c>
/// debe quedar en true (valor por defecto) — igual que se hizo con Smtp mientras no había
/// servidor SMTP real disponible en este entorno.
/// </summary>
public class WhatsAppNotificacionService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _opciones;
    private readonly ILogger<WhatsAppNotificacionService> _logger;

    public WhatsAppNotificacionService(HttpClient http, IOptions<WhatsAppOptions> opciones, ILogger<WhatsAppNotificacionService> logger)
    {
        _http = http;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<bool> EnviarImagenAsync(string numeroDestino, string urlImagenPublica, string leyenda)
    {
        var numero = NormalizarNumero(numeroDestino, _opciones.IndicativoPaisPorDefecto);

        if (_opciones.ModoSimulado || string.IsNullOrWhiteSpace(_opciones.AccountSid) || string.IsNullOrWhiteSpace(_opciones.AuthToken))
        {
            _logger.LogInformation(
                "[WhatsApp simulado] Para: {Numero} — Imagen: {Url} — Leyenda: {Leyenda}",
                numero, urlImagenPublica, leyenda);
            return true;
        }

        if (!string.Equals(_opciones.Proveedor, "Twilio", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("WhatsApp:Proveedor = '{Proveedor}' no está implementado (solo 'Twilio' en este alcance).", _opciones.Proveedor);
            return false;
        }

        try
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_opciones.AccountSid}/Messages.json";
            var credenciales = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opciones.AccountSid}:{_opciones.AuthToken}"));

            using var solicitud = new HttpRequestMessage(HttpMethod.Post, url);
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciales);
            solicitud.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = _opciones.NumeroRemitente,
                ["To"] = $"whatsapp:{numero}",
                ["Body"] = leyenda,
                ["MediaUrl"] = urlImagenPublica,
            });

            using var respuesta = await _http.SendAsync(solicitud);
            if (respuesta.IsSuccessStatusCode) return true;

            var cuerpo = await respuesta.Content.ReadAsStringAsync();
            _logger.LogError("Twilio devolvió {Codigo} al enviar WhatsApp a {Numero}: {Cuerpo}", (int)respuesta.StatusCode, numero, cuerpo);
            return false;
        }
        catch (Exception ex)
        {
            // Igual que en SmtpNotificacionService: un fallo de WhatsApp nunca debe tumbar
            // el flujo de negocio (el envío del correo con el resultado, por ejemplo, debe
            // seguir su curso aunque este canal falle).
            _logger.LogError(ex, "No se pudo enviar el WhatsApp a {Numero}", numero);
            return false;
        }
    }

    /// <summary>Normaliza a formato internacional E.164 (+&lt;indicativo&gt;&lt;número&gt;):
    /// quita espacios/guiones/paréntesis, y si no empieza por "+" asume el indicativo de país
    /// por defecto (Colombia, "57"). No valida longitud ni que sea un celular real — esa
    /// responsabilidad queda en quien registra el número (ver Empleados/Index.cshtml).</summary>
    internal static string NormalizarNumero(string numero, string indicativoPorDefecto)
    {
        var limpio = new string(numero.Where(char.IsDigit).ToArray());
        if (numero.TrimStart().StartsWith("+")) return "+" + limpio;
        return limpio.StartsWith(indicativoPorDefecto) ? "+" + limpio : "+" + indicativoPorDefecto + limpio;
    }
}
