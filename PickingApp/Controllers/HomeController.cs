using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;

namespace PickingApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;

            ViewBag.TotalPendientes = await _context.Pedidos.CountAsync(p => p.Estado == "Pendiente");
            ViewBag.TotalEnProceso = await _context.Pedidos.CountAsync(p => p.Estado == "Asignado" || p.Estado == "EnProceso");
            ViewBag.TotalCompletadosHoy = await _context.Pedidos.CountAsync(p => p.Estado == "Completado" && p.FechaFinalizacion >= hoy);
            ViewBag.EmpleadosDisponibles = await _context.Empleados.CountAsync(e => e.Activo && e.EstadoDisponibilidad == "Disponible");
            ViewBag.TotalBodegas = await _context.Bodegas.CountAsync(b => b.Activa);

            var ultimosPedidos = await _context.Pedidos
                .Include(p => p.Bodega)
                .Include(p => p.Empleado)
                .OrderByDescending(p => p.FechaCarga)
                .Take(5)
                .ToListAsync();

            return View(ultimosPedidos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
