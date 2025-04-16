using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoRelampago.Models
{
    [Table("ventasDetalles", Schema = "adminkt")]
    public class VentaDetalle
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("venta_id")]
        public int VentaId { get; set; }

        [Column("evento_id")]
        public int EventoId { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [ForeignKey("VentaId")]
        public virtual Venta Venta { get; set; }
    }
}