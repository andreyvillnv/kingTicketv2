using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoRelampago.Models
{
    public class CompraViewModel
    {
        public int EventoId { get; set; }
        public string EventoNombre { get; set; }

        [Required]
        public string ClienteNombre { get; set; }

        [Required]
        [EmailAddress]
        public string ClienteEmail { get; set; }

        [Required]
        public string ClienteIdentificacion { get; set; }

        public List<ItemCarrito> Carrito { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Servicio { get; set; }
        public decimal Total { get; set; }
        public bool ProteccionReembolso { get; set; }

        // Datos de pago
        [Required]
        [CreditCard]
        public string NumeroTarjeta { get; set; }

        [Required]
        public string FechaExpiracion { get; set; }

        [Required]
        public string CodigoSeguridad { get; set; }

        [Required]
        public string NombreTarjeta { get; set; }
    }

    public class ItemCarrito
    {
        public string Zona { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string EventoNombre { get; set; }
        public DateTime FechaEvento { get; set; }
    }
}