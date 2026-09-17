using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickingApp.Models
{
    public class Ubicacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BodegaId { get; set; }

        [ForeignKey("BodegaId")]
        public virtual Bodega? Bodega { get; set; }

        [Required(ErrorMessage = "El código de ubicación es obligatorio")]
        [StringLength(50)]
        public string CodigoUbicacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El pasillo es obligatorio")]
        [StringLength(20)]
        public string Pasillo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estante es obligatorio")]
        [StringLength(20)]
        public string Estante { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nivel es obligatorio")]
        [StringLength(20)]
        public string Nivel { get; set; } = string.Empty;

        public bool Activa { get; set; } = true;

        public virtual ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
    }
}
