using System.ComponentModel.DataAnnotations;

namespace PickingApp.Models
{
    public class Notificacion
    {
        [Key]
        public int Id { get; set; }

        public int? UsuarioDestinoId { get; set; }

        [StringLength(50)]
        public string? RolDestino { get; set; } // "Administrador", "Supervisor", "Auxiliar" o null

        [Required]
        [StringLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Mensaje { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "Info"; // "Info", "Warning", "Success", "Danger"

        public bool Leida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Enlace { get; set; }
    }
}
