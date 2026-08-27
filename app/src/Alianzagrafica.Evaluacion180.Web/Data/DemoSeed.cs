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
        // Tipo de personal "Conductor": tomado del formato real de evaluación de desempeño de
        // Alianzagrafica para el rol de despachos/transporte (código interno GHU-FOR-007) que se
        // usó como referencia para enriquecer esta demo — ver más abajo, sección "Competencias".
        var conductor = new TipoPersonal { Nombre = Constantes.TipoConductor, PermiteEvaluacionAscendente = false };
        db.TiposPersonal.AddRange(directivo, mandoMedio, administrativo, operario, auxiliarPlanta, conductor);
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
        var conductorDespachos = NuevoEmpleado(2009, "Diego Salazar", "Conductor de Despachos", "Logística", conductor, jefeProduccion.CodigoEmpleado, "diego.salazar@alianzagrafica-demo.com", hoy, ahora);

        db.Empleados.AddRange(gerente, jefeProduccion, jefeAdmin, analistaNomina, operarioOffset, operarioTroquelado, auxiliarBodega, auxiliarAdmin, conductorDespachos);
        await db.SaveChangesAsync();

        // ---- Contacto de WhatsApp (RF-23) — números ficticios, uno por empleado demo, para
        // poder probar el envío del resumen de resultados sin depender de Novasoft (ver
        // ContactoNotificacion.cs: tabla local, separada del dato maestro de Empleado). Se
        // sigue el mismo criterio de datos de ejemplo claramente ficticios usado en el resto
        // de esta siembra (correos @alianzagrafica-demo.com).
        var ahoraContacto = ahora;
        db.ContactosNotificacion.AddRange(
            new ContactoNotificacion { CodigoEmpleado = gerente.CodigoEmpleado, TelefonoWhatsApp = "3000000201", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = jefeProduccion.CodigoEmpleado, TelefonoWhatsApp = "3000000202", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = jefeAdmin.CodigoEmpleado, TelefonoWhatsApp = "3000000203", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = analistaNomina.CodigoEmpleado, TelefonoWhatsApp = "3000000204", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = operarioOffset.CodigoEmpleado, TelefonoWhatsApp = "3000000205", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = operarioTroquelado.CodigoEmpleado, TelefonoWhatsApp = "3000000206", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = auxiliarBodega.CodigoEmpleado, TelefonoWhatsApp = "3000000207", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = auxiliarAdmin.CodigoEmpleado, TelefonoWhatsApp = "3000000208", FechaActualizacion = ahoraContacto },
            new ContactoNotificacion { CodigoEmpleado = conductorDespachos.CodigoEmpleado, TelefonoWhatsApp = "3000000209", FechaActualizacion = ahoraContacto });
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
        var usuarioConductor = NuevoUsuarioDemo(conductorDespachos, ahora);

        db.Usuarios.AddRange(usuarioGerente, usuarioJefeProduccion, usuarioJefeAdmin, usuarioAnalista,
            usuarioOperario1, usuarioOperario2, usuarioAuxBodega, usuarioAuxAdmin, usuarioConductor);
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
            new UsuarioRol { IdUsuario = usuarioAuxAdmin.IdUsuario, IdRol = rolColaborador.IdRol },
            new UsuarioRol { IdUsuario = usuarioConductor.IdUsuario, IdRol = rolColaborador.IdRol });
        await db.SaveChangesAsync();

        // ---- Competencias (genéricas + específicas por tipo de personal) ----
        // Las 4 competencias "organizacionales" (genéricas) y las 5 competencias de "rol" del
        // Conductor se tomaron, con su nombre y definición, del formato real de evaluación de
        // desempeño de Alianzagrafica (código interno GHU-FOR-007 — hoja de cálculo que hoy se
        // diligencia manualmente por colaborador). No se usó ningún dato de personas reales
        // (nombres de trabajadores ni calificaciones) de ese archivo — solo la estructura de
        // competencias y sus definiciones, que son las mismas para cualquier colaborador del
        // cargo, no información personal.
        //
        // Categoria (ajuste posterior, sobre la hoja de ejemplo real "J.LUCUMI" del mismo
        // archivo): las competencias se agrupan en dos macro-grupos de 50% cada uno —
        // "EVALUACION DE COMPETENCIAS ORGANIZACIONALES" (Constantes.CategoriaOrganizacional) y
        // "EVALUACION DE COMPETENCIAS DE ROL" (Constantes.CategoriaDeRol). En esa hoja de
        // ejemplo, "Trabajo en equipo" para el Conductor está SOLO en el grupo "de Rol" (con
        // comportamientos propios de coordinación con el equipo de despachos), no en el grupo
        // organizacional — por eso el Conductor tiene su propia competencia "Trabajo en equipo"
        // (más abajo), distinta de la genérica que sigue aplicando igual al resto de tipos de
        // personal. Ver AsignacionService.GenerarFormulariosAsync para cómo se calcula el peso.
        db.Competencias.AddRange(
            // -- Organizacionales (aplican a todos los tipos de personal) --
            new Competencia
            {
                Nombre = "Adherencia a normas y políticas organizacionales",
                Descripcion = "Capacidad para adaptarse a las normas y políticas de la organización, mostrando compromiso al conocerlas, entenderlas y aplicarlas.",
                IdTipoPersonal = null,
                Categoria = Constantes.CategoriaOrganizacional,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Compromiso con la calidad del trabajo",
                Descripcion = "Capacidad para actuar con minuciosidad, velocidad y sentido de urgencia y tomar decisiones para alcanzar los objetivos del puesto de trabajo, del área u organizacionales, con altos niveles de desempeño.",
                IdTipoPersonal = null,
                Categoria = Constantes.CategoriaOrganizacional,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Trabajo en equipo",
                Descripcion = "Habilidad para interactuar con las personas, escuchar activamente y ser generador de ideas que faciliten la obtención de resultados exitosos, enmarcados en el beneficio común, por encima de los intereses personales.",
                IdTipoPersonal = null,
                Categoria = Constantes.CategoriaOrganizacional,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Eficiencia y productividad",
                Descripcion = "Habilidad para dirigir las propias acciones y/o las de otros de forma que agreguen valor a la organización, alcanzando los objetivos, cumpliendo con el tiempo disponible y con la calidad requerida.",
                IdTipoPersonal = null,
                Categoria = Constantes.CategoriaOrganizacional,
                Activa = true,
            },
            // -- Específicas por tipo de personal --
            new Competencia
            {
                Nombre = "Visión estratégica",
                Descripcion = "Capacidad para definir el rumbo de la organización a mediano y largo plazo, anticipando cambios del entorno y alineando los recursos disponibles.",
                IdTipoPersonal = directivo.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Liderazgo de equipos",
                Descripcion = "Capacidad para dirigir, motivar y desarrollar al equipo a cargo, garantizando el cumplimiento de los objetivos del área.",
                IdTipoPersonal = mandoMedio.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Precisión en el manejo de información",
                Descripcion = "Capacidad para procesar y registrar información administrativa con exactitud, evitando errores que afecten los procesos internos.",
                IdTipoPersonal = administrativo.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Calidad en el proceso productivo",
                Descripcion = "Capacidad para ejecutar el proceso productivo cumpliendo los estándares de calidad y minimizando unidades defectuosas.",
                IdTipoPersonal = operario.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Cumplimiento de normas de seguridad",
                Descripcion = "Capacidad para aplicar de forma consistente las normas de seguridad industrial y el uso adecuado de los elementos de protección personal.",
                IdTipoPersonal = auxiliarPlanta.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            // -- Competencias de rol del Conductor (de la sección "COMPETENCIAS DE ROL" del
            //    formato GHU-FOR-007, adaptadas al rol de despachos/transporte) --
            new Competencia
            {
                Nombre = "Orientación al cliente",
                Descripcion = "Capacidad de generar valor agregado y diferenciador a los clientes internos y externos, indagando, conociendo y resolviendo oportunamente sus necesidades.",
                IdTipoPersonal = conductor.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Trabajo en equipo",
                Descripcion = "Habilidad para interactuar con las personas, escuchar activamente y ser generador de ideas que faciliten resultados exitosos, coordinando con el coordinador y el auxiliar de despachos las actividades de cargue y recogida, por encima de los intereses individuales.",
                IdTipoPersonal = conductor.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Orientación al logro",
                Descripcion = "Gran capacidad para el seguimiento y velocidad en la consecución de los objetivos propuestos, con facilidad y oportunidad para la toma de decisiones que favorezcan a toda la organización.",
                IdTipoPersonal = conductor.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Atención al detalle",
                Descripcion = "Manejo eficaz y prolongado de información detallada, procurando eliminar el error y las duplicidades en el proceso de despacho y entrega.",
                IdTipoPersonal = conductor.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Sentido de la urgencia",
                Descripcion = "Capacidad para percibir la urgencia real de determinadas tareas y actuar con celeridad para alcanzar su realización en plazos breves de tiempo.",
                IdTipoPersonal = conductor.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            },
            new Competencia
            {
                Nombre = "Escucha activa",
                Descripcion = "Escucha activa de las instrucciones recibidas, preguntando hasta que los mensajes estén totalmente claros, y estando alerta a los cambios de la operación.",
                IdTipoPersonal = conductor.IdTipoPersonal,
                Categoria = Constantes.CategoriaDeRol,
                Activa = true,
            });
        await db.SaveChangesAsync();

        // ---- Indicadores de gestión (Entregable 11) ----
        // Tomados, con su nombre, fórmula y ponderación, del formato real de Alianzagrafica
        // "EVALUACION DESEMPEÑO Indicadores" (macro-grupo "INDICADORES DE GESTIÓN", 50% de la
        // nota final). Son genéricos (IdTipoPersonal = null): aplican a los seis tipos de
        // personal, igual que las 4 competencias organizacionales. No se copió ningún dato de
        // persona real de ese archivo — la Meta/Resultado del mes de cada indicador la
        // diligencia cada evaluador, no viene precargada en la siembra.
        //
        // Advertencia de ponderación (confirmada con el usuario, Entregable 11): en el Excel
        // origen, los 4 indicadores quedaron cada uno al 33.33% dentro del grupo, sumando ~133%
        // en vez de 100% — se dejó exactamente así a propósito ("tal y como está en el Excel"),
        // en vez de normalizarlo a que sume 100%. Ver Constantes.PesoIndicadoresGestion y
        // AsignacionService.GenerarFormulariosAsync para cómo se usa este peso.
        db.IndicadoresGestion.AddRange(
            new IndicadorGestion
            {
                Nombre = "Cultura: 5S+1",
                Formula = "Costo de reclamos del cliente ($) facturación.",
                Ponderacion = 33.33m,
                IdTipoPersonal = null,
                Activa = true,
            },
            new IndicadorGestion
            {
                Nombre = "Eficiencia",
                Formula = "Cantidad unidades defectuosas / Cantidad unidades producidas",
                Ponderacion = 33.33m,
                IdTipoPersonal = null,
                Activa = true,
            },
            new IndicadorGestion
            {
                Nombre = "Calidad",
                Formula = "(Horas laboradas - Horas de ausentismo) / Horas totales laboradas",
                Ponderacion = 33.33m,
                IdTipoPersonal = null,
                Activa = true,
            },
            new IndicadorGestion
            {
                Nombre = "Ausentismo",
                Formula = "Rendimiento real / Rendimiento esperado",
                Ponderacion = 33.33m,
                IdTipoPersonal = null,
                Activa = true,
            });
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
