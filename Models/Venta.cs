using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoRelampago.Models
{
    [Table("ventas", Schema = "adminkt")]
    public class Venta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cliente_nombre")]
        [StringLength(100)]
        public string ClienteNombre { get; set; }

        [Column("cliente_email")]
        [StringLength(100)]
        public string ClienteEmail { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        public virtual ICollection<VentaDetalle> Detalles { get; set; }
    }
}