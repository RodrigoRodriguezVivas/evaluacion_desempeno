using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Alianzagrafica.Evaluacion180.Web.Services;

/// <summary>
/// Genera la imagen-resumen (tarjeta PNG) de un resultado de evaluación consolidado, para
/// enviarla por correo (adjunta) y por WhatsApp (como imagen). Usa SixLabors.ImageSharp +
/// SixLabors.ImageSharp.Drawing: son librerías 100% administradas (no dependen de GDI+/libgdiplus
/// como System.Drawing.Common), así que el mismo código dibuja igual en el contenedor Docker
/// Linux de la demo y en el IIS de Alianzagrafica sobre Windows — a diferencia de la resolución
/// de la librería nativa de SQLite (sección 8.6 / 15.3 del documento de diseño), aquí no hay
/// ninguna dependencia nativa que fije por plataforma.
///
/// IMPORTANTE — no verificado con una compilación real: este entorno de desarrollo no tuvo
/// acceso a nuget.org (igual que el resto del proyecto — ver README y sección 15.5 del
/// documento de diseño), así que el código de esta clase se escribió con cuidado contra la API
/// pública documentada de ImageSharp/ImageSharp.Drawing, pero NO se pudo compilar ni ejecutar
/// contra los paquetes NuGet reales antes de entregarlo. Debe compilarse y probarse en un
/// entorno con acceso real a internet (ver .csproj) antes de darlo por cerrado.
/// </summary>
public class ResumenImagenService : IResumenImagenService
{
    private const int Ancho = 1000;
    private const int AltoMaximo = 1600;
    private static readonly Color ColorNavy = Color.ParseHex("1F3864");
    private static readonly Color ColorNavyClaro = Color.ParseHex("D9E2F3");
    private static readonly Color ColorGrisTexto = Color.ParseHex("404040");
    private static readonly Color ColorGrisClaro = Color.ParseHex("F2F2F2");
    private static readonly Color ColorBordeSuave = Color.ParseHex("DDE3EC");

    // Bandas de desempeño del formato interno GHU-FOR-007 (Entregable 12: el % es la escala
    // NATIVA de calificación desde este entregable, ya no una equivalencia informativa de la
    // escala 1-5 — ver Models/Entidades/EscalaCalificacion.cs, que centraliza los mismos puntos
    // de corte reutilizados aquí, en Diligenciar.cshtml y en Resultados/Reportes.cshtml).
    private static (string Nombre, Color Color) Banda(decimal porcentaje) => EscalaCalificacion.Clasificar(porcentaje) switch
    {
        "Deficiente" => ("Deficiente", Color.ParseHex("C0392B")),
        "Aceptable" => ("Aceptable", Color.ParseHex("BF8F00")),
        "Bueno" => ("Bueno", Color.ParseHex("2E7D32")),
        _ => ("Sobresaliente", Color.ParseHex("1B5E20")),
    };

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<ResumenImagenService> _logger;
    private readonly FontFamily _familiaRegular;
    private readonly FontFamily _familiaNegrita;

    public ResumenImagenService(AppDbContext db, IWebHostEnvironment entorno, ILogger<ResumenImagenService> logger)
    {
        _db = db;
        _entorno = entorno;
        _logger = logger;

        var carpetaFuentes = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        var coleccion = new FontCollection();
        _familiaRegular = coleccion.Add(Path.Combine(carpetaFuentes, "DejaVuSans.ttf"));
        _familiaNegrita = coleccion.Add(Path.Combine(carpetaFuentes, "DejaVuSans-Bold.ttf"));
    }

