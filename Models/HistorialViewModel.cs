using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoRelampago.Models
{
    public class HistorialViewModel
    {
        [Key]
        public int VentaId { get; set; }
        public int EventoId { get; set; }
        public string ClienteNombre { get; set; }
        public decimal Total { get; set; }
        public int CantidadTickets { get; set; }
        public DateTime Fecha { get; set; }
    }
}