using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickingApp.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El código de pedido es obligatorio")]
        [StringLength(50)]
        public string CodigoPedido { get; set; } = string.Empty;

        [Required]
        public int BodegaId { get; set; }

        [ForeignKey("BodegaId")]
        public virtual Bodega? Bodega { get; set; }

        [Required]
        [StringLength(20)]
        public string Prioridad { get; set; } = "Media"; // "Alta", "Media", "Baja"

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente"; // "Pendiente", "Asignado", "EnProceso", "Completado", "Cancelado", "Reasignado"

        public int? EmpleadoId { get; set; }

        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }

        public DateTime FechaCarga { get; set; } = DateTime.Now;

        public DateTime? FechaAsignacion { get; set; }

        public DateTime? FechaInicioProceso { get; set; }

        public DateTime? FechaFinalizacion { get; set; }

        public double? TiempoEstimadoMinutos { get; set; }

        public double? TiempoProcesamientoMinutos { get; set; }

        [StringLength(250)]
        public string? MotivoCancelacion { get; set; }

        [StringLength(250)]
        public string? MotivoReasignacion { get; set; }

        public bool ConfirmacionFisica { get; set; } = false;

        public virtual ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
        public virtual ICollection<NovedadPedido> Novedades { get; set; } = new List<NovedadPedido>();
    }
}
