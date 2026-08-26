using Alianzagrafica.Evaluacion180.Web.Data;
using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Services;

/// <summary>
/// Implementa en C# la misma lógica que las secciones 6 y 7 de
/// sql/01_esquema_y_datos_ficticios.sql, para que la aplicación pueda
/// generar formularios y asignaciones de un periodo sin depender de
/// ejecutar el script manualmente cada vez (RF-05, RF-08, RF-09).
/// </summary>
public class AsignacionService : IAsignacionService
{
    private readonly AppDbContext _db;

    public AsignacionService(AppDbContext db) => _db = db;

    public async Task<int> GenerarFormulariosAsync(int idPeriodo)
    {
        var periodo = await _db.PeriodosEvaluacion.FirstOrDefaultAsync(p => p.IdPeriodo == idPeriodo)
            ?? throw new InvalidOperationException("El periodo indicado no existe.");

        var tipos = await _db.TiposPersonal.ToListAsync();
        var competencias = await _db.Competencias.Where(c => c.Activa).ToListAsync();
        var existentes = await _db.FormulariosEvaluacion
            .Where(f => f.IdPeriodo == idPeriodo)
            .Select(f => new { f.TipoRelacion, f.IdTipoPersonal })
            .ToListAsync();

        var creados = 0;

        foreach (var tipo in tipos)
        {
            var relaciones = new List<string> { Constantes.RelacionAutoevaluacion, Constantes.RelacionJefe };
            if (tipo.PermiteEvaluacionAscendente) relaciones.Add(Constantes.RelacionAscendente);

            foreach (var relacion in relaciones)
            {
                if (existentes.Any(e => e.TipoRelacion == relacion && e.IdTipoPersonal == tipo.IdTipoPersonal))
                    continue; // ya existe: la generación es idempotente

                var formulario = new FormularioEvaluacion
                {
                    IdPeriodo = idPeriodo,
                    TipoRelacion = relacion,
                    IdTipoPersonal = tipo.IdTipoPersonal,
                    Nombre = $"Formulario {relacion} — {tipo.Nombre} — {periodo.Nombre}",
                };
                _db.FormulariosEvaluacion.Add(formulario);
                await _db.SaveChangesAsync(); // asigna IdFormulario para poder enlazar competencias

                var competenciasFormulario = competencias
                    .Where(c => c.IdTipoPersonal == tipo.IdTipoPersonal || c.IdTipoPersonal == null)
                    .ToList();

                if (competenciasFormulario.Count > 0)
                {
                    var ponderacion = Math.Round(100m / competenciasFormulario.Count, 2);
                    foreach (var comp in competenciasFormulario)
                    {
                        _db.FormularioCompetencias.Add(new FormularioCompetencia
                        {
                            IdFormulario = formulario.IdFormulario,
                            IdCompetencia = comp.IdCompetencia,
                            Ponderacion = ponderacion,
                        });
                    }
                }

                creados++;
            }
        }

        await _db.SaveChangesAsync();
        return creados;
    }

    public async Task<int> GenerarAsignacionesAsync(int idPeriodo)
    {
        var empleados = await _db.Empleados.Where(e => e.Estado == Constantes.EstadoEmpleadoActivo).ToListAsync();
        var tipos = await _db.TiposPersonal.ToListAsync();
        var formularios = await _db.FormulariosEvaluacion.Where(f => f.IdPeriodo == idPeriodo).ToListAsync();
        var existentes = (await _db.AsignacionesEvaluacion
            .Where(a => a.IdPeriodo == idPeriodo)
            .Select(a => new { a.CodigoEvaluador, a.CodigoEvaluado, a.TipoRelacion })
            .ToListAsync())
            .ToHashSet();

        var empleadosPorCodigo = empleados.ToDictionary(e => e.CodigoEmpleado);
        var tipoPorId = tipos.ToDictionary(t => t.IdTipoPersonal);

        FormularioEvaluacion? BuscarFormulario(string relacion, int idTipoPersonal) =>
            formularios.FirstOrDefault(f => f.TipoRelacion == relacion && f.IdTipoPersonal == idTipoPersonal);

        var nuevas = new List<AsignacionEvaluacion>();

        void AgregarSiNoExiste(int evaluador, int evaluado, string relacion, int? idFormulario)
        {
            if (existentes.Contains(new { CodigoEvaluador = evaluador, CodigoEvaluado = evaluado, TipoRelacion = relacion }))
                return;
            nuevas.Add(new AsignacionEvaluacion
            {
                IdPeriodo = idPeriodo,
                CodigoEvaluador = evaluador,
                CodigoEvaluado = evaluado,
                TipoRelacion = relacion,
                IdFormulario = idFormulario,
                Estado = Constantes.AsignacionProgramada,
            });
        }

        foreach (var empleado in empleados)
        {
            // 1) Autoevaluación
            var formAuto = BuscarFormulario(Constantes.RelacionAutoevaluacion, empleado.IdTipoPersonal);
            AgregarSiNoExiste(empleado.CodigoEmpleado, empleado.CodigoEmpleado, Constantes.RelacionAutoevaluacion, formAuto?.IdFormulario);

            // 2) Jefe -> Colaborador
            if (empleado.CodigoJefeDirecto is int codigoJefe && empleadosPorCodigo.ContainsKey(codigoJefe))
            {
                var formJefe = BuscarFormulario(Constantes.RelacionJefe, empleado.IdTipoPersonal);
                AgregarSiNoExiste(codigoJefe, empleado.CodigoEmpleado, Constantes.RelacionJefe, formJefe?.IdFormulario);

                // 3) Ascendente: el colaborador evalúa a su jefe si el tipo de personal del jefe lo permite
                var jefe = empleadosPorCodigo[codigoJefe];
                var tipoJefe = tipoPorId[jefe.IdTipoPersonal];
                if (tipoJefe.PermiteEvaluacionAscendente)
                {
                    var formAscendente = BuscarFormulario(Constantes.RelacionAscendente, jefe.IdTipoPersonal);
                    AgregarSiNoExiste(empleado.CodigoEmpleado, codigoJefe, Constantes.RelacionAscendente, formAscendente?.IdFormulario);
                }
            }
        }

        _db.AsignacionesEvaluacion.AddRange(nuevas);
        await _db.SaveChangesAsync();
        return nuevas.Count;
    }
}
