using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoRelampago.Models
{
    public class FacturaViewModel
    {
        public int EventoId { get; set; }
        public string EventoNombre { get; set; }
        public string EventoUbicacion { get; set; }
        public string TipoEvento { get; set; }
        public string Descripcion { get; set; }
        public string ClienteNombre { get; set; }
        public string img { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Identificacion { get; set; }
        public DateTime Fechainicio { get; set; }
        public string ClienteEmail { get; set; }
        public string ClienteIdentificacion { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Servicio { get; set; }
        public decimal Total { get; set; }
        public bool ProteccionReembolso { get; set; }
        public string NumeroTarjeta { get; set; }
        public string Zona { get; set; }


    }
}