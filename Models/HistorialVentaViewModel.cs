using System;

namespace ProyectoRelampago.Models
{
    public class HistorialVentaViewModel
    {

        public DateTime Fecha { get; set; }
        public string Cliente { get; set; }
        public decimal Total { get; set; }
        public int CantidadBoletos { get; set; }
    }
}