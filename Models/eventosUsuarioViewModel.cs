using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoRelampago.Models
{
    public class eventosUsuariosViewModel
    {
        public string lugar { get; set; }

        public DataType fecha { get; set; }

        public string Descripcion { get; set; }

        public string nombreEvento { get; set; }

    }
}