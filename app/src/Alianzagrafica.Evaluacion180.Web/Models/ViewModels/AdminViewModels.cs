using System.ComponentModel.DataAnnotations;

namespace Alianzagrafica.Evaluacion180.Web.Models.ViewModels;

public class PeriodoListaItemViewModel
{
    public int IdPeriodo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int TotalFormularios { get; set; }
    public int TotalAsignaciones { get; set; }
}

public class CrearPeriodoViewModel
{
    [Required(ErrorMessage = "El nombre del periodo es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre del periodo")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de apertura")]
    public DateTime FechaApertura { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de cierre")]
    public DateTime FechaCierre { get; set; } = DateTime.Today.AddDays(45);
}

public class CompetenciaListaItemViewModel
{
    public int IdCompetencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public string CategoriaTexto => Categoria switch
    {
        Entidades.Constantes.CategoriaOrganizacional => "Organizacional",
        Entidades.Constantes.CategoriaDeRol => "De Rol",
        _ => "Sin categoría",
    };
    public bool Activa { get; set; }
}

public class CrearCompetenciaViewModel
{
    [Required(ErrorMessage = "El nombre de la competencia es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(400)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Tipo de personal (vacío = genérica, aplica a todos)")]
    public int? IdTipoPersonal { get; set; }

    [Display(Name = "Macro-grupo de ponderación (RF-07)")]
    public string? Categoria { get; set; }

    public List<TipoPersonalOpcionViewModel> TiposDisponibles { get; set; } = new();
}

public class TipoPersonalOpcionViewModel
{
    public int IdTipoPersonal { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class EmpleadoListaItemViewModel
{
    public int CodigoEmpleado { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string TipoPersonal { get; set; } = string.Empty;
    public string? JefeDirecto { get; set; }
    public string Estado { get; set; } = string.Empty;

    // Único dato editable desde este sistema (RF-23): número de WhatsApp para el envío del
    // resumen de resultados. Se guarda en ContactoNotificacion, no en Empleado — ver
    // EmpleadosController.ActualizarContacto.
    public string? TelefonoWhatsApp { get; set; }
}

public class AuditoriaItemViewModel
{
    public long IdEvento { get; set; }
    public string? Usuario { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime FechaHora { get; set; }
    public string? DireccionIP { get; set; }
}
