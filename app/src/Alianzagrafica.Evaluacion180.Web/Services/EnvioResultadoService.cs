using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alianzagrafica.Evaluacion180.Web.Services;

/// <summary>
/// Orquesta el nuevo módulo de envío de resultados (RF-23): genera la imagen-resumen una sola
/// vez y la manda por correo (adjunta) y por WhatsApp (como imagen, vía una URL temporal), de
/// forma independiente por canal.
/// </summary>
public class EnvioResultadoService : IEnvioResultadoService
{
    private readonly AppDbContext _db;
    private readonly IResumenImagenService _imagenes;
    private readonly INotificacionService _notificaciones;
    private readonly IWhatsAppService _whatsApp;
    private readonly IAuditoriaService _auditoria;
    private readonly SmtpOptions _smtpOpciones;

    /// <summary>Vigencia del enlace público temporal de la imagen — solo necesita durar lo que
    /// tarde el proveedor de WhatsApp en descargarla tras recibir la solicitud de envío
    /// (segundos, en la práctica). 30 minutos deja margen de sobra sin dejar el enlace
    /// expuesto por más tiempo del necesario.</summary>
    private static readonly TimeSpan VigenciaToken = TimeSpan.FromMinutes(30);

    public EnvioResultadoService(
        AppDbContext db,
        IResumenImagenService imagenes,
        INotificacionService notificaciones,
        IWhatsAppService whatsApp,
        IAuditoriaService auditoria,
        IOptions<SmtpOptions> smtpOpciones)
    {
        _db = db;
        _imagenes = imagenes;
        _notificaciones = notificaciones;
        _whatsApp = whatsApp;
        _auditoria = auditoria;
        _smtpOpciones = smtpOpciones.Value;
    }

    public async Task<EnvioResultadoResumen> EnviarAsync(int codigoEvaluado, int idPeriodo, int? idUsuarioQueEnvia, string? direccionIp)
    {
        var salida = new EnvioResultadoResumen();

        var resultado = await _db.ResultadosConsolidados
            .Include(r => r.Evaluado)
            .Include(r => r.Periodo)
            .FirstOrDefaultAsync(r => r.CodigoEvaluado == codigoEvaluado && r.IdPeriodo == idPeriodo);

        if (resultado is null)
        {
            salida.Advertencias.Add("Este colaborador todavía no tiene un resultado consolidado en este periodo.");
            return salida;
        }
        salida.TieneResultado = true;

        var imagenPng = await _imagenes.GenerarAsync(codigoEvaluado, idPeriodo);
        if (imagenPng is null)
        {
            // No debería pasar (ya confirmamos que existe el resultado), pero por si acaso.
            salida.Advertencias.Add("No se pudo generar la imagen-resumen.");
            return salida;
        }

        // ---- Correo ----
        var correoOk = await _notificaciones.EnviarResultadoEvaluacionAsync(resultado.Evaluado, resultado, imagenPng);
        salida.CorreoEnviado = correoOk;
        if (!correoOk) salida.Advertencias.Add("No se pudo enviar el correo (revisa que el empleado tenga correo registrado, o el log para más detalle).");

        await _auditoria.RegistrarAsync(idUsuarioQueEnvia, "EnvioResultadoCorreo",
            $"Evaluado {codigoEvaluado}, periodo {idPeriodo} — {(correoOk ? "enviado" : "falló")}", direccionIp);

        // ---- WhatsApp ----
        var contacto = await _db.ContactosNotificacion.FirstOrDefaultAsync(c => c.CodigoEmpleado == codigoEvaluado);
        if (contacto?.TelefonoWhatsApp is { Length: > 0 } telefono)
        {
            var token = Guid.NewGuid().ToString("N");
            _db.EnviosResultadoToken.Add(new EnvioResultadoToken
            {
                Token = token,
                CodigoEvaluado = codigoEvaluado,
                IdPeriodo = idPeriodo,
                ImagenPng = imagenPng,
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.Add(VigenciaToken),
            });
            await _db.SaveChangesAsync();

            var urlImagen = $"{_smtpOpciones.UrlBase.TrimEnd('/')}/Resultados/ImagenResumen/{token}";
            var leyenda = $"Hola {resultado.Evaluado.Nombre}, este es el resumen de tu Evaluación de Desempeño 180° " +
                          $"({resultado.Periodo.Nombre}). Promedio general: {resultado.PromedioGeneral?.ToString("0.00") ?? "—"}. " +
                          "Consulta el detalle completo en el sistema — Alianzagrafica.";

            var whatsAppOk = await _whatsApp.EnviarImagenAsync(telefono, urlImagen, leyenda);
            salida.WhatsAppEnviado = whatsAppOk;
            if (!whatsAppOk) salida.Advertencias.Add("No se pudo enviar el WhatsApp (revisa el log para más detalle).");

            await _auditoria.RegistrarAsync(idUsuarioQueEnvia, "EnvioResultadoWhatsApp",
                $"Evaluado {codigoEvaluado}, periodo {idPeriodo} — {(whatsAppOk ? "enviado" : "falló")}", direccionIp);
        }
        else
        {
            salida.Advertencias.Add("Este colaborador no tiene número de WhatsApp registrado (Empleados → editar contacto), así que no se envió por ese canal.");
        }

        return salida;
    }
}
