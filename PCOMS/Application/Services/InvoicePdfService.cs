using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PCOMS.Application.DTOs;

namespace PCOMS.Application.Services
{
    public class InvoicePdfService
    {
        public byte[] Generate(ClientBillingDto invoice)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header().Text($"INVOICE")
                        .FontSize(20)
                        .Bold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Client: {invoice.ClientName}");
                        col.Item().Text($"Period: {invoice.StartDate:d} - {invoice.EndDate:d}");
                        col.Item().PaddingVertical(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.ConstantColumn(80);
                                c.ConstantColumn(80);
                                c.ConstantColumn(100);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Project").Bold();
                                h.Cell().Text("Rate").Bold();
                                h.Cell().Text("Hours").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            foreach (var item in invoice.LineItems)
                            {
                                table.Cell().Text(item.ProjectName);
                                table.Cell().Text($"₦{item.HourlyRate}");
                                table.Cell().Text(item.TotalHours.ToString());
                                table.Cell().Text($"₦{item.TotalAmount}");
                            }
                        });

                        col.Item().PaddingTop(15)
                            .AlignRight()
                            .Text($"TOTAL: ₦{invoice.TotalAmount}")
                            .Bold();
                    });
                });
            }).GeneratePdf();
        }
    }
}
