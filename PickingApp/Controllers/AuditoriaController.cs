using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;
using PickingApp.Services;

namespace PickingApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AuditoriaController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // CU-38: Consultar LOG del sistema
        public async Task<IActionResult> Index(string? modulo, string? usuario, DateTime? fecha)
        {
            var query = _context.LogsSistema.AsQueryable();

            if (!string.IsNullOrEmpty(modulo))
            {
                query = query.Where(l => l.Modulo == modulo);
            }

            if (!string.IsNullOrEmpty(usuario))
            {
                query = query.Where(l => l.Usuario.Contains(usuario.Trim()));
            }

            if (fecha.HasValue)
            {
                var fInicio = fecha.Value.Date;
                var fFin = fInicio.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.FechaHora >= fInicio && l.FechaHora <= fFin);
            }

            var logs = await query
                .OrderByDescending(l => l.FechaHora)
                .Take(200)
                .ToListAsync();

            ViewBag.ModuloFiltro = modulo;
            ViewBag.UsuarioFiltro = usuario;
            ViewBag.FechaFiltro = fecha?.ToString("yyyy-MM-dd");
            ViewBag.Modulos = await _context.LogsSistema.Select(l => l.Modulo).Distinct().ToListAsync();
            ViewBag.LogHabilitado = await _auditService.IsLogEnabledAsync();

            return View(logs);
        }

        // CU-39: Habilitar o deshabilitar registro de LOG y configurar parámetros
        [HttpGet]
        public async Task<IActionResult> Configuracion()
        {
            var configs = await _context.ConfiguracionesSistema.ToListAsync();
            return View(configs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracion(Dictionary<string, string> configuraciones)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";

            foreach (var kvp in configuraciones)
            {
                var cfg = await _context.ConfiguracionesSistema.FirstOrDefaultAsync(c => c.Clave == kvp.Key);
                if (cfg != null)
                {
                    cfg.Valor = kvp.Value;
                }
                else
                {
                    _context.ConfiguracionesSistema.Add(new ConfiguracionSistema { Clave = kvp.Key, Valor = kvp.Value });
                }
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userEmail, "Administrador", "Actualizar Configuración", "Sistema", 
                "Configuración general del sistema actualizada.");

            TempData["SuccessMessage"] = "Parámetros del sistema y auditoría guardados exitosamente.";
            return RedirectToAction(nameof(Configuracion));
        }

        // CU-40: Eliminar registros del LOG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLogs(DateTime? hastaFecha)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";

            IQueryable<LogSistema> query = _context.LogsSistema;
            if (hastaFecha.HasValue)
            {
                var f = hastaFecha.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.FechaHora <= f);
            }

            var registrosAEliminar = await query.ToListAsync();
            _context.LogsSistema.RemoveRange(registrosAEliminar);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userEmail, "Administrador", "Depuración de LOGs", "Auditoría", 
                $"Se eliminaron {registrosAEliminar.Count} registros del historial de LOG.");

            TempData["SuccessMessage"] = $"Se han depurado {registrosAEliminar.Count} registros del LOG del sistema.";
            return RedirectToAction(nameof(Index));
        }

        // CU-15: Limpiar base de datos
        [HttpGet]
        public IActionResult LimpiarBaseDatos()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarLimpiezaBaseDatos(string confirmacion)
        {
            if (confirmacion != "LIMPIAR_SISTEMA")
            {
                TempData["ErrorMessage"] = "Debe escribir exactamente 'LIMPIAR_SISTEMA' para confirmar la operación destructiva.";
                return View("LimpiarBaseDatos");
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";

            // Eliminar dependientes: Novedades, DetallesPedido, Pedidos, Solicitudes, Notificaciones
            _context.NovedadesPedido.RemoveRange(_context.NovedadesPedido);
            _context.DetallesPedido.RemoveRange(_context.DetallesPedido);
            _context.Pedidos.RemoveRange(_context.Pedidos);
            _context.Solicitudes.RemoveRange(_context.Solicitudes);
            _context.Notificaciones.RemoveRange(_context.Notificaciones);

            // Reiniciar estado de empleados a Disponible
            var empleados = await _context.Empleados.ToListAsync();
            foreach (var e in empleados)
            {
                e.EstadoDisponibilidad = "Disponible";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userEmail, "Administrador", "Limpieza de Base de Datos", "Mantenimiento", 
                "Se ejecutó una limpieza completa de datos operativos (pedidos, novedades y solicitudes).");

            TempData["SuccessMessage"] = "La base de datos operativa ha sido limpiada con éxito para pruebas. Los usuarios y maestros se mantuvieron protegidos.";
            return RedirectToAction("Index", "Home");
        }
    }
}