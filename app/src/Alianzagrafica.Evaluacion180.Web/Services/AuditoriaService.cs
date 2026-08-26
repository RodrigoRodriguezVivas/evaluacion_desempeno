using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;

namespace Alianzagrafica.Evaluacion180.Web.Services;

/// <summary>Registra eventos sensibles del sistema (RF-22): login, envío de evaluación,
/// apertura/cierre de periodo, cambios de configuración, exportación de reportes.</summary>
public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _db;

    public AuditoriaService(AppDbContext db) => _db = db;

    public async Task RegistrarAsync(int? idUsuario, string tipoEvento, string? detalle, string? direccionIp)
    {
        _db.Auditorias.Add(new Auditoria
        {
            IdUsuario = idUsuario,
            TipoEvento = tipoEvento,
            Detalle = detalle,
            DireccionIP = direccionIp,
            FechaHora = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }
}
