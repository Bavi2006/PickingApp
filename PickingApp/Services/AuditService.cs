using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;

namespace PickingApp.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsLogEnabledAsync()
        {
            var config = await _context.ConfiguracionesSistema
                .FirstOrDefaultAsync(c => c.Clave == "LogHabilitado");
            
            return config == null || config.Valor.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task SetLogEnabledAsync(bool enabled)
        {
            var config = await _context.ConfiguracionesSistema
                .FirstOrDefaultAsync(c => c.Clave == "LogHabilitado");

            if (config == null)
            {
                _context.ConfiguracionesSistema.Add(new ConfiguracionSistema
                {
                    Clave = "LogHabilitado",
                    Valor = enabled ? "true" : "false"
                });
            }
            else
            {
                config.Valor = enabled ? "true" : "false";
            }

            await _context.SaveChangesAsync();
        }

        public async Task LogAsync(string usuario, string rol, string accion, string modulo, string detalle, string? ip = null)
        {
            try
            {
                if (!await IsLogEnabledAsync()) return;

                var log = new LogSistema
                {
                    FechaHora = DateTime.Now,
                    Usuario = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario,
                    Rol = string.IsNullOrWhiteSpace(rol) ? "Sistema" : rol,
                    Accion = accion,
                    Modulo = modulo,
                    Detalle = detalle,
                    DireccionIp = ip
                };

                _context.LogsSistema.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Fallo silencioso en auditoría para no romper el flujo principal si hay error transitorio
            }
        }
    }
}
