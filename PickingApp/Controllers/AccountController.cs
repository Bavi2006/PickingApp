using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;
using PickingApp.Models.ViewModels;
using PickingApp.Services;

namespace PickingApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AccountController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo.ToLower() == model.Correo.Trim().ToLower());

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Credenciales de acceso inválidas.");
                await _auditService.LogAsync(model.Correo, "Desconocido", "Fallo de Autenticación", "Autenticación", 
                    $"Intento fallido con correo no registrado: {model.Correo}", ipAddress);
                return View(model);
            }

            if (usuario.Estado == "Bloqueado")
            {
                ModelState.AddModelError(string.Empty, 
                    "Tu cuenta se encuentra bloqueada por superar los 5 intentos consecutivos fallidos. Contacta a un Administrador.");
                await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Acceso Bloqueado", "Autenticación", 
                    "Intento de acceso a cuenta bloqueada", ipAddress);
                return View(model);
            }

            if (usuario.Estado == "Inactivo")
            {
                ModelState.AddModelError(string.Empty, "Tu cuenta se encuentra inactiva. Contacta al Administrador.");
                await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Acceso Inactivo", "Autenticación", 
                    "Intento de acceso a cuenta inactiva", ipAddress);
                return View(model);
            }

            // Validar hash de contraseña
            bool passwordValida = false;
            try
            {
                passwordValida = BCrypt.Net.BCrypt.Verify(model.Password, usuario.PasswordHash);
            }
            catch
            {
                passwordValida = false;
            }

            if (!passwordValida)
            {
                usuario.IntentosFallidos++;

                // RNF-06: Bloqueo a los 5 intentos consecutivos excepto Administrador
                if (usuario.IntentosFallidos >= 5 && usuario.Rol != "Administrador")
                {
                    usuario.Estado = "Bloqueado";
                    usuario.FechaBloqueo = DateTime.Now;

                    _context.Notificaciones.Add(new Notificacion
                    {
                        RolDestino = "Administrador",
                        Titulo = "Cuenta Bloqueada",
                        Mensaje = $"La cuenta del usuario {usuario.NombreCompleto} ({usuario.Correo}) ha sido bloqueada por 5 intentos fallidos.",
                        Tipo = "Danger",
                        FechaCreacion = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Bloqueo de Cuenta", "Seguridad", 
                        $"Cuenta bloqueada tras 5 intentos fallidos consecutivos desde IP {ipAddress}", ipAddress);

                    ModelState.AddModelError(string.Empty, 
                        "Has alcanzado el límite de 5 intentos fallidos. Tu cuenta ha sido bloqueada temporalmente por seguridad.");
                    return View(model);
                }

                await _context.SaveChangesAsync();

                var intentosRestantes = Math.Max(0, 5 - usuario.IntentosFallidos);
                var mensajeError = usuario.Rol == "Administrador"
                    ? "Credenciales de acceso inválidas."
                    : $"Credenciales inválidas. Intentos restantes antes del bloqueo: {intentosRestantes}";

                ModelState.AddModelError(string.Empty, mensajeError);

                await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Contraseña Incorrecta", "Autenticación", 
                    $"Intento fallido #{usuario.IntentosFallidos}", ipAddress);

                return View(model);
            }

            // Inicio de sesión exitoso
            usuario.IntentosFallidos = 0;
            usuario.UltimoAcceso = DateTime.Now;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) // 10 minutos de inactividad
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Inicio de Sesión", "Autenticación", 
                "Inicio de sesión correcto en el sistema", ipAddress);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Desconocido";
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Desconocido";

            await _auditService.LogAsync(userEmail, userRole, "Cierre de Sesión", "Autenticación", "Cierre de sesión de usuario");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SolicitarRecuperacion()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarRecuperacion(ForgotPasswordRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo.ToLower() == model.Correo.Trim().ToLower());

            if (usuario == null)
            {
                ViewBag.MensajeExito = "Si el correo está registrado en la plataforma, la solicitud ha sido enviada al Administrador.";
                return View();
            }

            // Registrar solicitud en el sistema (CU-42)
            var solicitud = new Solicitud
            {
                TipoSolicitud = "CambioContrasena",
                UsuarioSolicitanteId = usuario.Id,
                Estado = "Pendiente",
                FechaSolicitud = DateTime.Now,
                DatosJson = System.Text.Json.JsonSerializer.Serialize(new { Correo = usuario.Correo, Motivo = model.Motivo })
            };

            _context.Solicitudes.Add(solicitud);

            // Generar notificación interna para el Administrador (CU-43)
            _context.Notificaciones.Add(new Notificacion
            {
                RolDestino = "Administrador",
                Titulo = "Solicitud de Cambio/Recuperación de Contraseña",
                Mensaje = $"El usuario {usuario.NombreCompleto} ({usuario.Correo}) ha solicitado cambio/recuperación de contraseña.",
                Tipo = "Warning",
                FechaCreacion = DateTime.Now,
                Enlace = "/Solicitudes/Index"
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Solicitud Recuperación Contraseña", "Seguridad", 
                $"El usuario solicitó restablecimiento de clave: {model.Motivo}");

            ViewBag.MensajeExito = "Tu solicitud ha sido enviada al Administrador. Una vez sea aprobada se te asignará tu nueva credencial.";
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult CambiarContrasena()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login");
            }

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, usuario.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "La contraseña actual no es correcta.");
                return View(model);
            }

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(usuario.Correo, usuario.Rol, "Cambio de Contraseña", "Seguridad", 
                "El usuario actualizó su contraseña exitosamente");

            TempData["SuccessMessage"] = "Tu contraseña ha sido actualizada con éxito.";
            return RedirectToAction("Index", "Home");
        }
    }
}
