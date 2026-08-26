namespace Alianzagrafica.Evaluacion180.Web.Services;

public class EnvioResultadoResumen
{
    public bool TieneResultado { get; set; }
    public bool CorreoEnviado { get; set; }
    public bool WhatsAppEnviado { get; set; }
    public List<string> Advertencias { get; set; } = new();
}

public interface IEnvioResultadoService
{
    /// <summary>
    /// Orquesta el envío del resultado de evaluación de un colaborador por los dos canales
    /// pedidos: correo (con la imagen-resumen adjunta) y WhatsApp (la misma imagen). Genera la
    /// imagen una sola vez y la reutiliza en ambos canales. Cada canal se intenta de forma
    /// independiente — que uno falle (o que el colaborador no tenga ese dato de contacto) no
    /// impide que el otro se intente.
    /// </summary>
    Task<EnvioResultadoResumen> EnviarAsync(int codigoEvaluado, int idPeriodo, int? idUsuarioQueEnvia, string? direccionIp);
}
