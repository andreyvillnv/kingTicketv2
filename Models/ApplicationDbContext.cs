using System.Data.Entity;
using ProyectoRelampago.Models;

namespace ProyectoRelampago
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("name=DefaultConnection")
        {
            // Configuración para evitar problemas de inicialización
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public DbSet<Evento> Eventos { get; set; }
        // Definir DbSet para Ventas
        public DbSet<Venta> Ventas { get; set; }

        // Definir DbSet para Detalles de Venta
        public DbSet<VentaDetalle> VentaDetalles { get; set; }

        public DbSet<HistorialViewModel> VentaHistorialViewModel { get; set; }

        //  public DbSet<eventosUsuariosViewModel> EventosUser { get; set; }
        public DbSet<EmpleadoViewModel> Empleados { get; set; }

        public DbSet<MercadeoViewModel> Mercadeo { get; set; }

        public DbSet<CodigosViewModel> Codigos { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Especificamos el esquema si no lo hemos hecho con atributos
            modelBuilder.HasDefaultSchema("adminkt");
            base.OnModelCreating(modelBuilder);
        }
    }
}