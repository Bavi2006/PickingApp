using Microsoft.EntityFrameworkCore;
using PickingApp.Data;
using PickingApp.Models;

namespace PickingApp.Services
{
    public class PickingEngine : IPickingEngine
    {
        private readonly ApplicationDbContext _context;

        public PickingEngine(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<double> CalcularTiempoEstimadoAsync(int pedidoId, int empleadoId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null) return 10.0;

            var totalItems = pedido.Detalles.Sum(d => d.Cantidad);
            var totalUbicaciones = pedido.Detalles.Select(d => d.UbicacionId).Distinct().Count();

            // Buscar histórico de pedidos completados del empleado
            var pedidosCompletados = await _context.Pedidos
                .Include(p => p.Detalles)
                .Where(p => p.EmpleadoId == empleadoId && p.Estado == "Completado" && p.TiempoProcesamientoMinutos.HasValue)
                .ToListAsync();

            if (pedidosCompletados.Any())
            {
                // Calcular promedio de minutos por producto o por pedido
                var totalMinutosHistorico = pedidosCompletados.Sum(p => p.TiempoProcesamientoMinutos!.Value);
                var totalProductosHistorico = pedidosCompletados.Sum(p => p.Detalles.Sum(d => d.Cantidad));

                if (totalProductosHistorico > 0)
                {
                    var promedioMinutosPorUnidad = totalMinutosHistorico / totalProductosHistorico;
                    var estimado = Math.Round(totalItems * promedioMinutosPorUnidad, 1);
                    return estimado < 2.0 ? 2.0 : estimado;
                }
            }

            // Estimación base por defecto si no tiene histórico: 1.5 minutos por unidad + 1 minuto por ubicación
            var tiempoBase = (totalItems * 1.5) + (totalUbicaciones * 1.0);
            return Math.Round(Math.Max(5.0, tiempoBase), 1);
        }

        public async Task<AsignacionValidationResult> ValidarAsignacionAsync(int pedidoId, int empleadoId)
        {
            var result = new AsignacionValidationResult();

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                result.PuedeAsignar = false;
                result.MensajeError = "El pedido no existe.";
                return result;
            }

            var empleado = await _context.Empleados
                .Include(e => e.PedidosAsignados)
                .FirstOrDefaultAsync(e => e.Id == empleadoId);

            if (empleado == null)
            {
                result.PuedeAsignar = false;
                result.MensajeError = "El empleado seleccionado no existe.";
                return result;
            }

            // Regla 1: Empleado Activo
            if (!empleado.Activo)
            {
                result.PuedeAsignar = false;
                result.MensajeError = $"El empleado '{empleado.NombreCompleto}' se encuentra inactivo.";
                return result;
            }

            // Regla 2: Un solo pedido simultáneo
            var tienePedidoActivo = await _context.Pedidos
                .AnyAsync(p => p.EmpleadoId == empleadoId && (p.Estado == "Asignado" || p.Estado == "EnProceso") && p.Id != pedidoId);

            if (tienePedidoActivo || empleado.EstadoDisponibilidad == "EnPicking")
            {
                result.PuedeAsignar = false;
                result.MensajeError = $"El empleado '{empleado.NombreCompleto}' ya tiene un pedido asignado en curso (RN-01-CU26).";
                return result;
            }

            // Regla 3: Validar Jornada Laboral
            var horaActual = DateTime.Now.TimeOfDay;
            bool enHorario = true;

            if (empleado.HorarioEntrada < empleado.HorarioSalida)
            {
                // Turno diurno normal
                enHorario = horaActual >= empleado.HorarioEntrada && horaActual <= empleado.HorarioSalida;
            }
            else
            {
                // Turno nocturno que cruza medianoche
                enHorario = horaActual >= empleado.HorarioEntrada || horaActual <= empleado.HorarioSalida;
            }

            if (!enHorario)
            {
                result.PuedeAsignar = false;
                result.MensajeError = $"El empleado '{empleado.NombreCompleto}' se encuentra fuera de su jornada laboral ({empleado.HorarioEntrada:hh\\:mm} - {empleado.HorarioSalida:hh\\:mm}).";
                return result;
            }

            // Regla 4: Tiempo estimado vs Tiempo restante de jornada
            var tiempoEstimado = await CalcularTiempoEstimadoAsync(pedidoId, empleadoId);
            result.TiempoEstimadoMinutos = tiempoEstimado;

            TimeSpan tiempoRestanteJornada;
            if (empleado.HorarioSalida > horaActual)
            {
                tiempoRestanteJornada = empleado.HorarioSalida - horaActual;
            }
            else
            {
                tiempoRestanteJornada = (TimeSpan.FromHours(24) - horaActual) + empleado.HorarioSalida;
            }

            if (tiempoRestanteJornada.TotalMinutes < tiempoEstimado)
            {
                result.PuedeAsignar = false;
                result.MensajeError = $"El tiempo estimado para el pedido ({tiempoEstimado} min) supera el tiempo restante de la jornada del empleado ({Math.Round(tiempoRestanteJornada.TotalMinutes, 0)} min).";
                return result;
            }

            // Regla 5: Advertencia de Experiencia (< 6 meses y > 10 ubicaciones)
            var totalUbicacionesPedido = pedido.Detalles.Select(d => d.UbicacionId).Distinct().Count();
            if (empleado.TieneMenosDeSeisMeses && totalUbicacionesPedido > 10)
            {
                result.GeneraAdvertencia = true;
                result.MensajeAdvertencia = $"ADVERTENCIA: El empleado '{empleado.NombreCompleto}' tiene menos de 6 meses de experiencia ({empleado.MesesExperiencia} meses) y el pedido contiene {totalUbicacionesPedido} ubicaciones (> 10). La asignación quedará registrada para auditoría del Administrador.";
            }

            result.PuedeAsignar = true;
            return result;
        }

        public double CalcularTiempoProcesamiento(DateTime fechaAsignacion, DateTime fechaFinalizacion, int margenMinutos = 2)
        {
            var inicioEfectivo = fechaAsignacion.AddMinutes(margenMinutos);
            if (fechaFinalizacion <= inicioEfectivo)
            {
                return 1.0; // Mínimo 1 minuto
            }

            var diferencia = (fechaFinalizacion - inicioEfectivo).TotalMinutes;
            return Math.Round(diferencia, 1);
        }
    }
}
