using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickingApp.Models
{
    public class Solicitud
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoSolicitud { get; set; } = string.Empty; 
        // "RegistroEmpleado", "EdicionEmpleado", "InactivacionEmpleado", "EliminacionUbicacion", "CambioContrasena"

        [Required]
        public int UsuarioSolicitanteId { get; set; }

        [ForeignKey("UsuarioSolicitanteId")]
        public virtual Usuario? UsuarioSolicitante { get; set; }

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente"; // "Pendiente", "Aprobada", "Rechazada"

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Required]
        public string DatosJson { get; set; } = "{}";

        public int? UsuarioRespondeId { get; set; }

        [ForeignKey("UsuarioRespondeId")]
        public virtual Usuario? UsuarioResponde { get; set; }

        public DateTime? FechaRespuesta { get; set; }

        [StringLength(500)]
        public string? MotivoRechazo { get; set; }
    }
}
