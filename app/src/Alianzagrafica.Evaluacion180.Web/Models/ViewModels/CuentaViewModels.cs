using System.ComponentModel.DataAnnotations;

namespace Alianzagrafica.Evaluacion180.Web.Models.ViewModels;

public class IniciarSesionViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [Display(Name = "Correo electrónico")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;

    public string? UrlRetorno { get; set; }
    public string? MensajeError { get; set; }
}
