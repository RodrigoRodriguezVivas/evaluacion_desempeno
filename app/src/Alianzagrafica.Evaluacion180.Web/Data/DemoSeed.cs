using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Alianzagrafica.Evaluacion180.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Data;

/// <summary>
/// Siembra un conjunto de datos ficticios (empleados, usuarios, competencias, un periodo
/// abierto y sus asignaciones) para que el ambiente de DEMOSTRACIÓN tenga contenido
/// navegable desde el primer arranque, sin depender de Novasoft ni de un script SQL externo.
///
/// Solo se ejecuta cuando <c>Demo:Habilitado=true</c> (ver appsettings.Demo.json) — nunca en
/// el despliegue real en el IIS de Alianzagrafica. Es idempotente: si ya hay datos, no hace
/// nada, así que es seguro que corra en cada arranque del contenedor.
/// </summary>
public static class DemoSeed
{
    /// <summary>Clave de acceso documentada en el README para el personal "Local" de ejemplo.
    /// En el ambiente de demostración, <c>Auth:ModoPruebasLocal</c> además acepta esta misma
    /// clave para CUALQUIER usuario activo, así que en la práctica sirve para iniciar sesión
    /// como cualquiera de los usuarios sembrados aquí.</summary>
    public const string ClaveDemoPersonalLocal = "Demo2026*";

