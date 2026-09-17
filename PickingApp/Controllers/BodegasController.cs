using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;
using PickingApp.Services;

namespace PickingApp.Controllers
{
    [Authorize]
    public class BodegasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public BodegasController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // CU-22: Consultar bodegas y ubicaciones
        public async Task<IActionResult> Index()
        {
            var bodegas = await _context.Bodegas
                .Include(b => b.Ubicaciones)
                .Include(b => b.Pedidos)
                .OrderBy(b => b.Codigo)
                .ToListAsync();

            return View(bodegas);
        }

        // Detalle de Bodega y sus Ubicaciones
        public async Task<IActionResult> Detalle(int id)
        {
            var bodega = await _context.Bodegas
                .Include(b => b.Ubicaciones)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bodega == null)
            {
                return NotFound();
            }

            return View(bodega);
        }

        // CU-18: Crear bodega (Solo Administrador)
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Bodega model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Codigo = model.Codigo.Trim().ToUpper();
            if (await _context.Bodegas.AnyAsync(b => b.Codigo == model.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe una bodega registrada con ese código.");
                return View(model);
            }

            model.FechaCreacion = DateTime.Now;
            _context.Bodegas.Add(model);
            await _context.SaveChangesAsync();

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            await _auditService.LogAsync(userEmail, "Administrador", "Crear Bodega", "Bodegas", 
                $"Bodega creada: {model.Codigo} - {model.Nombre}");

            TempData["SuccessMessage"] = $"Bodega '{model.Nombre}' ({model.Codigo}) creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // CU-19: Crear ubicación (Administrador y Supervisor)
        [Authorize(Roles = "Administrador,Supervisor")]
        [HttpGet]
        public async Task<IActionResult> CrearUbicacion(int bodegaId)
        {
            var bodega = await _context.Bodegas.FindAsync(bodegaId);
            if (bodega == null) return NotFound();

            ViewBag.Bodega = bodega;
            var model = new Ubicacion { BodegaId = bodegaId };
            return View(model);
        }

        [Authorize(Roles = "Administrador,Supervisor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUbicacion(Ubicacion model)
        {
            var bodega = await _context.Bodegas.FindAsync(model.BodegaId);
            if (bodega == null) return NotFound();
            ViewBag.Bodega = bodega;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generar o estandarizar código de ubicación si no viene completo
            if (string.IsNullOrWhiteSpace(model.CodigoUbicacion))
            {
                model.CodigoUbicacion = $"PAS-{model.Pasillo.Trim().ToUpper()}-EST-{model.Estante.Trim().ToUpper()}-NIV-{model.Nivel.Trim().ToUpper()}";
            }
            else
            {
                model.CodigoUbicacion = model.CodigoUbicacion.Trim().ToUpper();
            }

            if (await _context.Ubicaciones.AnyAsync(u => u.BodegaId == model.BodegaId && u.CodigoUbicacion == model.CodigoUbicacion))
            {
                ModelState.AddModelError("CodigoUbicacion", "Ya existe una ubicación con ese código en esta bodega.");
                return View(model);
            }

            _context.Ubicaciones.Add(model);
            await _context.SaveChangesAsync();

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
            await _auditService.LogAsync(userEmail, userRole, "Crear Ubicación", "Ubicaciones", 
                $"Ubicación creada en bodega {bodega.Codigo}: {model.CodigoUbicacion}");

            TempData["SuccessMessage"] = $"Ubicación '{model.CodigoUbicacion}' creada con éxito.";
            return RedirectToAction(nameof(Detalle), new { id = model.BodegaId });
        }

        // CU-20 y CU-21: Eliminar o Solicitar Eliminación de Ubicación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUbicacion(int id, string? motivo)
        {
            var ubicacion = await _context.Ubicaciones
                .Include(u => u.Bodega)
                .Include(u => u.DetallesPedido)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (ubicacion == null) return NotFound();

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Si tiene pedidos asociados, no se puede eliminar
            if (ubicacion.DetallesPedido.Any())
            {
                TempData["ErrorMessage"] = "No se puede eliminar la ubicación porque tiene pedidos asociados históricos.";
                return RedirectToAction(nameof(Detalle), new { id = ubicacion.BodegaId });
            }

            // Administrador: Eliminación Directa
            if (userRole == "Administrador")
            {
                _context.Ubicaciones.Remove(ubicacion);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Eliminar Ubicación", "Ubicaciones", 
                    $"Ubicación {ubicacion.CodigoUbicacion} eliminada directamente por el Administrador.");

                TempData["SuccessMessage"] = $"Ubicación '{ubicacion.CodigoUbicacion}' eliminada del sistema.";
            }
            // Supervisor: Solicitar Eliminación (CU-20)
            else if (userRole == "Supervisor")
            {
                var solicitud = new Solicitud
                {
                    TipoSolicitud = "EliminacionUbicacion",
                    UsuarioSolicitanteId = userId,
                    Estado = "Pendiente",
                    FechaSolicitud = DateTime.Now,
                    DatosJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        UbicacionId = ubicacion.Id,
                        CodigoUbicacion = ubicacion.CodigoUbicacion,
                        BodegaCodigo = ubicacion.Bodega?.Codigo,
                        Motivo = motivo ?? "Solicitud de eliminación por cambio operativo"
                    })
                };

                _context.Solicitudes.Add(solicitud);
                _context.Notificaciones.Add(new Notificacion
                {
                    RolDestino = "Administrador",
                    Titulo = "Solicitud de Eliminación de Ubicación",
                    Mensaje = $"El supervisor {userEmail} solicitó eliminar la ubicación {ubicacion.CodigoUbicacion}.",
                    Tipo = "Warning",
                    FechaCreacion = DateTime.Now,
                    Enlace = "/Solicitudes/Index"
                });

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Solicitud Eliminación Ubicación", "Solicitudes", 
                    $"Supervisor solicitó eliminación de ubicación {ubicacion.CodigoUbicacion}. Motivo: {motivo}");

                TempData["SuccessMessage"] = $"Se ha generado la solicitud de eliminación para la ubicación '{ubicacion.CodigoUbicacion}'. Requiere aprobación del Administrador.";
            }
            else
            {
                TempData["ErrorMessage"] = "No tienes permisos para realizar o solicitar esta acción.";
            }

            return RedirectToAction(nameof(Detalle), new { id = ubicacion.BodegaId });
        }
    }
}
