namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

/// <summary>
/// Escala de calificación en porcentaje (0-100%) — desde el Entregable 12, esta es la escala
/// NATIVA de calificación en toda la aplicación (competencias e indicadores de gestión por
/// igual), a pedido explícito del usuario. Antes, las competencias se calificaban de 1 a 5 y el
/// porcentaje solo aparecía como una referencia visual informativa junto a esa escala; ahora es
/// al revés: el evaluador califica directamente en % y esta clase centraliza la clasificación
/// cualitativa (Deficiente/Aceptable/Bueno/Sobresaliente), reutilizada por
/// <see cref="Services.ResultadoService"/> (indirectamente, vía la vista), por
/// <see cref="Services.ResumenImagenService"/> (imagen-resumen de correo/WhatsApp) y por las
/// vistas de Diligenciar/Resultados/Reportes, para que las cuatro bandas sean idénticas en toda
/// la aplicación. Los puntos de corte son los del formato interno GHU-FOR-007 de Alianzagrafica.
/// </summary>
public static class EscalaCalificacion
{
    public static string Clasificar(decimal porcentaje) => porcentaje switch
    {
        < 60m => "Deficiente",
        < 80m => "Aceptable",
        <= 90m => "Bueno",
        _ => "Sobresaliente",
    };

    /// <summary>Clase de badge de Bootstrap para mostrar la banda con un color consistente
    /// (rojo=Deficiente, ámbar=Aceptable, celeste=Bueno, verde=Sobresaliente) en las vistas Razor.</summary>
    public static string ClaseBadge(decimal porcentaje) => Clasificar(porcentaje) switch
    {
        "Deficiente" => "bg-danger",
        "Aceptable" => "bg-warning text-dark",
        "Bueno" => "bg-info text-dark",
        _ => "bg-success",
    };
}
