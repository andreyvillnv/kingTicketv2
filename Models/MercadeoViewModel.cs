using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace ProyectoRelampago.Models
{

    [Table("Mercadeo", Schema = "adminkt")]
    public class MercadeoViewModel
    {
        [Column("destino")]
        public string destino{ get; set; }
        [Column("fechaInicio")]
        public DateTime fechaInicio { get; set; }
        [Column("fechaFin")]
        public DateTime fechaFin { get; set; }
        [Column("mensaje")]
        public string mensaje { get; set; }

        [Column("asunto")]
        public string asunto { get; set; }

        [Key]
        [Column("idmercadeo")]
        public int idmercadeo { get; set; }
    }
}