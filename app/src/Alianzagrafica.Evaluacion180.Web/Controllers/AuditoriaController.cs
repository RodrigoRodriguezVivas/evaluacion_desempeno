using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize(Roles = Constantes.RolAdministradorSistema)]
public class AuditoriaController : Controller
{
    private readonly AppDbContext _db;

    public AuditoriaController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var eventos = await _db.Auditorias
            .Include(a => a.Usuario).ThenInclude(u => u!.Empleado)
            .OrderByDescending(a => a.FechaHora)
            .Take(200)
            .ToListAsync();

        var modelo = eventos.Select(a => new AuditoriaItemViewModel
        {
            IdEvento = a.IdEvento,
            Usuario = a.Usuario?.Empleado.Nombre,
            TipoEvento = a.TipoEvento,
            Detalle = a.Detalle,
            FechaHora = a.FechaHora,
            DireccionIP = a.DireccionIP,
        }).ToList();

        return View(modelo);
    }
}
