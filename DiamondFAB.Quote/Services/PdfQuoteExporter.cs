using DiamondFAB.Quote.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.Linq; // Any()
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
                // Colors as strings to avoid Color/string mixing
                string accent = Colors.Red.Medium;
                string greyBorder = Colors.Grey.Lighten2;
                string greyHeader = Colors.Grey.Lighten4;
                string greyPanel = Colors.Grey.Lighten5;
                string white = "#FFFFFF";

                var doc = Document.Create(container =>
                {
                    // ===================== PAGE 1 — QUOTE SUMMARY =====================
                    container.Page(page =>
                    {
                        page.Margin(40);
                        page.Size(PageSizes.A4);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        // Header & Footer
                        page.Header().Element(h => BuildHeader(h, settings, accent));
                        page.Footer().Element(BuildFooter);

                        // Content
                        page.Content().Column(col =>
                        {
                            col.Spacing(12);

                            // Title + meta
                            col.Item().Element(c => BuildQuoteMeta(c, quote, greyPanel, greyBorder));

                            // Divider
                            col.Item().Height(1).Background(greyBorder);

                            // Line items
                            col.Item().Element(c => BuildLineItemsTable(c, quote, greyHeader, greyBorder, greyPanel, white));

                            // Totals card
                            col.Item().AlignRight().Width(280).Element(c => BuildTotalsCard(c, quote, greyPanel, greyBorder));

                            // Terms
                            col.Item().PaddingTop(6).Element(c => BuildTerms(c, settings, greyPanel, greyBorder));
                        });
                    });

                    // ===================== PAGE 2 — PART-LEVEL BREAKDOWN =====================
                    if (quote.PartDetails?.Any() == true)
                    {
                        container.Page(page =>
                        {
                            page.Margin(40);
                            page.Size(PageSizes.A4);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11));

                            page.Header().Column(header =>
                            {
                                header.Item().Text("Part-Level Cost Breakdown").Bold().FontSize(16);
                                header.Item().Height(3).Background(accent);
                            });

                            page.Footer().Element(BuildFooter);

                            page.Content().Column(col =>
                            {
                                col.Item().Element(c => BuildPartsTable(c, quote, greyHeader, greyBorder, greyPanel, white));
                            });
                        });
                    }
                });

                doc.GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                var msg = $"❗ PDF export failed:\n{ex.GetType()}: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(msg, "PDF Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine(msg);
            }
        }

        // ============================= Builders =============================

        private static void BuildHeader(IContainer header, Settings settings, string accent)
        {
            header.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(settings.CompanyName).Bold().FontSize(18);

                        if (!string.IsNullOrWhiteSpace(settings.CompanyAddress))
                            c.Item().Text(settings.CompanyAddress);

                        if (!string.IsNullOrWhiteSpace(settings.ContactEmail))
                            c.Item().Text(settings.ContactEmail);
                    });

                    if (!string.IsNullOrWhiteSpace(settings.LogoPath) && File.Exists(settings.LogoPath))
                        row.ConstantItem(110).Height(50).Element(e =>
                        {
                            // QuestPDF 2023.5+ style
                            e.Image(settings.LogoPath).FitArea();
                        });
                });

                // Accent bar
                col.Item().Height(3).Background(accent);
            });
        }

        private static void BuildFooter(IContainer footer)
        {
            footer.AlignCenter().Text(t =>
            {
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            });
        }

        private static void BuildQuoteMeta(IContainer container, QuoteModel quote, string panelBg, string borderColor)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("Quote").Bold().FontSize(16);

                row.ConstantItem(260)
                   .PaddingTop(6) // visual offset from accent bar
                   .Background(panelBg)
                   .Border(1).BorderColor(borderColor)
                   .Padding(8)
                   .Column(meta =>
                   {
                       meta.Spacing(4);
                       meta.Item().Text($"Quote #: {quote.QuoteNumber}").SemiBold();
                       meta.Item().Text($"Date: {quote.Date.ToString("d", CultureInfo.CurrentCulture)}");
                   });
            });
        }

        private static void BuildLineItemsTable(
            IContainer container,
            QuoteModel quote,
            string headerBg,
            string borderColor,
            string zebraAltBg,
            string white)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(5);   // Description
                    cols.ConstantColumn(50);  // Qty
                    cols.ConstantColumn(90);  // Unit
                    cols.ConstantColumn(100); // Total
                });

                // Header (inline styling, no typed helpers)
                table.Header(h =>
                {
                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).Text("Description").SemiBold();

                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).AlignCenter().Text("Qty").SemiBold();

                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).AlignRight().Text("Unit").SemiBold();

                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).AlignRight().Text("Total").SemiBold();
                });

                // Rows
                bool even = false;
                foreach (var item in quote.LineItems)
                {
                    even = !even;
                    string bg = even ? white : zebraAltBg;

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6)
                         .Text(item.Description);

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignCenter()
                         .Text(item.Quantity.ToString());

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignRight()
                         .Text(item.UnitPrice.ToString("C", CultureInfo.CurrentCulture));

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignRight()
                         .Text(item.Total.ToString("C", CultureInfo.CurrentCulture));
                }
            });
        }

        private static void BuildTotalsCard(IContainer container, QuoteModel quote, string panelBg, string borderColor)
        {
            container.Background(panelBg)
                     .Border(1).BorderColor(borderColor)
                     .Padding(10)
                     .Column(card =>
                     {
                         card.Spacing(4);

                         decimal subtotal = Convert.ToDecimal(quote.Subtotal);
                         decimal discountAmount = Convert.ToDecimal(quote.DiscountAmount);
                         decimal tax = Convert.ToDecimal(quote.Tax);
                         decimal total = Convert.ToDecimal(quote.GrandTotal);

                         bool hasDiscount = discountAmount > 0m;

                         string discountLabel = hasDiscount && subtotal > 0m
                             ? $"Discount ({(discountAmount / subtotal * 100m):0.#}%):"
                             : "Discount:";

                         // Subtotal
                         card.Item().Row(r =>
                         {
                             r.RelativeItem().Text("Subtotal:");
                             r.ConstantItem(120).AlignRight().Text(subtotal.ToString("C", CultureInfo.CurrentCulture));
                         });

                         // Discount (optional)
                         if (hasDiscount)
                         {
                             card.Item().Row(r =>
                             {
                                 r.RelativeItem().Text(discountLabel);
                                 r.ConstantItem(120).AlignRight().Text($"-{discountAmount.ToString("C", CultureInfo.CurrentCulture)}");
                             });
                         }

                         // Tax
                         card.Item().Row(r =>
                         {
                             r.RelativeItem().Text("Tax:");
                             r.ConstantItem(120).AlignRight().Text(tax.ToString("C", CultureInfo.CurrentCulture));
                         });

                         // Total
                         card.Item().PaddingTop(6).Row(r =>
                         {
                             r.RelativeItem().Text("Total:").SemiBold();
                             r.ConstantItem(120).AlignRight().Text(total.ToString("C", CultureInfo.CurrentCulture)).SemiBold();
                         });
                     });
        }

        private static void BuildTerms(IContainer container, Settings settings, string panelBg, string borderColor)
        {
            container.Column(terms =>
            {
                terms.Item().Text("Terms and Conditions").Bold().FontSize(12);
                terms.Item().Background(panelBg)
                            .Border(1).BorderColor(borderColor)
                            .Padding(10)
                            .Text(settings.TermsAndConditions ?? "None Provided");
            });
        }

        private static void BuildPartsTable(
            IContainer container,
            QuoteModel quote,
            string headerBg,
            string borderColor,
            string zebraAltBg,
            string white)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(6);   // Part
                    cols.ConstantColumn(60);  // Qty
                    cols.ConstantColumn(100); // Total
                });

                // Header
                table.Header(h =>
                {
                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).Text("Part").SemiBold();

                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).AlignCenter().Text("Qty").SemiBold();

                    h.Cell().Background(headerBg).BorderBottom(1).BorderColor(borderColor)
                        .PaddingVertical(6).PaddingHorizontal(6).AlignRight().Text("Total").SemiBold();
                });

                bool even = false;
                foreach (var part in quote.PartDetails)
                {
                    even = !even;
                    string bg = even ? white : zebraAltBg;

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6)
                         .Text(part.Name);

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignCenter()
                         .Text(part.Quantity.ToString());

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignRight()
                         .Text(part.TotalCost.ToString("C", CultureInfo.CurrentCulture));
                }
            });
        }
    }
}