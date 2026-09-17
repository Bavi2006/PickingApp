using PickingApp.Models;

namespace PickingApp.Services
{
    public class ReporteTiemposEmpleadoItem
    {
        public string Identificacion { get; set; } = string.Empty;
        public string Empleado { get; set; } = string.Empty;
        public int TotalPedidos { get; set; }
        public double PromedioMinutos { get; set; }
        public double TiempoMinimo { get; set; }
        public double TiempoMaximo { get; set; }
    }

    public class ReporteGeneralTiemposItem
    {
        public string Bodega { get; set; } = string.Empty;
        public int TotalCompletados { get; set; }
        public double PromedioMinutos { get; set; }
        public double TiempoTotalMinutos { get; set; }
    }

    public class ReporteEstadosPedidosItem
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double Porcentaje { get; set; }
    }

    public interface IReportService
    {
        byte[] GenerarReporteTiemposPorEmpleadoPdf(List<ReporteTiemposEmpleadoItem> datos, DateTime fechaInicio, DateTime fechaFin);
        byte[] GenerarReporteGeneralTiemposPdf(List<ReporteGeneralTiemposItem> datos, DateTime fechaInicio, DateTime fechaFin);
        byte[] GenerarReportePedidosPorPeriodoPdf(List<Pedido> pedidos, DateTime fechaInicio, DateTime fechaFin);
        byte[] GenerarReporteCompletadosCanceladosReasignadosPdf(List<Pedido> pedidos, DateTime fechaInicio, DateTime fechaFin);
    }
}