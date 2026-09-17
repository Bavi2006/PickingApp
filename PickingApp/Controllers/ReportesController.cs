using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Services;

namespace PickingApp.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly IAuditService _auditService;

        public ReportesController(
            ApplicationDbContext context, 
            IReportService reportService, 
            IAuditService auditService)
        {
            _context = context;
            _reportService = reportService;
            _auditService = auditService;
        }

        // CU-37: Consultar reportes generados
        public IActionResult Index()
        {
            return View();
        }

        // CU-32: Generar reporte de tiempos por empleado
        public async Task<IActionResult> TiemposPorEmpleado(DateTime? fechaInicio, DateTime? fechaFin, bool exportarPdf = false)
        {
            var inicio = fechaInicio ?? DateTime.Today.AddMonths(-1);
            var fin = (fechaFin ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var empleados = await _context.Empleados
                .Include(e => e.PedidosAsignados)
                .ToListAsync();

            var datos = new List<ReporteTiemposEmpleadoItem>();

            foreach (var emp in empleados)
            {
                var pedidosPeriodo = emp.PedidosAsignados
                    .Where(p => p.Estado == "Completado" && 
                                p.FechaFinalizacion >= inicio && 
                                p.FechaFinalizacion <= fin && 
                                p.TiempoProcesamientoMinutos.HasValue)
                    .ToList();

                if (pedidosPeriodo.Any())
                {
                    datos.Add(new ReporteTiemposEmpleadoItem
                    {
                        Identificacion = emp.Identificacion,
                        Empleado = emp.NombreCompleto,
                        TotalPedidos = pedidosPeriodo.Count,
                        PromedioMinutos = Math.Round(pedidosPeriodo.Average(p => p.TiempoProcesamientoMinutos!.Value), 1),
                        TiempoMinimo = Math.Round(pedidosPeriodo.Min(p => p.TiempoProcesamientoMinutos!.Value), 1),
                        TiempoMaximo = Math.Round(pedidosPeriodo.Max(p => p.TiempoProcesamientoMinutos!.Value), 1)
                    });
                }
            }

            if (exportarPdf)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
                var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
                await _auditService.LogAsync(userEmail, userRole, "Exportar PDF Reporte Tiempos Empleado", "Reportes", 
                    $"Exportación PDF periodo {inicio:dd/MM/yyyy} a {fin:dd/MM/yyyy}");

                var pdfBytes = _reportService.GenerarReporteTiemposPorEmpleadoPdf(datos, inicio, fin);
                return File(pdfBytes, "application/pdf", $"Reporte_Tiempos_Empleado_{DateTime.Now:yyyyMMdd}.pdf");
            }

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            return View(datos);
        }

        // CU-33: Generar reporte general de tiempos de entrega
        public async Task<IActionResult> TiemposGenerales(DateTime? fechaInicio, DateTime? fechaFin, bool exportarPdf = false)
        {
            var inicio = fechaInicio ?? DateTime.Today.AddMonths(-1);
            var fin = (fechaFin ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var bodegas = await _context.Bodegas
                .Include(b => b.Pedidos)
                .ToListAsync();

            var datos = new List<ReporteGeneralTiemposItem>();

            foreach (var b in bodegas)
            {
                var pedidos = b.Pedidos
                    .Where(p => p.Estado == "Completado" && 
                                p.FechaFinalizacion >= inicio && 
                                p.FechaFinalizacion <= fin && 
                                p.TiempoProcesamientoMinutos.HasValue)
                    .ToList();

                if (pedidos.Any())
                {
                    datos.Add(new ReporteGeneralTiemposItem
                    {
                        Bodega = $"{b.Nombre} ({b.Codigo})",
                        TotalCompletados = pedidos.Count,
                        PromedioMinutos = Math.Round(pedidos.Average(p => p.TiempoProcesamientoMinutos!.Value), 1),
                        TiempoTotalMinutos = Math.Round(pedidos.Sum(p => p.TiempoProcesamientoMinutos!.Value), 1)
                    });
                }
            }

            if (exportarPdf)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
                var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
                await _auditService.LogAsync(userEmail, userRole, "Exportar PDF Reporte Tiempos General", "Reportes", 
                    $"Exportación PDF periodo {inicio:dd/MM/yyyy} a {fin:dd/MM/yyyy}");

                var pdfBytes = _reportService.GenerarReporteGeneralTiemposPdf(datos, inicio, fin);
                return File(pdfBytes, "application/pdf", $"Reporte_General_Tiempos_{DateTime.Now:yyyyMMdd}.pdf");
            }

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            return View(datos);
        }

        // CU-34: Generar reporte de pedidos por periodo
        public async Task<IActionResult> PedidosPorPeriodo(DateTime? fechaInicio, DateTime? fechaFin, bool exportarPdf = false)
        {
            var inicio = fechaInicio ?? DateTime.Today.AddMonths(-1);
            var fin = (fechaFin ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var pedidos = await _context.Pedidos
                .Include(p => p.Bodega)
                .Include(p => p.Empleado)
                .Where(p => p.FechaCarga >= inicio && p.FechaCarga <= fin)
                .OrderByDescending(p => p.FechaCarga)
                .ToListAsync();

            if (exportarPdf)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
                var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
                await _auditService.LogAsync(userEmail, userRole, "Exportar PDF Pedidos por Periodo", "Reportes", 
                    $"Exportación PDF {pedidos.Count} pedidos entre {inicio:dd/MM/yyyy} y {fin:dd/MM/yyyy}");

                var pdfBytes = _reportService.GenerarReportePedidosPorPeriodoPdf(pedidos, inicio, fin);
                return File(pdfBytes, "application/pdf", $"Reporte_Pedidos_Periodo_{DateTime.Now:yyyyMMdd}.pdf");
            }

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            return View(pedidos);
        }

        // CU-35: Generar reporte de pedidos completados, cancelados y reasignados
        public async Task<IActionResult> CompletadosCanceladosReasignados(DateTime? fechaInicio, DateTime? fechaFin, bool exportarPdf = false)
        {
            var inicio = fechaInicio ?? DateTime.Today.AddMonths(-1);
            var fin = (fechaFin ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var pedidos = await _context.Pedidos
                .Include(p => p.Empleado)
                .Where(p => (p.Estado == "Completado" || p.Estado == "Cancelado" || p.Estado == "Reasignado") &&
                            p.FechaCarga >= inicio && p.FechaCarga <= fin)
                .OrderByDescending(p => p.FechaCarga)
                .ToListAsync();

            if (exportarPdf)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Usuario";
                var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Usuario";
                await _auditService.LogAsync(userEmail, userRole, "Exportar PDF Completados Cancelados Reasignados", "Reportes", 
                    $"Exportación PDF {pedidos.Count} registros");

                var pdfBytes = _reportService.GenerarReporteCompletadosCanceladosReasignadosPdf(pedidos, inicio, fin);
                return File(pdfBytes, "application/pdf", $"Reporte_Completados_Cancelados_Reasignados_{DateTime.Now:yyyyMMdd}.pdf");
            }

            ViewBag.FechaInicio = inicio;
            ViewBag.FechaFin = fin;
            return View(pedidos);
        }
    }
}