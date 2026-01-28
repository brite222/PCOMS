using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PCOMS.Models;

namespace PCOMS.Application.Pdf
{
    public class InvoicePdfDocument : IDocument
    {
        private readonly Invoice _invoice;

        public InvoicePdfDocument(Invoice invoice)
        {
            _invoice = invoice;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("PCOMS © ").FontSize(10);
                    text.Span(DateTime.Now.Year.ToString());
                });
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("INVOICE").FontSize(20).Bold();
                    col.Item().Text($"Invoice #: {_invoice.InvoiceNumber}");
                    col.Item().Text($"Date: {_invoice.CreatedAt:d}");
                });

                row.ConstantItem(150).AlignRight().Text(text =>
                {
                    text.Line("PCOMS").Bold();
                    text.Line("Project Control System");
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingTop(20).Column(col =>
            {
                col.Item().Text($"Client: {_invoice.Client.Name}").Bold();
                col.Item().Text($"Period: {_invoice.PeriodFrom:d} → {_invoice.PeriodTo:d}");

                col.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Description").Bold();
                        header.Cell().AlignRight().Text("Amount").Bold();
                    });

                    table.Cell().Text("Approved Work");
                    table.Cell().AlignRight().Text($"₦{_invoice.TotalAmount:N2}");
                });

                col.Item().PaddingTop(20)
                    .AlignRight()
                    .Text($"TOTAL: ₦{_invoice.TotalAmount:N2}")
                    .FontSize(14)
                    .Bold();
            });
        }
    }
}
