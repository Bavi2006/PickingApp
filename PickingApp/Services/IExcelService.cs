using ClosedXML.Excel;
using PickingApp.Models;

namespace PickingApp.Services
{
    public class ExcelValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<Pedido> PedidosValidados { get; set; } = new List<Pedido>();
        public int TotalFilas { get; set; }
        public int TotalPedidos { get; set; }
    }

    public interface IExcelService
    {
        Task<ExcelValidationResult> ValidarArchivoExcelAsync(Stream stream);
        byte[] GenerarPlantillaExcel();
    }
}
