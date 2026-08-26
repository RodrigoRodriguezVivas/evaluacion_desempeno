namespace Alianzagrafica.Evaluacion180.Web.Services;

public interface IAuditoriaService
{
    Task RegistrarAsync(int? idUsuario, string tipoEvento, string? detalle, string? direccionIp);
}
