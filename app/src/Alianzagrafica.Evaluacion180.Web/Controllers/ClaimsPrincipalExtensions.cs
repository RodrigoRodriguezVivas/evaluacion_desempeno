using System.Security.Claims;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static int CodigoEmpleado(this ClaimsPrincipal principal)
    {
        var valor = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, out var codigo) ? codigo : 0;
    }
}
