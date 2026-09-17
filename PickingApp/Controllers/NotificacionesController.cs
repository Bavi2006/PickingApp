using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;

namespace PickingApp.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // CU-43: Mostrar notificaciones internas
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Auxiliar";

            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioDestinoId == userId || n.RolDestino == userRole || (n.UsuarioDestinoId == null && n.RolDestino == null))
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50)
                .ToListAsync();

            return View(notificaciones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var notificacion = await _context.Notificaciones.FindAsync(id);
            if (notificacion != null)
            {
                notificacion.Leida = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}