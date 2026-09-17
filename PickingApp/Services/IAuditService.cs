using PickingApp.Models;

namespace PickingApp.Services
{
    public interface IAuditService
    {
        Task LogAsync(string usuario, string rol, string accion, string modulo, string detalle, string? ip = null);
        Task<bool> IsLogEnabledAsync();
        Task SetLogEnabledAsync(bool enabled);
    }
}