    public async Task<byte[]?> GenerarAsync(int codigoEvaluado, int idPeriodo)
    {
        var resultado = await _db.ResultadosConsolidados
            .Include(r => r.Evaluado)
            .Include(r => r.Periodo)
            .FirstOrDefaultAsync(r => r.CodigoEvaluado == codigoEvaluado && r.IdPeriodo == idPeriodo);

        if (resultado is null)
        {
            _logger.LogWarning("Se pidió generar la imagen-resumen para evaluado {Codigo}, periodo {Periodo}, pero no hay ResultadoConsolidado.", codigoEvaluado, idPeriodo);
            return null;
        }

        var competencias = await ObtenerPromediosPorCompetenciaAsync(codigoEvaluado, idPeriodo);

        var alto = Math.Min(AltoMaximo, 560 + competencias.Count * 56);

        using var imagen = new Image<Rgba32>(Ancho, alto);
        imagen.Mutate(ctx => ctx.Fill(Color.White));

        var fuenteTitulo = _familiaNegrita.CreateFont(30, FontStyle.Bold);
        var fuenteSubtitulo = _familiaRegular.CreateFont(18, FontStyle.Regular);
        var fuenteEtiqueta = _familiaRegular.CreateFont(15, FontStyle.Regular);
        var fuenteValorGrande = _familiaNegrita.CreateFont(52, FontStyle.Bold);
        var fuenteBanda = _familiaNegrita.CreateFont(20, FontStyle.Bold);
        var fuenteStat = _familiaNegrita.CreateFont(22, FontStyle.Bold);
        var fuenteStatEtiqueta = _familiaRegular.CreateFont(13, FontStyle.Regular);
        var fuenteCompetencia = _familiaRegular.CreateFont(15, FontStyle.Regular);
        var fuenteCompetenciaValor = _familiaNegrita.CreateFont(15, FontStyle.Bold);
        var fuentePie = _familiaRegular.CreateFont(12, FontStyle.Regular);

        // ---- Encabezado navy con el isotipo de Aligraf ----
        const int altoEncabezado = 130;
        imagen.Mutate(ctx => ctx.Fill(ColorNavy, new RectangleF(0, 0, Ancho, altoEncabezado)));

        var rutaLogo = Path.Combine(_entorno.WebRootPath, "img", "logo-icon.png");
        if (File.Exists(rutaLogo))
        {
            using var logo = Image.Load<Rgba32>(rutaLogo);
            logo.Mutate(x => x.Resize(0, 64)); // conserva proporción, alto fijo 64px
            imagen.Mutate(ctx => ctx.DrawImage(logo, new Point(40, 33), 1f));
        }

        imagen.Mutate(ctx => ctx.DrawText("Resultado de Evaluación de Desempeño 180°", fuenteSubtitulo, Color.White, new PointF(150, 40)));
        imagen.Mutate(ctx => ctx.DrawText("Alianzagrafica — Alianza Gráfica S.A.", fuenteEtiqueta, ColorNavyClaro, new PointF(150, 70)));

        var y = altoEncabezado + 30;

        // ---- Datos del colaborador ----
        imagen.Mutate(ctx => ctx.DrawText(resultado.Evaluado.Nombre, fuenteTitulo, ColorNavy, new PointF(40, y)));
        y += 40;
        imagen.Mutate(ctx => ctx.DrawText($"{resultado.Evaluado.Cargo} — {resultado.Evaluado.Area}", fuenteSubtitulo, ColorGrisTexto, new PointF(40, y)));
        y += 26;
        imagen.Mutate(ctx => ctx.DrawText(resultado.Periodo.Nombre, fuenteEtiqueta, ColorGrisTexto, new PointF(40, y)));
        y += 40;

        // ---- Promedio general + banda ----
        var promedioGeneral = resultado.PromedioGeneral ?? 0m;
        var (nombreBanda, colorBanda) = Banda(promedioGeneral);

        imagen.Mutate(ctx => ctx.Fill(ColorGrisClaro, new RectangleF(40, y, Ancho - 80, 150)));
        imagen.Mutate(ctx => ctx.DrawText("PROMEDIO GENERAL", fuenteEtiqueta, ColorGrisTexto, new PointF(64, y + 20)));
        imagen.Mutate(ctx => ctx.DrawText(promedioGeneral.ToString("0.0") + "%", fuenteValorGrande, ColorNavy, new PointF(64, y + 44)));

        var anchoEtiquetaBanda = TextMeasurer.MeasureSize(nombreBanda, new TextOptions(fuenteBanda)).Width + 28;
        imagen.Mutate(ctx => ctx.Fill(colorBanda, new RectangleF(Ancho - 64 - anchoEtiquetaBanda, y + 55, anchoEtiquetaBanda, 40)));
        imagen.Mutate(ctx => ctx.DrawText(nombreBanda, fuenteBanda, Color.White, new PointF(Ancho - 50 - anchoEtiquetaBanda, y + 65)));

        imagen.Mutate(ctx => ctx.DrawText("Escala 0-100% · equivalente GHU-FOR-007: 0-59% Deficiente · 60-79% Aceptable · 80-90% Bueno · 91-100% Sobresaliente",
            fuentePie, ColorGrisTexto, new PointF(64, y + 118)));

        y += 150 + 30;

        // ---- Sub-promedios por tipo de evaluación ----
        var subPromedios = new (string Etiqueta, decimal? Valor)[]
        {
            ("Autoevaluación", resultado.PromedioAutoevaluacion),
            ("Evaluación del jefe", resultado.PromedioJefe),
            ("Evaluación ascendente", resultado.PromedioAscendente),
        };
        var anchoColumna = (Ancho - 80) / subPromedios.Length;
        for (var i = 0; i < subPromedios.Length; i++)
        {
            var x = 40 + i * anchoColumna;
            var (etiqueta, valor) = subPromedios[i];
            var texto = valor is decimal v ? v.ToString("0.0") + "%" : "—";
            imagen.Mutate(ctx => ctx.DrawText(texto, fuenteStat, ColorNavy, new PointF(x, y)));
            imagen.Mutate(ctx => ctx.DrawText(etiqueta, fuenteStatEtiqueta, ColorGrisTexto, new PointF(x, y + 30)));
        }
        y += 70;

        imagen.Mutate(ctx => ctx.Fill(ColorBordeSuave, new RectangleF(40, y, Ancho - 80, 1)));
        y += 24;

        // ---- Detalle por competencia (barras horizontales) ----
        if (competencias.Count > 0)
        {
            imagen.Mutate(ctx => ctx.DrawText("Detalle por competencia", fuenteBanda, ColorNavy, new PointF(40, y)));
            y += 36;

            const int anchoBarraMax = 560;
            foreach (var (nombre, promedio) in competencias)
            {
                var (_, colorBarra) = Banda(promedio);
                imagen.Mutate(ctx => ctx.DrawText(Truncar(nombre, 48), fuenteCompetencia, ColorGrisTexto, new PointF(40, y)));

                var anchoFondo = anchoBarraMax;
                var anchoValor = (float)(anchoBarraMax * Math.Min(1m, promedio / 100m));
                imagen.Mutate(ctx => ctx.Fill(ColorGrisClaro, new RectangleF(340, y + 2, anchoFondo, 18)));
                imagen.Mutate(ctx => ctx.Fill(colorBarra, new RectangleF(340, y + 2, anchoValor, 18)));
                imagen.Mutate(ctx => ctx.DrawText(promedio.ToString("0.0") + "%", fuenteCompetenciaValor, ColorGrisTexto, new PointF(340 + anchoBarraMax + 12, y)));

                y += 40;
            }
            y += 16;
        }

        // ---- Pie de página ----
        imagen.Mutate(ctx => ctx.Fill(ColorBordeSuave, new RectangleF(40, y, Ancho - 80, 1)));
        y += 20;
        imagen.Mutate(ctx => ctx.DrawText(
            "Información confidencial de uso exclusivo del colaborador. Generado automáticamente por el " +
            "Sistema de Evaluación de Desempeño 180° de Alianzagrafica — no responder a este mensaje.",
            fuentePie, ColorGrisTexto, new PointF(40, y)));

        using var salida = new MemoryStream();
        await imagen.SaveAsPngAsync(salida);
        return salida.ToArray();
    }

