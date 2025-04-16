using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Promociones", Schema = "adminkt")]
public class CodigosViewModel
{
    [Column("codigo")]
    public string codigo { get; set; }

    [Column("mensaje")]
    public string mensaje { get; set; }
    [Column("descuento")]
    public decimal descuento { get; set; }

    [Column("fechaInicio")]
    public DateTime fechaInicio { get; set; }

    [Column("fechaFin")]
    public DateTime fechaFin { get; set; }

    [Column("activo")]
    public bool activo { get; set; }

    [Column("destino")]
    public string destino { get; set; }

    [Key]
    [Column("PromocionID")]
    public int PromocionID { get; set; }

}
