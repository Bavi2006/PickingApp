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
    public class EmpleadosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public EmpleadosController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // CU-16: Consultar empleados
        public async Task<IActionResult> Index()
        {
            var empleados = await _context.Empleados
                .Include(e => e.PedidosAsignados)
                .OrderBy(e => e.NombreCompleto)
                .ToListAsync();

            return View(empleados);
        }

        // CU-06 y CU-07: Registrar Empleado
        [HttpGet]
        public IActionResult Crear()
        {
            var model = new Empleado
            {
                FechaIngreso = DateTime.Today,
                HorarioEntrada = new TimeSpan(8, 0, 0),
                HorarioSalida = new TimeSpan(17, 0, 0)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Empleado model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Identificacion = model.Identificacion.Trim();
            if (await _context.Empleados.AnyAsync(e => e.Identificacion == model.Identificacion))
            {
                ModelState.AddModelError("Identificacion", "Ya existe un empleado registrado con esta identificación.");
                return View(model);
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Auxiliar";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Administrador y Supervisor: Creación directa (CU-06)
            if (userRole == "Administrador" || userRole == "Supervisor")
            {
                _context.Empleados.Add(model);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Registrar Empleado", "Empleados", 
                    $"Empleado registrado: {model.Identificacion} - {model.NombreCompleto}");

                TempData["SuccessMessage"] = $"Empleado '{model.NombreCompleto}' registrado exitosamente.";
            }
            // Auxiliar: Solicitar registro de empleado (CU-07)
            else
            {
                var solicitud = new Solicitud
                {
                    TipoSolicitud = "RegistroEmpleado",
                    UsuarioSolicitanteId = userId,
                    Estado = "Pendiente",
                    FechaSolicitud = DateTime.Now,
                    DatosJson = System.Text.Json.JsonSerializer.Serialize(model)
                };

                _context.Solicitudes.Add(solicitud);
                _context.Notificaciones.Add(new Notificacion
                {
                    RolDestino = "Supervisor",
                    Titulo = "Solicitud de Nuevo Empleado",
                    Mensaje = $"El auxiliar {userEmail} solicitó el registro del empleado {model.NombreCompleto}.",
                    Tipo = "Info",
                    FechaCreacion = DateTime.Now,
                    Enlace = "/Solicitudes/Index"
                });

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Solicitud Registro Empleado", "Solicitudes", 
                    $"Auxiliar solicitó registro de empleado: {model.NombreCompleto}");

                TempData["SuccessMessage"] = $"Se ha generado la solicitud para registrar a '{model.NombreCompleto}'. Requiere aprobación de un Supervisor o Administrador.";
            }

            return RedirectToAction(nameof(Index));
        }

        // CU-09 y CU-10: Editar datos de empleado
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            return View(empleado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Empleado model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var empleadoExistente = await _context.Empleados.FindAsync(model.Id);
            if (empleadoExistente == null) return NotFound();

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Auxiliar";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Administrador: Edición directa (CU-09)
            if (userRole == "Administrador")
            {
                empleadoExistente.NombreCompleto = model.NombreCompleto;
                empleadoExistente.Correo = model.Correo;
                empleadoExistente.JornadaLaboral = model.JornadaLaboral;
                empleadoExistente.HorarioEntrada = model.HorarioEntrada;
                empleadoExistente.HorarioSalida = model.HorarioSalida;
                empleadoExistente.FechaIngreso = model.FechaIngreso;
                empleadoExistente.EstadoDisponibilidad = model.EstadoDisponibilidad;

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Editar Empleado", "Empleados", 
                    $"Datos actualizados de empleado: {empleadoExistente.Identificacion}");

                TempData["SuccessMessage"] = $"Datos de '{empleadoExistente.NombreCompleto}' actualizados exitosamente.";
            }
            // Supervisor y Auxiliar: Solicitar edición de empleado (CU-10)
            else
            {
                var solicitud = new Solicitud
                {
                    TipoSolicitud = "EdicionEmpleado",
                    UsuarioSolicitanteId = userId,
                    Estado = "Pendiente",
                    FechaSolicitud = DateTime.Now,
                    DatosJson = System.Text.Json.JsonSerializer.Serialize(model)
                };

                _context.Solicitudes.Add(solicitud);
                _context.Notificaciones.Add(new Notificacion
                {
                    RolDestino = "Administrador",
                    Titulo = "Solicitud de Edición de Empleado",
                    Mensaje = $"El usuario {userEmail} solicitó modificar los datos del empleado {model.NombreCompleto}.",
                    Tipo = "Info",
                    FechaCreacion = DateTime.Now,
                    Enlace = "/Solicitudes/Index"
                });

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Solicitud Edición Empleado", "Solicitudes", 
                    $"Solicitud de edición enviada para empleado {model.NombreCompleto}");

                TempData["SuccessMessage"] = $"Se ha generado la solicitud de modificación para '{model.NombreCompleto}'. Requiere aprobación del Administrador.";
            }

            return RedirectToAction(nameof(Index));
        }

        // CU-12, CU-13: Inactivar empleado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inactivar(int id, string? motivo)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null) return NotFound();

            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Auxiliar";
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Administrador: Inactivación Directa (CU-12)
            if (userRole == "Administrador")
            {
                empleado.Activo = !empleado.Activo;
                empleado.EstadoDisponibilidad = empleado.Activo ? "Disponible" : "Inactivo";
                await _context.SaveChangesAsync();

                var accion = empleado.Activo ? "Reactivar Empleado" : "Inactivar Empleado";
                await _auditService.LogAsync(userEmail, userRole, accion, "Empleados", 
                    $"Estado de empleado {empleado.Identificacion} cambiado a {(empleado.Activo ? "Activo" : "Inactivo")}");

                TempData["SuccessMessage"] = $"El empleado '{empleado.NombreCompleto}' ahora está {(empleado.Activo ? "Activo" : "Inactivo")}.";
            }
            // Supervisor y Auxiliar: Solicitar Inactivación (CU-13)
            else
            {
                var solicitud = new Solicitud
                {
                    TipoSolicitud = "InactivacionEmpleado",
                    UsuarioSolicitanteId = userId,
                    Estado = "Pendiente",
                    FechaSolicitud = DateTime.Now,
                    DatosJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        EmpleadoId = empleado.Id,
                        Identificacion = empleado.Identificacion,
                        NombreCompleto = empleado.NombreCompleto,
                        Motivo = motivo ?? "Solicitud de inactivación por novedades laborales"
                    })
                };

                _context.Solicitudes.Add(solicitud);
                _context.Notificaciones.Add(new Notificacion
                {
                    RolDestino = "Administrador",
                    Titulo = "Solicitud de Inactivación de Empleado",
                    Mensaje = $"El usuario {userEmail} solicitó inactivar al empleado {empleado.NombreCompleto}.",
                    Tipo = "Warning",
                    FechaCreacion = DateTime.Now,
                    Enlace = "/Solicitudes/Index"
                });

                await _context.SaveChangesAsync();

                await _auditService.LogAsync(userEmail, userRole, "Solicitud Inactivar Empleado", "Solicitudes", 
                    $"Solicitud de inactivación para {empleado.NombreCompleto}. Motivo: {motivo}");

                TempData["SuccessMessage"] = $"Se ha generado la solicitud de inactivación para '{empleado.NombreCompleto}'. Requiere aprobación del Administrador.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