    /// <summary>Promedio de calificación por competencia, a partir de todas las respuestas
    /// ENVIADAS (Constantes.RespuestaEnviada) de las asignaciones del evaluado en el periodo —
    /// mismo criterio de "completo" que usa ResultadoService al consolidar.</summary>
    private async Task<List<(string Nombre, decimal Promedio)>> ObtenerPromediosPorCompetenciaAsync(int codigoEvaluado, int idPeriodo)
    {
        var idsAsignacion = await _db.AsignacionesEvaluacion
            .Where(a => a.CodigoEvaluado == codigoEvaluado && a.IdPeriodo == idPeriodo)
            .Select(a => a.IdAsignacion)
            .ToListAsync();

        if (idsAsignacion.Count == 0) return new List<(string, decimal)>();

        var detalles = await _db.RespuestaDetalles
            .Include(d => d.Respuesta)
            .Include(d => d.Competencia)
            .Where(d => idsAsignacion.Contains(d.Respuesta.IdAsignacion) && d.Respuesta.Estado == Constantes.RespuestaEnviada)
            .ToListAsync();

        return detalles
            .GroupBy(d => d.Competencia.Nombre)
            .Select(g => (Nombre: g.Key, Promedio: Math.Round(g.Average(d => d.Calificacion), 2)))
            .OrderByDescending(c => c.Promedio)
            .ToList();
    }

    private static string Truncar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..(maximo - 1)] + "…";
}
