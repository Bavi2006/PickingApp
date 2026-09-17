using System.ComponentModel.DataAnnotations;

namespace PickingApp.Models
{
    public class Bodega
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El código de bodega es obligatorio")]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la bodega es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
