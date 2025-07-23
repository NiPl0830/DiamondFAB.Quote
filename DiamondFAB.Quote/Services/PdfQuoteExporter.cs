using DiamondFAB.Quote.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.Services
{
    public static class PdfQuoteExporter
    {
        public static void Export(QuoteModel quote, Settings settings, string filePath)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    // Page 1: Main Quote Summary
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
                                row.ConstantItem(100).Height(60).Image(settings.LogoPath);
                        });

                        page.Content().Column(col =>
                        {
                            col.Item().Text($"Quote #: {quote.QuoteNumber}").Bold().FontSize(14);
                            col.Item().Text($"Date: {quote.Date:d}").FontSize(12);

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

                                // Header styling
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Description").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Qty").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Unit").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                                });

                                bool even = false;
                                foreach (var item in quote.LineItems)
                                {
                                    even = !even;
                                    var bg = even ? Colors.White : Colors.Grey.Lighten5;

                                    table.Cell().Background(bg).Padding(4).Text(item.Description);
                                    table.Cell().Background(bg).Padding(4).AlignCenter().Text(item.Quantity.ToString());
                                    table.Cell().Background(bg).Padding(4).AlignRight().Text(item.UnitPrice.ToString("C", CultureInfo.CurrentCulture));
                                    table.Cell().Background(bg).Padding(4).AlignRight().Text(item.Total.ToString("C", CultureInfo.CurrentCulture));
                                }
                            });

                            col.Item().AlignRight().PaddingTop(10).Column(right =>
                            {
                                right.Item().Text($"Subtotal: {quote.Subtotal.ToString("C", CultureInfo.CurrentCulture)}");
                                right.Item().Text($"Tax: {quote.Tax.ToString("C", CultureInfo.CurrentCulture)}");
                                right.Item().Text($"Total: {quote.GrandTotal.ToString("C", CultureInfo.CurrentCulture)}").Bold();
                            });

                            col.Item().PaddingTop(20).Text("Terms and Conditions").Bold();
                            col.Item().Text(settings.TermsAndConditions ?? "None Provided");
                        });
                    });

                    // Page 2: Part-Level Breakdown
                    if (quote.PartDetails?.Count > 0)
                    {
                        container.Page(page =>
                        {
                            page.Margin(40);
                            page.Size(PageSizes.A4);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(12));

                            page.Content().Column(col =>
                            {
                                col.Item().PaddingBottom(10).Text("🔍 Part-Level Cost Breakdown").Bold().FontSize(16);

                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);  // Name
                                        columns.ConstantColumn(50); // Qty
                                        columns.ConstantColumn(60); // Cut"
                                        columns.ConstantColumn(70); // Laser
                                        columns.ConstantColumn(70); // Mat
                                        columns.ConstantColumn(80); // Total
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Part").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Qty").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Cut\"").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Laser").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Mat").Bold();
                                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                                    });

                                    bool even = false;
                                    foreach (var part in quote.PartDetails)
                                    {
                                        even = !even;
                                        var bg = even ? Colors.White : Colors.Grey.Lighten5;

                                        table.Cell().Background(bg).Padding(4).Text(part.Name);
                                        table.Cell().Background(bg).Padding(4).AlignCenter().Text(part.Quantity.ToString());
                                        table.Cell().Background(bg).Padding(4).AlignRight().Text(part.CutDistance.ToString("F2"));
                                        table.Cell().Background(bg).Padding(4).AlignRight().Text(part.LaserCost.ToString("C", CultureInfo.CurrentCulture));
                                        table.Cell().Background(bg).Padding(4).AlignRight().Text(part.MaterialCost.ToString("C", CultureInfo.CurrentCulture));
                                        table.Cell().Background(bg).Padding(4).AlignRight().Text(part.TotalCost.ToString("C", CultureInfo.CurrentCulture));
                                    }
                                });
                            });
                        });
                    }
                });

                document.GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                var msg = $"❗ PDF export failed:\n{ex.GetType()}: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(msg, "PDF Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine(msg);
            }
        }
    }
}