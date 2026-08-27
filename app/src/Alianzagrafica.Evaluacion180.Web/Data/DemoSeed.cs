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

        // ---- Competencias y comportamientos (Entregable 13) ----
        // Reemplaza el catálogo anterior (competencias específicas por tipo de personal —
        // "Visión estratégica" del Directivo, "Liderazgo de equipos" del Mando medio, las 6 del
        // Conductor, etc.) por el del formato real "EVALUACION DESEMPEÑO_Evaluaciones" de
        // Alianzagrafica (Excel adjuntado por el usuario), que desglosa cada competencia en sus
        // comportamientos observables (columna "COMPORTAMIENTOS"). A pedido explícito del usuario
        // ("mismo listado para todos los perfiles"), es un catálogo ÚNICO y genérico
        // (IdTipoPersonal = null): las mismas 6 competencias (3 Organizacional + 3 DeRol) y sus 20
        // comportamientos aplican igual a los seis tipos de personal (Directivo, Mando medio,
        // Administrativo, Operario, Auxiliar de planta, Conductor). No se usó ningún dato de
        // personas reales del Excel (nombres ni calificaciones) — solo la estructura de
        // competencias/comportamientos y sus definiciones.
        //
        // La "NOTA FINAL" de cada competencia (RespuestaDetalle.Calificacion) es el promedio de
        // sus comportamientos ya calificados — calculado en el servidor a partir de
        // RespuestaComportamientoDetalle, nunca a partir de un total posteado directamente (ver
        // Comportamiento.cs y EvaluacionesController.Guardar). Categoria sigue igual que antes:
        // "Organizacional" (20% del total) y "DeRol" (30% del total) — ver Constantes.
        var compAdherencia = new Competencia
        {
            Nombre = "Adherencia a normas y políticas organizacionales",
            Descripcion = "Capacidad para adaptarse a las normas y políticas de la organización, mostrando compromiso al conocerlas, entenderlas y aplicarlas.",
            IdTipoPersonal = null,
            Categoria = Constantes.CategoriaOrganizacional,
            Activa = true,
        };
        var compCalidadTrabajo = new Competencia
        {
            Nombre = "Compromiso con la calidad de trabajo",
            Descripcion = "Capacidad para actuar con minuciosidad, velocidad y sentido de urgencia y tomar decisiones para alcanzar los objetivos de su puesto de trabajo, del área, u organizacionales, con altos niveles de desempeño.",
            IdTipoPersonal = null,
            Categoria = Constantes.CategoriaOrganizacional,
            Activa = true,
        };
        var compEficienciaProductividad = new Competencia
        {
            Nombre = "Eficiencia y Productividad",
            Descripcion = "Habilidad para dirigir las propias acciones y/o las de otros de forma que agreguen valor a la organización, alcanzando los objetivos, cumpliendo con el tiempo disponible y con la calidad requerida.",
            IdTipoPersonal = null,
            Categoria = Constantes.CategoriaOrganizacional,
            Activa = true,
        };
        var compAtencionDetalle = new Competencia
        {
            Nombre = "Atención al detalle",
            Descripcion = "Capacidad para identificar, evaluar y controlar los detalles que comprende una acción o actividad, verificando la calidad y el procedimiento, para evitar afectaciones en la gestión.",
            IdTipoPersonal = null,
            Categoria = Constantes.CategoriaDeRol,
            Activa = true,
        };
        var compCalidadDeRol = new Competencia
        {
            Nombre = "Calidad de trabajo",
            Descripcion = "Capacidad para determinar eficazmente las metas y prioridades de su tarea/área/proyecto estipulando la acción, los plazos y los recursos requeridos.",
            IdTipoPersonal = null,
            Categoria = Constantes.CategoriaDeRol,
            Activa = true,
        };
        var compPlanificacionSeguimiento = new Competencia
        {
            Nombre = "Planificación y seguimiento",
            Descripcion = "Es la capacidad de identificar y determinar de forma efectiva sus prioridades estableciendo fechas, actividades y responsables.",
            IdTipoPersonal = null,
            Categoria = Constantes.CategoriaDeRol,
            Activa = true,
        };

        db.Competencias.AddRange(compAdherencia, compCalidadTrabajo, compEficienciaProductividad,
            compAtencionDetalle, compCalidadDeRol, compPlanificacionSeguimiento);
        await db.SaveChangesAsync(); // asigna IdCompetencia para poder enlazar comportamientos

        db.Comportamientos.AddRange(
            // -- Adherencia a normas y políticas organizacionales (6 comportamientos) --
            NuevoComportamiento(compAdherencia, 1, "Cumple con las normas y procedimientos establecidos por la compañía."),
            NuevoComportamiento(compAdherencia, 2, "Utiliza los elementos de protección personal."),
            NuevoComportamiento(compAdherencia, 3, "Porta el uniforme adecuadamente, conforme a las políticas de la compañía."),
            NuevoComportamiento(compAdherencia, 4, "Se dirige con respeto frente a su jefe y compañeros."),
            NuevoComportamiento(compAdherencia, 5, "Cuenta con disposición para el trabajo adicional cuando la compañía lo requiere."),
            NuevoComportamiento(compAdherencia, 6, "Cumple con los horarios establecidos para su turno de trabajo."),
            // -- Compromiso con la calidad de trabajo (3 comportamientos) --
            NuevoComportamiento(compCalidadTrabajo, 1, "Utiliza métodos estructurados para definir las actividades necesarias durante el proceso, para lograr el resultado esperado (producto)."),
            NuevoComportamiento(compCalidadTrabajo, 2, "Evalúa los posibles riesgos, consecuencias e impactos negativos que se pueden obtener como consecuencia de la falta de control de proceso."),
            NuevoComportamiento(compCalidadTrabajo, 3, "Toma decisiones y emprende acciones de mejora en base al análisis de los resultados obtenidos."),
            // -- Eficiencia y Productividad (3 comportamientos) --
            NuevoComportamiento(compEficienciaProductividad, 1, "Mantiene un buen nivel de actividad, variando su ritmo en función del tiempo disponible y realizando su trabajo según los tiempos establecidos."),
            NuevoComportamiento(compEficienciaProductividad, 2, "Se esfuerza por aumentar el volumen de trabajo realizado, sin descuidar la calidad."),
            NuevoComportamiento(compEficienciaProductividad, 3, "Comprueba que la calidad y los beneficios obtenidos de su trabajo son los esperados."),
            // -- Atención al detalle (5 comportamientos) --
            NuevoComportamiento(compAtencionDetalle, 1, "Lee e interpreta la orden de producción."),
            NuevoComportamiento(compAtencionDetalle, 2, "Realiza un adecuado despeje de línea al iniciar la inspección de cada producto."),
            NuevoComportamiento(compAtencionDetalle, 3, "Empaca los productos conforme a las especificaciones de la orden de producción o manual de empaque."),
            NuevoComportamiento(compAtencionDetalle, 4, "Realiza el control de cierre al finalizar la revisión de la orden de producción."),
            NuevoComportamiento(compAtencionDetalle, 5, "Evita mezclas de producto en todas las referencias procesadas."),
            // -- Calidad de trabajo (1 comportamiento) --
            NuevoComportamiento(compCalidadDeRol, 1, "Informa las no conformidades observadas durante la realización de procesos de inspección."),
            // -- Planificación y seguimiento (2 comportamientos) --
            NuevoComportamiento(compPlanificacionSeguimiento, 1, "Marca las fajillas y paquetes con el número que le corresponde."),
            NuevoComportamiento(compPlanificacionSeguimiento, 2, "Revisa y segrega eficientemente cada producto inspeccionado, ya sea lateral y AUT, en mesa o plano."));
        await db.SaveChangesAsync();

        // ---- Indicadores de gestión (Entregable 11) ----
        // Tomados, con su nombre, fórmula y ponderación, del formato real de Alianzagrafica
        // "EVALUACION DESEMPEÑO Indicadores" (macro-grupo "INDICADORES DE GESTIÓN", 50% de la
        // nota final). Son genéricos (IdTipoPersonal = null): aplican a los seis tipos de
        // personal, igual que las 3 competencias organizacionales (Entregable 13). No se copió ningún dato de
        // persona real de ese archivo — la Meta/Resultado del mes de cada indicador la
        // diligencia cada evaluador, no viene precargada en la siembra.
        //
        // Advertencia de ponderación (confirmada con el usuario, Entregable 11): en el Excel
        // origen, los 4 indicadores quedaron cada uno al 33.33% dentro del grupo, sumando ~133%
        // en vez de 100% — se dejó exactamente así a propósito ("tal y como está en el Excel"),
        // en vez de normalizarlo a que sume 100%. Ver Constantes.PesoIndicadoresGestion y
        // AsignacionService.GenerarFormulariosAsync para cómo se usa este peso.
        // Meta fija por indicador (Entregable 12, a pedido explícito del usuario): ya no la
        // escribe el evaluador cada vez, es un valor del catálogo.
        db.IndicadoresGestion.AddRange(
            new IndicadorGestion
            {
                Nombre = "Cultura: 5S+1",
                Formula = "Costo de reclamos del cliente ($) facturación.",
                Ponderacion = 33.33m,
                Meta = 90m,
                IdTipoPersonal = null,
                Activa = true,
            },
            new IndicadorGestion
            {
                Nombre = "Eficiencia",
                Formula = "Cantidad unidades defectuosas / Cantidad unidades producidas",
                Ponderacion = 33.33m,
                Meta = 90m,
                IdTipoPersonal = null,
                Activa = true,
            },
            new IndicadorGestion
            {
                Nombre = "Calidad",
                Formula = "(Horas laboradas - Horas de ausentismo) / Horas totales laboradas",
                Ponderacion = 33.33m,
                Meta = 100m,
                IdTipoPersonal = null,
                Activa = true,
            },
            new IndicadorGestion
            {
                Nombre = "Ausentismo",
                Formula = "Rendimiento real / Rendimiento esperado",
                Ponderacion = 33.33m,
                Meta = 90m,
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

    /// <summary>Comportamiento de una competencia (Entregable 13) — ver Comportamiento.cs.</summary>
    private static Comportamiento NuevoComportamiento(Competencia competencia, int orden, string descripcion) => new()
    {
        IdCompetencia = competencia.IdCompetencia,
        Orden = orden,
        Descripcion = descripcion,
        Activo = true,
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
