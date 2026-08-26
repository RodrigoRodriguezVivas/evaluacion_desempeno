using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Models.ViewModels;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Controllers;

[Authorize]
public class ResultadosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEnvioResultadoService _envioResultado;

    public ResultadosController(AppDbContext db, IEnvioResultadoService envioResultado)
    {
        _db = db;
        _envioResultado = envioResultado;
    }

    // GET /Resultados/Mios — resultado consolidado propio (RF-16)
    public async Task<IActionResult> Mios()
    {
        var codigoEmpleado = User.CodigoEmpleado();
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.CodigoEmpleado == codigoEmpleado);
        if (empleado is null) return NotFound();

        var resultados = await _db.ResultadosConsolidados
            .Include(r => r.Periodo)
            .Where(r => r.CodigoEvaluado == codigoEmpleado)
            .OrderByDescending(r => r.Periodo.FechaApertura)
            .ToListAsync();

        var modelo = new MisResultadosViewModel
        {
            NombreEmpleado = empleado.Nombre,
            Historico = resultados.Select(r => new ResultadoPeriodoViewModel
            {
                PeriodoNombre = r.Periodo.Nombre,
                PromedioAutoevaluacion = r.PromedioAutoevaluacion,
                PromedioJefe = r.PromedioJefe,
                PromedioAscendente = r.PromedioAscendente,
                PromedioGeneral = r.PromedioGeneral,
                FechaConsolidacion = r.FechaConsolidacion,
            }).ToList(),
        };

        return View(modelo);
    }

    // GET /Resultados/Reportes — vista agregada por área (RF-17, RF-18, RF-19)
    [Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema + "," + Constantes.RolConsultaDirectiva)]
    public async Task<IActionResult> Reportes(int? idPeriodo)
    {
        var periodo = idPeriodo is int id
            ? await _db.PeriodosEvaluacion.FirstOrDefaultAsync(p => p.IdPeriodo == id)
            : await _db.PeriodosEvaluacion.OrderByDescending(p => p.FechaApertura).FirstOrDefaultAsync();

        var modelo = new ReportesViewModel { IdPeriodo = periodo?.IdPeriodo, PeriodoNombre = periodo?.Nombre };
        if (periodo is null) return View(modelo);

        var asignaciones = await _db.AsignacionesEvaluacion
            .Include(a => a.Evaluado)
            .Where(a => a.IdPeriodo == periodo.IdPeriodo)
            .ToListAsync();

        modelo.AvancePorArea = asignaciones
            .GroupBy(a => a.Evaluado.Area)
            .Select(g => new ReporteAreaViewModel
            {
                Area = g.Key,
                TotalAsignadas = g.Count(),
                Completadas = g.Count(a => a.EstaCompletada),
                Pendientes = g.Count(a => !a.EstaCompletada),
            })
            .OrderBy(a => a.Area)
            .ToList();

        var resultados = await _db.ResultadosConsolidados
            .Include(r => r.Evaluado).ThenInclude(e => e.TipoPersonal)
            .Where(r => r.IdPeriodo == periodo.IdPeriodo)
            .ToListAsync();

        modelo.Resultados = resultados
            .OrderBy(r => r.PromedioGeneral)
            .Select(r => new ResultadoEmpleadoViewModel
            {
                CodigoEmpleado = r.Evaluado.CodigoEmpleado,
                NombreEmpleado = r.Evaluado.Nombre,
                Cargo = r.Evaluado.Cargo,
                TipoPersonal = r.Evaluado.TipoPersonal.Nombre,
                Area = r.Evaluado.Area,
                PromedioGeneral = r.PromedioGeneral,
            })
            .ToList();

        return View(modelo);
    }

    // POST /Resultados/EnviarResultado — dispara el envío del resultado consolidado al
    // empleado evaluado por correo electrónico y, si tiene un número registrado, un resumen
    // por WhatsApp (RF-23, ver Services/EnvioResultadoService.cs). No se resuelve el
    // IdUsuario del administrador que dispara el envío (se registra null en la auditoría),
    // siguiendo la misma convención usada en PeriodosController para acciones administrativas
    // masivas.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constantes.RolAdministradorGestionHumana + "," + Constantes.RolAdministradorSistema)]
    public async Task<IActionResult> EnviarResultado(int codigoEvaluado, int idPeriodo)
    {
        var resumen = await _envioResultado.EnviarAsync(codigoEvaluado, idPeriodo, null,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!resumen.TieneResultado)
        {
            TempData["Mensaje"] = "No se encontró un resultado consolidado para este empleado en el período seleccionado.";
        }
        else
        {
            var partes = new List<string>
            {
                resumen.CorreoEnviado ? "correo enviado" : "el correo no se pudo enviar",
            };
            if (resumen.WhatsAppEnviado)
            {
                partes.Add("WhatsApp enviado");
            }

            var mensaje = "Resultado procesado: " + string.Join(", ", partes) + ".";
            if (resumen.Advertencias.Count > 0)
            {
                mensaje += " " + string.Join(" ", resumen.Advertencias);
            }

            TempData["Mensaje"] = mensaje;
        }

        return RedirectToAction(nameof(Reportes), new { idPeriodo });
    }

    // GET /Resultados/ImagenResumen/{token} — sirve la imagen-resumen generada para un envío
    // puntual de WhatsApp (RF-23). Endpoint anónimo por necesidad: el proveedor de WhatsApp
    // (Twilio) descarga la imagen desde esta URL pública, no acepta un adjunto binario directo
    // en este flujo simple. La superficie de exposición se acota con un token aleatorio de un
    // solo uso conceptual y una vigencia corta (ver EnvioResultadoService.VigenciaToken);
    // pendiente de mejora documentado: no hay una tarea que purgue filas ya expiradas.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ImagenResumen(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return NotFound();

        var registro = await _db.EnviosResultadoToken.FirstOrDefaultAsync(t => t.Token == token);
        if (registro is null) return NotFound();
        if (registro.FechaExpiracion < DateTime.UtcNow) return NotFound();

        return File(registro.ImagenPng, "image/png");
    }
}
