using System.ComponentModel.DataAnnotations;

namespace PickingApp.Models
{
    public class ConfiguracionSistema
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Clave { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Valor { get; set; } = string.Empty;
    }
}
