using Alianzagrafica.Evaluacion180.Web.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Alianzagrafica.Evaluacion180.Web.Data;

/// <summary>
/// Contexto de datos de EF Core. Mapea el esquema creado por
/// sql/01_esquema_y_datos_ficticios.sql tal cual — este contexto
/// NO ejecuta migraciones ni crea el esquema (Database First):
/// la base de datos debe existir de antemano (ver README).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TipoPersonal> TiposPersonal { get; set; } = null!;
    public DbSet<Empleado> Empleados { get; set; } = null!;
    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<UsuarioRol> UsuarioRoles { get; set; } = null!;
    public DbSet<Competencia> Competencias { get; set; } = null!;
    public DbSet<PeriodoEvaluacion> PeriodosEvaluacion { get; set; } = null!;
    public DbSet<FormularioEvaluacion> FormulariosEvaluacion { get; set; } = null!;
    public DbSet<FormularioCompetencia> FormularioCompetencias { get; set; } = null!;
    public DbSet<AsignacionEvaluacion> AsignacionesEvaluacion { get; set; } = null!;
    public DbSet<RespuestaEvaluacion> RespuestasEvaluacion { get; set; } = null!;
    public DbSet<RespuestaDetalle> RespuestaDetalles { get; set; } = null!;
    public DbSet<ResultadoConsolidado> ResultadosConsolidados { get; set; } = null!;
    public DbSet<Auditoria> Auditorias { get; set; } = null!;
    public DbSet<ContactoNotificacion> ContactosNotificacion { get; set; } = null!;
    public DbSet<EnvioResultadoToken> EnviosResultadoToken { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---- TipoPersonal ----
        modelBuilder.Entity<TipoPersonal>(e =>
        {
            e.ToTable("TipoPersonal");
            e.HasKey(x => x.IdTipoPersonal);
            e.Property(x => x.IdTipoPersonal).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ---- Empleado (tabla ficticia temporal — ver Models/Entidades/Empleado.cs) ----
        modelBuilder.Entity<Empleado>(e =>
        {
            e.ToTable("Empleado");
            e.HasKey(x => x.CodigoEmpleado);
            e.Property(x => x.CodigoEmpleado).ValueGeneratedNever(); // = ID del empleado en Novasoft
            e.Property(x => x.NumeroIdentificacion).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Cargo).HasMaxLength(100).IsRequired();
            e.Property(x => x.Area).HasMaxLength(100).IsRequired();
            e.Property(x => x.CorreoElectronico).HasMaxLength(150);
            e.Property(x => x.Estado).HasMaxLength(10).IsRequired().HasDefaultValue(Constantes.EstadoEmpleadoActivo);
            e.HasIndex(x => x.NumeroIdentificacion).IsUnique();

            e.HasOne(x => x.TipoPersonal)
                .WithMany(t => t.Empleados)
                .HasForeignKey(x => x.IdTipoPersonal)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.JefeDirecto)
                .WithMany(x => x.Colaboradores)
                .HasForeignKey(x => x.CodigoJefeDirecto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Rol ----
        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("Rol");
            e.HasKey(x => x.IdRol);
            e.Property(x => x.IdRol).ValueGeneratedOnAdd();
            e.Property(x => x.NombreRol).HasMaxLength(50).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(250);
            e.HasIndex(x => x.NombreRol).IsUnique();
        });

        // ---- Usuario ----
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).ValueGeneratedOnAdd();
            e.Property(x => x.NombreUsuario).HasMaxLength(100).IsRequired();
            e.Property(x => x.TipoAutenticacion).HasMaxLength(20).IsRequired().HasDefaultValue(Constantes.AutenticacionActiveDirectory);
            e.Property(x => x.Activo).HasDefaultValue(true);
            e.HasIndex(x => x.CodigoEmpleado).IsUnique();
            e.HasIndex(x => x.NombreUsuario).IsUnique();

            e.HasOne(x => x.Empleado)
                .WithOne(emp => emp.Usuario!)
                .HasForeignKey<Usuario>(x => x.CodigoEmpleado)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- UsuarioRol (N:M explícita) ----
        modelBuilder.Entity<UsuarioRol>(e =>
        {
            e.ToTable("UsuarioRol");
            e.HasKey(x => new { x.IdUsuario, x.IdRol });

            e.HasOne(x => x.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Rol)
                .WithMany(r => r.UsuarioRoles)
                .HasForeignKey(x => x.IdRol)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Competencia ----
        modelBuilder.Entity<Competencia>(e =>
        {
            e.ToTable("Competencia");
            e.HasKey(x => x.IdCompetencia);
            e.Property(x => x.IdCompetencia).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(400);
            e.Property(x => x.Categoria).HasMaxLength(30);
            e.Property(x => x.Activa).HasDefaultValue(true);

            e.HasOne(x => x.TipoPersonal)
                .WithMany(t => t.Competencias)
                .HasForeignKey(x => x.IdTipoPersonal)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- PeriodoEvaluacion ----
        modelBuilder.Entity<PeriodoEvaluacion>(e =>
        {
            e.ToTable("PeriodoEvaluacion");
            e.HasKey(x => x.IdPeriodo);
            e.Property(x => x.IdPeriodo).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue(Constantes.PeriodoProgramado);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ---- FormularioEvaluacion ----
        modelBuilder.Entity<FormularioEvaluacion>(e =>
        {
            e.ToTable("FormularioEvaluacion");
            e.HasKey(x => x.IdFormulario);
            e.Property(x => x.IdFormulario).ValueGeneratedOnAdd();
            e.Property(x => x.TipoRelacion).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();

            e.HasOne(x => x.Periodo)
                .WithMany(p => p.Formularios)
                .HasForeignKey(x => x.IdPeriodo)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TipoPersonal)
                .WithMany(t => t.Formularios)
                .HasForeignKey(x => x.IdTipoPersonal)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- FormularioCompetencia (N:M explícita, con ponderación) ----
        modelBuilder.Entity<FormularioCompetencia>(e =>
        {
            e.ToTable("FormularioCompetencia");
            e.HasKey(x => new { x.IdFormulario, x.IdCompetencia });
            e.Property(x => x.Ponderacion).HasPrecision(5, 2);

            e.HasOne(x => x.Formulario)
                .WithMany(f => f.FormularioCompetencias)
                .HasForeignKey(x => x.IdFormulario)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Competencia)
                .WithMany(c => c.FormularioCompetencias)
                .HasForeignKey(x => x.IdCompetencia)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- AsignacionEvaluacion ----
        modelBuilder.Entity<AsignacionEvaluacion>(e =>
        {
            e.ToTable("AsignacionEvaluacion");
            e.HasKey(x => x.IdAsignacion);
            e.Property(x => x.IdAsignacion).ValueGeneratedOnAdd();
            e.Property(x => x.TipoRelacion).HasMaxLength(20).IsRequired();
            e.Property(x => x.Estado).HasMaxLength(20).IsRequired().HasDefaultValue(Constantes.AsignacionProgramada);
            e.HasIndex(x => new { x.IdPeriodo, x.CodigoEvaluador, x.CodigoEvaluado, x.TipoRelacion }).IsUnique();

            e.HasOne(x => x.Periodo)
                .WithMany(p => p.Asignaciones)
                .HasForeignKey(x => x.IdPeriodo)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Evaluador)
                .WithMany()
                .HasForeignKey(x => x.CodigoEvaluador)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Evaluado)
                .WithMany()
                .HasForeignKey(x => x.CodigoEvaluado)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Formulario)
                .WithMany(f => f.Asignaciones)
                .HasForeignKey(x => x.IdFormulario)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- RespuestaEvaluacion ----
        modelBuilder.Entity<RespuestaEvaluacion>(e =>
        {
            e.ToTable("RespuestaEvaluacion");
            e.HasKey(x => x.IdRespuesta);
            e.Property(x => x.IdRespuesta).ValueGeneratedOnAdd();
            e.Property(x => x.Estado).HasMaxLength(10).IsRequired().HasDefaultValue(Constantes.RespuestaBorrador);
            e.HasIndex(x => x.IdAsignacion).IsUnique();

            e.HasOne(x => x.Asignacion)
                .WithOne(a => a.Respuesta!)
                .HasForeignKey<RespuestaEvaluacion>(x => x.IdAsignacion)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- RespuestaDetalle ----
        modelBuilder.Entity<RespuestaDetalle>(e =>
        {
            e.ToTable("RespuestaDetalle");
            e.HasKey(x => new { x.IdRespuesta, x.IdCompetencia });
            e.Property(x => x.Comentario).HasMaxLength(500);

            e.HasOne(x => x.Respuesta)
                .WithMany(r => r.Detalles)
                .HasForeignKey(x => x.IdRespuesta)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Competencia)
                .WithMany()
                .HasForeignKey(x => x.IdCompetencia)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ResultadoConsolidado ----
        modelBuilder.Entity<ResultadoConsolidado>(e =>
        {
            e.ToTable("ResultadoConsolidado");
            e.HasKey(x => new { x.CodigoEvaluado, x.IdPeriodo });
            e.Property(x => x.PromedioAutoevaluacion).HasPrecision(4, 2);
            e.Property(x => x.PromedioJefe).HasPrecision(4, 2);
            e.Property(x => x.PromedioAscendente).HasPrecision(4, 2);
            e.Property(x => x.PromedioGeneral).HasPrecision(4, 2);

            e.HasOne(x => x.Evaluado)
                .WithMany()
                .HasForeignKey(x => x.CodigoEvaluado)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Periodo)
                .WithMany()
                .HasForeignKey(x => x.IdPeriodo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Auditoria ----
        modelBuilder.Entity<Auditoria>(e =>
        {
            e.ToTable("Auditoria");
            e.HasKey(x => x.IdEvento);
            e.Property(x => x.IdEvento).ValueGeneratedOnAdd();
            e.Property(x => x.TipoEvento).HasMaxLength(50).IsRequired();
            e.Property(x => x.Detalle).HasMaxLength(500);
            e.Property(x => x.DireccionIP).HasMaxLength(45);

            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- ContactoNotificacion (módulo de envío de resultados — ver Models/Entidades/ContactoNotificacion.cs
        // sobre por qué vive separada de Empleado) ----
        modelBuilder.Entity<ContactoNotificacion>(e =>
        {
            e.ToTable("ContactoNotificacion");
            e.HasKey(x => x.CodigoEmpleado);
            e.Property(x => x.CodigoEmpleado).ValueGeneratedNever();
            e.Property(x => x.TelefonoWhatsApp).HasMaxLength(30);

            e.HasOne(x => x.Empleado)
                .WithOne()
                .HasForeignKey<ContactoNotificacion>(x => x.CodigoEmpleado)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- EnvioResultadoToken (módulo de envío de resultados — ver Models/Entidades/EnvioResultadoToken.cs) ----
        modelBuilder.Entity<EnvioResultadoToken>(e =>
        {
            e.ToTable("EnvioResultadoToken");
            e.HasKey(x => x.Token);
            e.Property(x => x.Token).HasMaxLength(32);
        });
    }
}
