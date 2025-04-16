using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoRelampago.Models
{
    [Table("Eventos", Schema = "adminkt")]
    public class Evento
    {
        [Key]
        [Column("EventoID")]
        public int Id { get; set; }

        [Required]
        [Column("Nombre")]
        public string Titulo { get; set; }

        [Column("Descripcion")]
        public string Descripcion { get; set; }

        [Column("FechaInicio")]
        public DateTime FechaInicio { get; set; }

        [Column("FechaFin")]
        public DateTime? FechaFin { get; set; }

        [Column("Ubicacion")]
        public string Lugar { get; set; }

        [Column("TipoEvento")]
        public string Categoria { get; set; }

        [Column("Activo")]
        public bool Activo { get; set; }

        [Column("img")]
        public string Img { get; set; }
    }
}