using Microsoft.Ajax.Utilities;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoRelampago.Models
{
    public class UusariosViewModel
    {
        [Key]
        public int idusuario { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Pass { get; set; }
        public Boolean activo { get; set; }
        public Boolean estadoAuthen { get; set; }
        public string telefono { get; set; }
        public string codigoAuthen { get; set; }
        public string Rol { get; set; }

    }
}