    public static async Task SembrarSiVacioAsync(AppDbContext db, IPasswordHasher hasher, IAsignacionService asignaciones)
    {
        if (await db.TiposPersonal.AnyAsync()) return;

        // ---- Tipos de personal ----
        var directivo = new TipoPersonal { Nombre = Constantes.TipoDirectivo, PermiteEvaluacionAscendente = true };
        var mandoMedio = new TipoPersonal { Nombre = Constantes.TipoMandoMedio, PermiteEvaluacionAscendente = true };
        var administrativo = new TipoPersonal { Nombre = Constantes.TipoAdministrativo, PermiteEvaluacionAscendente = false };
        var operario = new TipoPersonal { Nombre = Constantes.TipoOperario, PermiteEvaluacionAscendente = false };
        var auxiliarPlanta = new TipoPersonal { Nombre = Constantes.TipoAuxiliarPlanta, PermiteEvaluacionAscendente = false };
        db.TiposPersonal.AddRange(directivo, mandoMedio, administrativo, operario, auxiliarPlanta);
        await db.SaveChangesAsync();

        // ---- Empleados (organigrama ficticio de una empresa gráfica industrial) ----
        var hoy = DateTime.Today;
        var ahora = DateTime.UtcNow;

        var gerente = NuevoEmpleado(2001, "Camila Torres", "Gerente General", "Gerencia", directivo, jefe: null, "camila.torres@alianzagrafica-demo.com", hoy, ahora);
        var jefeProduccion = NuevoEmpleado(2002, "Julián Restrepo", "Jefe de Producción", "Producción", mandoMedio, gerente.CodigoEmpleado, "julian.restrepo@alianzagrafica-demo.com", hoy, ahora);
        var jefeAdmin = NuevoEmpleado(2003, "Marcela Duque", "Jefe Administrativa y Financiera", "Administración", mandoMedio, gerente.CodigoEmpleado, "marcela.duque@alianzagrafica-demo.com", hoy, ahora);
        var analistaNomina = NuevoEmpleado(2004, "Sandra Palacio", "Analista de Nómina", "Administración", administrativo, jefeAdmin.CodigoEmpleado, "sandra.palacio@alianzagrafica-demo.com", hoy, ahora);
        var operarioOffset = NuevoEmpleado(2005, "Andrés Zapata", "Operario Offset", "Producción", operario, jefeProduccion.CodigoEmpleado, "andres.zapata@alianzagrafica-demo.com", hoy, ahora);
        var operarioTroquelado = NuevoEmpleado(2006, "Diana Correa", "Operario de Troquelado", "Producción", operario, jefeProduccion.CodigoEmpleado, "diana.correa@alianzagrafica-demo.com", hoy, ahora);
        var auxiliarBodega = NuevoEmpleado(2007, "Luis Herrera", "Auxiliar de Bodega", "Producción", auxiliarPlanta, jefeProduccion.CodigoEmpleado, "luis.herrera@alianzagrafica-demo.com", hoy, ahora);
        var auxiliarAdmin = NuevoEmpleado(2008, "Paola Giraldo", "Auxiliar Administrativa", "Administración", auxiliarPlanta, jefeAdmin.CodigoEmpleado, "paola.giraldo@alianzagrafica-demo.com", hoy, ahora);

        db.Empleados.AddRange(gerente, jefeProduccion, jefeAdmin, analistaNomina, operarioOffset, operarioTroquelado, auxiliarBodega, auxiliarAdmin);
        await db.SaveChangesAsync();

        // ---- Roles ----
        var rolAdminSistema = new Rol { NombreRol = Constantes.RolAdministradorSistema };
        var rolAdminGH = new Rol { NombreRol = Constantes.RolAdministradorGestionHumana };
        var rolJefe = new Rol { NombreRol = Constantes.RolJefeEvaluador };
        var rolColaborador = new Rol { NombreRol = Constantes.RolColaboradorEvaluado };
        var rolConsultaDirectiva = new Rol { NombreRol = Constantes.RolConsultaDirectiva };
        db.Roles.AddRange(rolAdminSistema, rolAdminGH, rolJefe, rolColaborador, rolConsultaDirectiva);
        await db.SaveChangesAsync();

        // ---- Usuarios: uno por cada empleado, para poder probar cualquier rol ----
        var usuarioGerente = new Usuario { CodigoEmpleado = gerente.CodigoEmpleado, NombreUsuario = gerente.CorreoElectronico!, TipoAutenticacion = Constantes.AutenticacionLocal, ClaveHash = hasher.Hash(ClaveDemoPersonalLocal), Activo = true, FechaCreacion = ahora };
        var usuarioJefeProduccion = NuevoUsuarioDemo(jefeProduccion, ahora);
        var usuarioJefeAdmin = NuevoUsuarioDemo(jefeAdmin, ahora);
        var usuarioAnalista = NuevoUsuarioDemo(analistaNomina, ahora);
        var usuarioOperario1 = NuevoUsuarioDemo(operarioOffset, ahora);
        var usuarioOperario2 = NuevoUsuarioDemo(operarioTroquelado, ahora);
        var usuarioAuxBodega = NuevoUsuarioDemo(auxiliarBodega, ahora);
        var usuarioAuxAdmin = NuevoUsuarioDemo(auxiliarAdmin, ahora);

        db.Usuarios.AddRange(usuarioGerente, usuarioJefeProduccion, usuarioJefeAdmin, usuarioAnalista,
            usuarioOperario1, usuarioOperario2, usuarioAuxBodega, usuarioAuxAdmin);
        await db.SaveChangesAsync();

        db.UsuarioRoles.AddRange(
            new UsuarioRol { IdUsuario = usuarioGerente.IdUsuario, IdRol = rolAdminSistema.IdRol },
            new UsuarioRol { IdUsuario = usuarioGerente.IdUsuario, IdRol = rolAdminGH.IdRol },
            new UsuarioRol { IdUsuario = usuarioGerente.IdUsuario, IdRol = rolConsultaDirectiva.IdRol },
            new UsuarioRol { IdUsuario = usuarioGerente.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioJefeProduccion.IdUsuario, IdRol = rolJefe.IdRol },
            new UsuarioRol { IdUsuario = usuarioJefeProduccion.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioJefeAdmin.IdUsuario, IdRol = rolJefe.IdRol },
            new UsuarioRol { IdUsuario = usuarioJefeAdmin.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioAnalista.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioOperario1.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioOperario2.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioAuxBodega.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioAuxAdmin.IdUsuario, IdRol = rolColaborador.IdRol });
        await db.SaveChangesAsync();

        // ---- Competencias (genéricas + específicas por tipo de personal) ----
        db.Competencias.AddRange(
            new Competencia { Nombre = "Trabajo en equipo", IdTipoPersonal = null, Activa = true },
            new Competencia { Nombre = "Comunicación efectiva", IdTipoPersonal = null, Activa = true },
            new Competencia { Nombre = "Visión estratégica", IdTipoPersonal = directivo.IdTipoPersonal, Activa = true },
            new Competencia { Nombre = "Liderazgo de equipos", IdTipoPersonal = mandoMedio.IdTipoPersonal, Activa = true },
            new Competencia { Nombre = "Precisión en el manejo de información", IdTipoPersonal = administrativo.IdTipoPersonal, Activa = true },
            new Competencia { Nombre = "Calidad en el proceso productivo", IdTipoPersonal = operario.IdTipoPersonal, Activa = true },
            new Competencia { Nombre = "Cumplimiento de normas de seguridad", IdTipoPersonal = auxiliarPlanta.IdTipoPersonal, Activa = true });
        await db.SaveChangesAsync();

        // ---- Periodo de evaluación abierto ----
        var periodo = new PeriodoEvaluacion
        {
            Nombre = "Evaluación de Desempeño 2026 (Demo)",
            FechaApertura = hoy,
            FechaCierre = hoy.AddDays(45),
            Estado = Constantes.PeriodoAbierto,
        };
        db.PeriodosEvaluacion.Add(periodo);
        await db.SaveChangesAsync();

        // Reutiliza la misma lógica de negocio (RF-05, RF-08, RF-09) que usa la app real —
        // no hay generación de asignaciones "de mentiras" distinta a la de producción.
        await asignaciones.GenerarFormulariosAsync(periodo.IdPeriodo);
        await asignaciones.GenerarAsignacionesAsync(periodo.IdPeriodo);
    }

    private static Empleado NuevoEmpleado(int codigo, string nombre, string cargo, string area, TipoPersonal tipo, int? jefe, string correo, DateTime hoy, DateTime ahora) => new()
    {
        CodigoEmpleado = codigo,
        NumeroIdentificacion = codigo.ToString(),
        Nombre = nombre,
        Cargo = cargo,
        Area = area,
        IdTipoPersonal = tipo.IdTipoPersonal,
        CodigoJefeDirecto = jefe,
        CorreoElectronico = correo,
        Estado = Constantes.EstadoEmpleadoActivo,
        FechaIngreso = hoy,
        FechaSincronizacion = ahora,
    };

    private static Usuario NuevoUsuarioDemo(Empleado empleado, DateTime ahora) => new()
    {
        CodigoEmpleado = empleado.CodigoEmpleado,
        NombreUsuario = empleado.CorreoElectronico!,
        TipoAutenticacion = Constantes.AutenticacionLocal,
        Activo = true,
        FechaCreacion = ahora,
    };
}
