

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Empleados", Schema = "adminkt")]
public class EmpleadoViewModel
{
    [Column("Nombre")]
    public string Nombre { get; set; }
    [Column("Apellidos")]

    public string Apellidos { get; set; }
    [Column("Cedula")]
    public string Cedula { get; set; }
    [Column("Correo")]
    public string Correo { get; set; }
    [Column("Contraseña")]
    public string Contraseña { get; set; }
    [Column("Celular")]
    public string Celular { get; set; }
    [Column("Direccion")]
    public string Direccion { get; set; }
    [Column("Rol")]
    public string Rol { get; set; }
    [Key]
    [Column("idEmpleados")]
    public int idEmpleados { get; set; }
}
