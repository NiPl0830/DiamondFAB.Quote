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

                            // Bill To section (customer info)
                            col.Item().Element(c => BuildBillToSection(c, quote, greyPanel, greyBorder));

                            // Divider
                            col.Item().Height(1).Background(greyBorder);

                            // Summary metrics (laser hours, material weight)
                            col.Item().Element(c => BuildSummaryMetrics(c, quote, greyPanel, greyBorder, accent));

                            // Line items (+ extra charges from settings)
                            col.Item().Element(c => BuildLineItemsTable(c, quote, settings, greyHeader, greyBorder, greyPanel, white));

                            // Totals card
                            col.Item().AlignRight().Width(280).Element(c => BuildTotalsCard(c, quote, greyPanel, greyBorder));

                            // Terms
                            col.Item().PaddingTop(6).Element(c => BuildTerms(c, settings, greyPanel, greyBorder));
                        });
                    });

                    // Note: Page 2 for part-level breakdown has been removed.
                    // Part details are now integrated into the line items table on Page 1.
                });

                doc.GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"PDF export failed for {filePath}", ex);
                var msg = $"PDF export failed:\n{ex.Message}";
                MessageBox.Show(msg, "PDF Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        // Use YourCompanyName, fallback to CompanyName for backward compatibility
                        string companyName = !string.IsNullOrWhiteSpace(settings.YourCompanyName)
                            ? settings.YourCompanyName
                            : (settings.CompanyName ?? string.Empty);

                        c.Item().Text(companyName).Bold().FontSize(18);

                        // Use YourCompanyAddress, fallback to CompanyAddress
                        string address = !string.IsNullOrWhiteSpace(settings.YourCompanyAddress)
                            ? settings.YourCompanyAddress
                            : (settings.CompanyAddress ?? string.Empty);

                        if (!string.IsNullOrWhiteSpace(address))
                            c.Item().Text(address);

                        // Use YourContactEmail, fallback to ContactEmail
                        string email = !string.IsNullOrWhiteSpace(settings.YourContactEmail)
                            ? settings.YourContactEmail
                            : (settings.ContactEmail ?? string.Empty);

                        if (!string.IsNullOrWhiteSpace(email))
                            c.Item().Text(email);

                        // Use YourCompanyPhone if available
                        if (!string.IsNullOrWhiteSpace(settings.YourCompanyPhone))
                            c.Item().Text(settings.YourCompanyPhone);
                    });

                    // Use YourLogoPath, fallback to LogoPath
                    string logoPath = !string.IsNullOrWhiteSpace(settings.YourLogoPath)
                        ? settings.YourLogoPath
                        : (settings.LogoPath ?? string.Empty);

                    if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                        row.ConstantItem(110).Height(50).Element(e =>
                        {
                            // QuestPDF 2023.5+ style
                            e.Image(logoPath).FitArea();
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

                       // Add Valid Until date if set
                       if (quote.ExpirationDate.HasValue)
                       {
                           meta.Item().Text($"Valid Until: {quote.ExpirationDate.Value.ToString("d", CultureInfo.CurrentCulture)}")
                               .FontColor(Colors.Red.Medium);
                       }
                   });
            });
        }

        private static void BuildBillToSection(IContainer container, QuoteModel quote, string panelBg, string borderColor)
        {
            // Get customer info from quote, falling back to AppSettings if not set
            var customerName = quote.CustomerName;
            var customerAddress = quote.CustomerAddress;
            var customerEmail = quote.CustomerEmail;
            var customerPhone = quote.CustomerPhone;

            // If quote doesn't have customer info, try using AppSettings as fallback
            if (string.IsNullOrWhiteSpace(customerName) && quote.AppSettings != null)
            {
                customerName = quote.AppSettings.CompanyName;
                customerAddress = quote.AppSettings.CompanyAddress;
                customerEmail = quote.AppSettings.ContactEmail;
                customerPhone = quote.AppSettings.CompanyPhone;
            }

            // Only show if customer info exists
            bool hasCustomerInfo = !string.IsNullOrWhiteSpace(customerName) ||
                                   !string.IsNullOrWhiteSpace(customerAddress) ||
                                   !string.IsNullOrWhiteSpace(customerEmail) ||
                                   !string.IsNullOrWhiteSpace(customerPhone);

            if (!hasCustomerInfo)
                return;

            container.Background(panelBg)
                     .Border(1).BorderColor(borderColor)
                     .Padding(10)
                     .Column(col =>
                     {
                         col.Spacing(3);
                         col.Item().Text("Bill To:").Bold().FontSize(12);

                         if (!string.IsNullOrWhiteSpace(customerName))
                             col.Item().Text(customerName).SemiBold();

                         if (!string.IsNullOrWhiteSpace(customerAddress))
                             col.Item().Text(customerAddress);

                         if (!string.IsNullOrWhiteSpace(customerEmail))
                             col.Item().Text(customerEmail);

                         if (!string.IsNullOrWhiteSpace(customerPhone))
                             col.Item().Text(customerPhone);
                     });
        }

        private static void BuildSummaryMetrics(IContainer container, QuoteModel quote, string panelBg, string borderColor, string accentColor)
        {
            // Extract total laser hours and material weight from line items
            double totalLaserHours = 0;
            double totalMaterialLbs = 0;

            foreach (var item in quote.LineItems)
            {
                var desc = item.Description ?? "";

                // Extract laser time (e.g., "Steel-18g – Laser Time (2h 15m)")
                if (desc.Contains("Laser Time", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(desc, @"\((\d+)h\s*(\d+)m\)");
                    if (match.Success)
                    {
                        int hours = int.Parse(match.Groups[1].Value);
                        int minutes = int.Parse(match.Groups[2].Value);
                        totalLaserHours += hours + (minutes / 60.0);
                    }
                    else
                    {
                        // Try minutes only (e.g., "45m")
                        match = System.Text.RegularExpressions.Regex.Match(desc, @"\((\d+)m\)");
                        if (match.Success)
                        {
                            int minutes = int.Parse(match.Groups[1].Value);
                            totalLaserHours += minutes / 60.0;
                        }
                    }
                }

                // Extract material weight (e.g., "Steel-18g – Material Cost (45.2 lbs)")
                if (desc.Contains("Material Cost", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(desc, @"\(([\d.]+)\s*lbs\)");
                    if (match.Success)
                    {
                        totalMaterialLbs += double.Parse(match.Groups[1].Value);
                    }
                }
            }

            // Only show if we have meaningful data
            if (totalLaserHours <= 0 && totalMaterialLbs <= 0)
                return;

            container.Background(panelBg)
                     .Border(2).BorderColor(accentColor)
                     .Padding(10)
                     .Row(row =>
                     {
                         if (totalLaserHours > 0)
                         {
                             row.RelativeItem().Column(col =>
                             {
                                 col.Item().Text("Total Laser Time").FontSize(10).FontColor(Colors.Grey.Darken1);
                                 col.Item().Text($"{totalLaserHours:0.##} hours").Bold().FontSize(14);
                             });
                         }

                         if (totalMaterialLbs > 0)
                         {
                             row.RelativeItem().Column(col =>
                             {
                                 col.Item().Text("Total Material").FontSize(10).FontColor(Colors.Grey.Darken1);
                                 col.Item().Text($"{totalMaterialLbs:0.##} lbs").Bold().FontSize(14);
                             });
                         }
                     });
        }

        private static void BuildLineItemsTable(
    IContainer container,
    QuoteModel quote,
    Settings settings,
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

                // Header
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

                bool even = false;

                // Render ONLY the quote's line items (no settings injection)
                foreach (var item in quote.LineItems)
                {
                    even = !even;
                    string bg = even ? white : zebraAltBg;

                    var description = item.Description?.Replace("(Charge)", "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (string.IsNullOrEmpty(description))
                        description = "Line Item";

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6)
                         .Text(description);

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignCenter()
                         .Text(item.Quantity.ToString());

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignRight()
                         .Text(item.UnitPrice.ToString("C", CultureInfo.CurrentCulture));

                    table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6).AlignRight()
                         .Text(item.Total.ToString("C", CultureInfo.CurrentCulture));

                    // Show part details below Laser Time line items if setting is enabled
                    if (settings.ShowPartLevelBreakdown &&
                        item.Description != null &&
                        item.Description.Contains("Laser Time", StringComparison.OrdinalIgnoreCase) &&
                        quote.PartDetails != null &&
                        quote.PartDetails.Count > 0)
                    {
                        foreach (var part in quote.PartDetails)
                        {
                            string partBg = bg; // Keep same background as parent line item
                            string partDesc = $"  ↳ {part.Name} (qty: {part.Quantity}) • {part.CutDistance:0.#}\" cut length";

                            table.Cell().Background(partBg).PaddingVertical(3).PaddingLeft(20).PaddingRight(6)
                                 .Text(partDesc).FontSize(10).FontColor(Colors.Grey.Darken1);

                            table.Cell().Background(partBg).PaddingVertical(3).PaddingHorizontal(6).AlignCenter()
                                 .Text("").FontSize(10); // Empty qty cell

                            table.Cell().Background(partBg).PaddingVertical(3).PaddingHorizontal(6).AlignRight()
                                 .Text("").FontSize(10); // Empty unit price cell

                            table.Cell().Background(partBg).PaddingVertical(3).PaddingHorizontal(6).AlignRight()
                                 .Text(part.LaserCost.ToString("C", CultureInfo.CurrentCulture)).FontSize(10).FontColor(Colors.Grey.Darken1);
                        }
                    }
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