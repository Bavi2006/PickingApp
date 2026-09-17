using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickingApp.Models
{
    public class NovedadPedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }

        [ForeignKey("PedidoId")]
        public virtual Pedido? Pedido { get; set; }

        [Required(ErrorMessage = "La descripción de la novedad es obligatoria")]
        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TipoNovedad { get; set; } = "Faltante"; // "Faltante", "Avería", "Ubicación Incorrecta", "Otro"

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string RegistradoPor { get; set; } = string.Empty;

        public bool Atendida { get; set; } = false;

        [StringLength(100)]
        public string? AtendidaPor { get; set; }

        public DateTime? FechaAtencion { get; set; }

        [StringLength(500)]
        public string? ObservacionesAtencion { get; set; }
    }
}
