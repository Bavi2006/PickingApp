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
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UsuariosController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // Listar usuarios del sistema
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.Rol)
                .ThenBy(u => u.NombreCompleto)
                .ToListAsync();

            return View(usuarios);
        }

        // Crear usuario
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Usuario model, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("PasswordHash", "La contraseña debe tener al menos 6 caracteres.");
            }

            model.Correo = model.Correo.Trim().ToLower();
            if (await _context.Usuarios.AnyAsync(u => u.Correo == model.Correo))
            {
                ModelState.AddModelError("Correo", "Ya existe un usuario con este correo electrónico.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            model.FechaRegistro = DateTime.Now;
            model.Estado = "Activo";
            model.IntentosFallidos = 0;

            _context.Usuarios.Add(model);
            await _context.SaveChangesAsync();

            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            await _auditService.LogAsync(adminEmail, "Administrador", "Crear Usuario", "Usuarios", 
                $"Usuario creado: {model.Correo} con rol {model.Rol}");

            TempData["SuccessMessage"] = $"Usuario '{model.NombreCompleto}' ({model.Correo}) creado con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // CU-04 y CU-05: Editar credenciales, roles y permisos
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string nombreCompleto, string rol, string estado, string? nuevaPassword)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";

            usuario.NombreCompleto = nombreCompleto.Trim();
            usuario.Rol = rol;
            usuario.Estado = estado;

            if (estado == "Activo")
            {
                usuario.IntentosFallidos = 0;
                usuario.FechaBloqueo = null;
            }

            if (!string.IsNullOrWhiteSpace(nuevaPassword) && nuevaPassword.Length >= 6)
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(adminEmail, "Administrador", "Editar Usuario/Rol", "Usuarios", 
                $"Usuario {usuario.Correo} actualizado: Rol={rol}, Estado={estado}");

            TempData["SuccessMessage"] = $"Usuario '{usuario.Correo}' actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // Desbloquear usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desbloquear(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Estado = "Activo";
            usuario.IntentosFallidos = 0;
            usuario.FechaBloqueo = null;

            await _context.SaveChangesAsync();

            var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
            await _auditService.LogAsync(adminEmail, "Administrador", "Desbloquear Usuario", "Seguridad", 
                $"Cuenta de usuario {usuario.Correo} desbloqueada manualmente.");

            TempData["SuccessMessage"] = $"La cuenta de '{usuario.Correo}' ha sido desbloqueada.";
            return RedirectToAction(nameof(Index));
        }
    }
}