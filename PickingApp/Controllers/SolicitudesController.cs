using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;
using PickingApp.Services;

namespace PickingApp.Controllers
{
    [Authorize]
    public class SolicitudesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public SolicitudesController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // Bandeja de aprobación para Administrador y Supervisor
        [Authorize(Roles = "Administrador,Supervisor")]
        public async Task<IActionResult> Index(string? estado = null)
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var query = _context.Solicitudes
                .Include(s => s.UsuarioSolicitante)
                .Include(s => s.UsuarioResponde)
                .AsQueryable();

            // Supervisor solo puede aprobar/rechazar solicitudes de Registro de Empleado (CU-08)
            if (userRole == "Supervisor")
            {
                query = query.Where(s => s.TipoSolicitud == "RegistroEmpleado");
            }

            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(s => s.Estado == estado);
            }
            else
            {
                // Por defecto mostrar pendientes primero
                query = query.OrderByDescending(s => s.Estado == "Pendiente")
                             .ThenByDescending(s => s.FechaSolicitud);
            }

            var solicitudes = await query.ToListAsync();
            ViewBag.FiltroEstado = estado;
            return View(solicitudes);
        }

        // CU-17: Consultar estado de solicitudes propias (Auxiliar y Supervisor)
        public async Task<IActionResult> MisSolicitudes()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var misSolicitudes = await _context.Solicitudes
                .Include(s => s.UsuarioResponde)
                .Where(s => s.UsuarioSolicitanteId == userId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(misSolicitudes);
        }

        // Aprobar Solicitud (CU-08, CU-11, CU-14, CU-21, CU-41)
        [Authorize(Roles = "Administrador,Supervisor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.UsuarioSolicitante)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null || solicitud.Estado != "Pendiente")
            {
                TempData["ErrorMessage"] = "La solicitud no se encuentra disponible para aprobación.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Supervisor solo puede aprobar registro de empleado
            if (userRole == "Supervisor" && solicitud.TipoSolicitud != "RegistroEmpleado")
            {
                TempData["ErrorMessage"] = "Los Supervisores solo tienen permiso para aprobar registros de empleados.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Ejecutar la acción según el tipo de solicitud
                switch (solicitud.TipoSolicitud)
                {
                    case "RegistroEmpleado":
                        var empleado = JsonSerializer.Deserialize<Empleado>(solicitud.DatosJson);
                        if (empleado != null)
                        {
                            empleado.Id = 0; // Para nuevo registro
                            _context.Empleados.Add(empleado);
                        }
                        break;

                    case "EdicionEmpleado":
                        var empData = JsonSerializer.Deserialize<Empleado>(solicitud.DatosJson);
                        if (empData != null)
                        {
                            var empExistente = await _context.Empleados.FindAsync(empData.Id);
                            if (empExistente != null)
                            {
                                empExistente.NombreCompleto = empData.NombreCompleto;
                                empExistente.Correo = empData.Correo;
                                empExistente.JornadaLaboral = empData.JornadaLaboral;
                                empExistente.HorarioEntrada = empData.HorarioEntrada;
                                empExistente.HorarioSalida = empData.HorarioSalida;
                                empExistente.FechaIngreso = empData.FechaIngreso;
                                empExistente.EstadoDisponibilidad = empData.EstadoDisponibilidad;
                            }
                        }
                        break;

                    case "InactivacionEmpleado":
                        using (var doc = JsonDocument.Parse(solicitud.DatosJson))
                        {
                            if (doc.RootElement.TryGetProperty("EmpleadoId", out var empIdProp))
                            {
                                var emp = await _context.Empleados.FindAsync(empIdProp.GetInt32());
                                if (emp != null)
                                {
                                    emp.Activo = false;
                                    emp.EstadoDisponibilidad = "Inactivo";
                                }
                            }
                        }
                        break;

                    case "EliminacionUbicacion":
                        using (var doc = JsonDocument.Parse(solicitud.DatosJson))
                        {
                            if (doc.RootElement.TryGetProperty("UbicacionId", out var ubiIdProp))
                            {
                                var ubi = await _context.Ubicaciones.FindAsync(ubiIdProp.GetInt32());
                                if (ubi != null)
                                {
                                    _context.Ubicaciones.Remove(ubi);
                                }
                            }
                        }
                        break;

                    case "CambioContrasena":
                        var usuario = await _context.Usuarios.FindAsync(solicitud.UsuarioSolicitanteId);
                        if (usuario != null)
                        {
                            // Asignar contraseña temporal estandarizada
                            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Picking2026*");
                            usuario.Estado = "Activo";
                            usuario.IntentosFallidos = 0;
                            usuario.FechaBloqueo = null;
                        }
                        break;
                }

                solicitud.Estado = "Aprobada";
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.UsuarioRespondeId = userId;

                // Notificar al usuario solicitante (CU-43)
                _context.Notificaciones.Add(new Notificacion
                {
                    UsuarioDestinoId = solicitud.UsuarioSolicitanteId,
                    Titulo = "Solicitud Aprobada",
                    Mensaje = $"Tu solicitud de {solicitud.TipoSolicitud} ha sido aprobada por {userEmail}.",
                    Tipo = "Success",
                    FechaCreacion = DateTime.Now,
                    Enlace = "/Solicitudes/MisSolicitudes"
                });

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Aprobar Solicitud", "Solicitudes", 
                    $"Solicitud #{solicitud.Id} ({solicitud.TipoSolicitud}) aprobada.");

                TempData["SuccessMessage"] = $"Solicitud #{solicitud.Id} aprobada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al procesar la aprobación: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Rechazar Solicitud (CU-08, CU-11, CU-14, CU-21, CU-41)
        [Authorize(Roles = "Administrador,Supervisor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string motivoRechazo)
        {
            if (string.IsNullOrWhiteSpace(motivoRechazo))
            {
                TempData["ErrorMessage"] = "Debe indicar un motivo de rechazo obligatorio.";
                return RedirectToAction(nameof(Index));
            }

            var solicitud = await _context.Solicitudes.FindAsync(id);
            if (solicitud == null || solicitud.Estado != "Pendiente")
            {
                TempData["ErrorMessage"] = "La solicitud no se encuentra disponible para rechazo.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            solicitud.Estado = "Rechazada";
            solicitud.MotivoRechazo = motivoRechazo.Trim();
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.UsuarioRespondeId = userId;

            // Notificar al usuario solicitante (CU-43)
            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioDestinoId = solicitud.UsuarioSolicitanteId,
                Titulo = "Solicitud Rechazada",
                Mensaje = $"Tu solicitud de {solicitud.TipoSolicitud} fue rechazada. Motivo: {motivoRechazo}",
                Tipo = "Danger",
                FechaCreacion = DateTime.Now,
                Enlace = "/Solicitudes/MisSolicitudes"
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userEmail, userRole, "Rechazar Solicitud", "Solicitudes", 
                $"Solicitud #{solicitud.Id} rechazada. Motivo: {motivoRechazo}");

            TempData["SuccessMessage"] = $"Solicitud #{solicitud.Id} rechazada.";
            return RedirectToAction(nameof(Index));
        }
    }
}
