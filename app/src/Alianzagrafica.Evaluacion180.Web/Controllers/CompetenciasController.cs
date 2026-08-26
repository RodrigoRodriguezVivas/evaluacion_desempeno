using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema)]
public class CompetenciasController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAuditoriaService _auditoria;

    public CompetenciasController(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _auditoria = auditoria;
    }

    public async Task<IActionResult> Index()
    {
        var competencias = await _db.Competencias.Include(c => c.TipoPersonal).OrderBy(c => c.IdTipoPersonal == null ? 0 : 1).ThenBy(c => c.Nombre).ToListAsync();

        var modelo = competencias.Select(c => new CompetenciaListaItemViewModel
        {
            IdCompetencia = c.IdCompetencia,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            Grupo = c.GrupoDescripcion,
            Activa = c.Activa,
        }).ToList();

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var tipos = await _db.TiposPersonal.OrderBy(t => t.Nombre).ToListAsync();
        return View(new CrearCompetenciaViewModel
        {
            TiposDisponibles = tipos.Select(t => new TipoPersonalOpcionViewModel { IdTipoPersonal = t.IdTipoPersonal, Nombre = t.Nombre }).ToList(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearCompetenciaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            modelo.TiposDisponibles = (await _db.TiposPersonal.OrderBy(t => t.Nombre).ToListAsync())
                .Select(t => new TipoPersonalOpcionViewModel { IdTipoPersonal = t.IdTipoPersonal, Nombre = t.Nombre }).ToList();
            return View(modelo);
        }

        _db.Competencias.Add(new Competencia
        {
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            IdTipoPersonal = modelo.IdTipoPersonal,
            Activa = true,
        });
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(null, "CreacionCompetencia", modelo.Nombre, HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["Mensaje"] = "Competencia creada. Recuerda que solo se incluirá en los formularios de los periodos que se creen a partir de ahora.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarActiva(int id)
    {
        var competencia = await _db.Competencias.FirstOrDefaultAsync(c => c.IdCompetencia == id);
        if (competencia is null) return NotFound();

        competencia.Activa = !competencia.Activa;
        await _db.SaveChangesAsync();

        await _auditoria.RegistrarAsync(null, "CambioEstadoCompetencia", $"{competencia.Nombre}: Activa={competencia.Activa}", HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToAction(nameof(Index));
    }
}
