namespace Alianzagrafica.Evaluacion180.Web.Models.Entidades;

public class Usuario
{
    public int IdUsuario { get; set; }
    public int CodigoEmpleado { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string TipoAutenticacion { get; set; } = Constantes.AutenticacionActiveDirectory;
    public byte[]? ClaveHash { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public Empleado Empleado { get; set; } = null!;
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

public class UsuarioRol
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}
