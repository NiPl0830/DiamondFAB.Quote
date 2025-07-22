using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiamondFAB.Quote.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.IO;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.Services
{
    public static class PdfQuoteExporter
    {
        public static void Export(QuoteModel quote, Settings settings, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(settings.CompanyName).Bold().FontSize(18);
                            col.Item().Text(settings.CompanyAddress);
                            col.Item().Text(settings.ContactEmail);
                        });

                        if (!string.IsNullOrWhiteSpace(settings.LogoPath) && File.Exists(settings.LogoPath))
                        {
                            row.ConstantItem(100).Height(60).Image(settings.LogoPath);
                        }
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Quote #: {quote.QuoteNumber ?? "N/A"}").Bold();
                        col.Item().Text($"Date: {quote.Date:d}");
                        col.Item().Text($"Customer: {quote.CustomerName}");

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Description").Bold();
                                header.Cell().Text("Qty").Bold();
                                header.Cell().Text("Unit").Bold();
                                header.Cell().Text("Total").Bold();
                            });

                            foreach (var item in quote.LineItems)
                            {
                                table.Cell().Text(item.Description);
                                table.Cell().Text(item.Quantity.ToString());
                                table.Cell().Text($"${item.UnitPrice:F2}");
                                table.Cell().Text($"${item.Total:F2}");
                            }
                        });

                        col.Item().AlignRight().Column(right =>
                        {
                            right.Item().Text($"Subtotal: ${quote.Subtotal:F2}");
                            right.Item().Text($"Tax: ${quote.Tax:F2}");
                            right.Item().Text($"Total: ${quote.GrandTotal:F2}").Bold();
                        });

                        col.Item().PaddingTop(20).Text("Terms and Conditions").Bold();
                        col.Item().Text(settings.TermsAndConditions ?? "None Provided");
                    });

                });
            }).GeneratePdf(filePath);
        }
    }
}
