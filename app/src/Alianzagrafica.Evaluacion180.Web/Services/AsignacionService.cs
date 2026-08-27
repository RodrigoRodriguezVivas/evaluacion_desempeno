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
        var indicadores = await _db.IndicadoresGestion.Where(i => i.Activa).ToListAsync();
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
                    // Una competencia propia del tipo de personal reemplaza a la genérica del
                    // mismo nombre (ej. "Trabajo en equipo" tiene una versión propia para
                    // Conductor, con comportamientos de rol distintos — GHU-FOR-007, sección
                    // "COMPETENCIAS DE ROL"), para que no se evalúe dos veces la misma competencia.
                    .GroupBy(c => c.Nombre, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(c => c.IdTipoPersonal.HasValue).First())
                    .ToList();

                // Mismo criterio de deduplicación que las competencias (Entregable 11): un
                // indicador propio del tipo de personal reemplazaría a uno genérico del mismo
                // nombre. Hoy los 4 indicadores del formato "EVALUACION DESEMPEÑO Indicadores"
                // son genéricos (aplican a los seis tipos de personal), pero el mecanismo queda
                // listo para indicadores específicos por tipo de personal si se agregan después.
                var indicadoresFormulario = indicadores
                    .Where(i => i.IdTipoPersonal == tipo.IdTipoPersonal || i.IdTipoPersonal == null)
                    .GroupBy(i => i.Nombre, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(i => i.IdTipoPersonal.HasValue).First())
                    .ToList();

                if (competenciasFormulario.Count > 0 || indicadoresFormulario.Count > 0)
                {
                    // Ponderación por macro-grupo (RF-07). Hasta el Entregable 10, el 100% se
                    // repartía en partes iguales entre las categorías de competencias presentes
                    // (ej. "Organizacional" y "DeRol" → 50%/50%). Desde el Entregable 11, con el
                    // grupo "Indicadores de Gestión" incorporado (formato real "EVALUACION
                    // DESEMPEÑO Indicadores"), se usan pesos FIJOS — Indicadores de Gestión 50%,
                    // Organizacional 20%, De Rol 30% — cuando los tres grupos están presentes en
                    // el formulario. Si falta alguno (ej. un tipo de personal sin indicadores
                    // configurados todavía, o una competencia sin categoría asignada), se usa
                    // como respaldo el reparto parejo histórico entre los grupos presentes, para
                    // que el formulario siempre llegue a sumar 100%.
                    var gruposCompetencias = competenciasFormulario
                        .GroupBy(c => c.Categoria ?? $"__sin_categoria_{c.IdCompetencia}")
                        .ToList();

                    var clavesPresentes = gruposCompetencias.Select(g => g.Key).ToList();
                    if (indicadoresFormulario.Count > 0) clavesPresentes.Add(Constantes.CategoriaIndicadoresGestion);

                    var pesosFijos = new Dictionary<string, decimal>
                    {
                        [Constantes.CategoriaOrganizacional] = Constantes.PesoOrganizacional,
                        [Constantes.CategoriaDeRol] = Constantes.PesoDeRol,
                        [Constantes.CategoriaIndicadoresGestion] = Constantes.PesoIndicadoresGestion,
                    };
                    var usaPesosFijos = clavesPresentes.Count == 3 && clavesPresentes.All(pesosFijos.ContainsKey);
                    var pesoParejo = clavesPresentes.Count > 0 ? Math.Round(100m / clavesPresentes.Count, 4) : 0m;

                    decimal PesoDelGrupo(string clave) => usaPesosFijos ? pesosFijos[clave] : pesoParejo;

                    foreach (var grupo in gruposCompetencias)
                    {
                        var pesoGrupo = PesoDelGrupo(grupo.Key);
                        var pesoPorCompetencia = Math.Round(pesoGrupo / grupo.Count(), 2);
                        foreach (var comp in grupo)
                        {
                            _db.FormularioCompetencias.Add(new FormularioCompetencia
                            {
                                IdFormulario = formulario.IdFormulario,
                                IdCompetencia = comp.IdCompetencia,
                                Ponderacion = pesoPorCompetencia,
                            });
                        }
                    }

                    if (indicadoresFormulario.Count > 0)
                    {
                        var pesoGrupoIndicadores = PesoDelGrupo(Constantes.CategoriaIndicadoresGestion);
                        foreach (var ind in indicadoresFormulario)
                        {
                            // A diferencia de las competencias (reparto parejo dentro del grupo),
                            // cada indicador conserva su propia ponderación intra-grupo tal como
                            // viene configurada (ej. 33.33% cada uno) — no se reparte en partes
                            // iguales ni se normaliza a que sume 100%, a propósito (ver
                            // IndicadorGestion.Ponderacion).
                            var pesoAbsoluto = Math.Round(pesoGrupoIndicadores * (ind.Ponderacion / 100m), 2);
                            _db.FormularioIndicadores.Add(new FormularioIndicador
                            {
                                IdFormulario = formulario.IdFormulario,
                                IdIndicador = ind.IdIndicador,
                                Ponderacion = pesoAbsoluto,
                            });
                        }
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
