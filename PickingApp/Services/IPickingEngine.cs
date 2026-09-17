using PickingApp.Models;

namespace PickingApp.Services
{
    public class AsignacionValidationResult
    {
        public bool PuedeAsignar { get; set; }
        public string? MensajeError { get; set; }
        public bool GeneraAdvertencia { get; set; }
        public string? MensajeAdvertencia { get; set; }
        public double TiempoEstimadoMinutos { get; set; }
    }

    public interface IPickingEngine
    {
        Task<AsignacionValidationResult> ValidarAsignacionAsync(int pedidoId, int empleadoId);
        Task<double> CalcularTiempoEstimadoAsync(int pedidoId, int empleadoId);
        double CalcularTiempoProcesamiento(DateTime fechaAsignacion, DateTime fechaFinalizacion, int margenMinutos = 2);
    }
}
