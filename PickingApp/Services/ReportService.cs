using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PickingApp.Models;

namespace PickingApp.Services
{
    public class ReportService : IReportService
    {
        public ReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerarReporteTiemposPorEmpleadoPdf(List<ReporteTiemposEmpleadoItem> datos, DateTime fechaInicio, DateTime fechaFin)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, "Reporte de Tiempos de Picking por Empleado (CU-32)", fechaInicio, fechaFin));

                    page.Content().PaddingVertical(10).Element(c =>
                    {
                        c.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("Doc ID");
                                header.Cell().Element(CellStyleHeader).Text("Empleado");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Pedidos");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Promedio (m)");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Mínimo (m)");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Máximo (m)");
                            });

                            foreach (var item in datos)
                            {
                                table.Cell().Element(CellStyleRow).Text(item.Identificacion);
                                table.Cell().Element(CellStyleRow).Text(item.Empleado);
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.TotalPedidos.ToString());
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.PromedioMinutos.ToString("0.0"));
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.TiempoMinimo.ToString("0.0"));
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.TiempoMaximo.ToString("0.0"));
                            }
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerarReporteGeneralTiemposPdf(List<ReporteGeneralTiemposItem> datos, DateTime fechaInicio, DateTime fechaFin)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, "Reporte General de Tiempos de Entrega (CU-33)", fechaInicio, fechaFin));

                    page.Content().PaddingVertical(10).Element(c =>
                    {
                        c.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("Bodega");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Pedidos Completados");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Promedio (min)");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Total Minutos");
                            });

                            foreach (var item in datos)
                            {
                                table.Cell().Element(CellStyleRow).Text(item.Bodega);
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.TotalCompletados.ToString());
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.PromedioMinutos.ToString("0.0"));
                                table.Cell().Element(CellStyleRow).AlignRight().Text(item.TiempoTotalMinutos.ToString("0.0"));
                            }
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerarReportePedidosPorPeriodoPdf(List<Pedido> pedidos, DateTime fechaInicio, DateTime fechaFin)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, "Reporte Consolidado de Pedidos por Periodo (CU-34)", fechaInicio, fechaFin));

                    page.Content().PaddingVertical(10).Element(c =>
                    {
                        c.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("Código");
                                header.Cell().Element(CellStyleHeader).Text("Bodega");
                                header.Cell().Element(CellStyleHeader).Text("Prioridad");
                                header.Cell().Element(CellStyleHeader).Text("Estado");
                                header.Cell().Element(CellStyleHeader).Text("Empleado");
                                header.Cell().Element(CellStyleHeader).Text("Fecha Carga");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Tiempo (m)");
                            });

                            foreach (var p in pedidos)
                            {
                                table.Cell().Element(CellStyleRow).Text(p.CodigoPedido);
                                table.Cell().Element(CellStyleRow).Text(p.Bodega?.Nombre ?? "N/A");
                                table.Cell().Element(CellStyleRow).Text(p.Prioridad);
                                table.Cell().Element(CellStyleRow).Text(p.Estado);
                                table.Cell().Element(CellStyleRow).Text(p.Empleado?.NombreCompleto ?? "Sin asignar");
                                table.Cell().Element(CellStyleRow).Text(p.FechaCarga.ToString("dd/MM/yyyy HH:mm"));
                                table.Cell().Element(CellStyleRow).AlignRight().Text(p.TiempoProcesamientoMinutos?.ToString("0.0") ?? "-");
                            }
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerarReporteCompletadosCanceladosReasignadosPdf(List<Pedido> pedidos, DateTime fechaInicio, DateTime fechaFin)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, "Reporte de Pedidos Completados, Cancelados y Reasignados (CU-35)", fechaInicio, fechaFin));

                    page.Content().PaddingVertical(10).Element(c =>
                    {
                        c.Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(70);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("Código");
                                header.Cell().Element(CellStyleHeader).Text("Estado");
                                header.Cell().Element(CellStyleHeader).Text("Empleado");
                                header.Cell().Element(CellStyleHeader).Text("Fecha Fin");
                                header.Cell().Element(CellStyleHeader).AlignRight().Text("Tiempo");
                                header.Cell().Element(CellStyleHeader).Text("Motivo / Observación");
                            });

                            foreach (var p in pedidos)
                            {
                                var motivo = !string.IsNullOrEmpty(p.MotivoCancelacion) ? $"Cancelación: {p.MotivoCancelacion}" :
                                             (!string.IsNullOrEmpty(p.MotivoReasignacion) ? $"Reasignación: {p.MotivoReasignacion}" : 
                                             (p.ConfirmacionFisica ? "Entrega física confirmada" : "-"));

                                table.Cell().Element(CellStyleRow).Text(p.CodigoPedido);
                                table.Cell().Element(CellStyleRow).Text(p.Estado);
                                table.Cell().Element(CellStyleRow).Text(p.Empleado?.NombreCompleto ?? "-");
                                table.Cell().Element(CellStyleRow).Text(p.FechaFinalizacion?.ToString("dd/MM/yyyy HH:mm") ?? "-");
                                table.Cell().Element(CellStyleRow).AlignRight().Text(p.TiempoProcesamientoMinutos?.ToString("0.0") + " min");
                                table.Cell().Element(CellStyleRow).Text(motivo);
                            }
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, string titulo, DateTime fechaInicio, DateTime fechaFin)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("PickingApp - Sistema de Gestión Logística").FontSize(16).Bold().FontColor("#0D6EFD");
                        col.Item().Text(titulo).FontSize(13).SemiBold();
                        col.Item().Text($"Periodo: {fechaInicio:dd/MM/yyyy} hasta {fechaFin:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    row.ConstantItem(140).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        col.Item().AlignRight().Text("Documento Oficial").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("PickingApp &bull; Reporte exportado en formato PDF conforme a CU-36").FontSize(8).FontColor(Colors.Grey.Medium);
                row.ConstantItem(100).AlignRight().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }

        private static IContainer CellStyleHeader(IContainer container)
        {
            return container.DefaultTextStyle(x => x.Bold().FontColor(Colors.White))
                            .PaddingVertical(6)
                            .PaddingHorizontal(4)
                            .Background("#0D6EFD");
        }

        private static IContainer CellStyleRow(IContainer container)
        {
            return container.BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(5)
                            .PaddingHorizontal(4);
        }
    }
}
