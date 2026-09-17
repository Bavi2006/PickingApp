using System.ComponentModel.DataAnnotations;

namespace PickingApp.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido")]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Rol { get; set; } = "Auxiliar"; // "Administrador", "Supervisor", "Auxiliar"

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Activo"; // "Activo", "Inactivo", "Bloqueado"

        public int IntentosFallidos { get; set; } = 0;

        public DateTime? FechaBloqueo { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public DateTime? UltimoAcceso { get; set; }
    }
}
