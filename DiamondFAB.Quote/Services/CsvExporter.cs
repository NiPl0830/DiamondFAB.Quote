using DiamondFAB.Quote.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.Services
{
    public static class CsvExporter
    {
        public static void ExportQuote(QuoteModel quote, Settings settings, string filePath)
        {
            try
            {
                var sb = new StringBuilder();

                // Quote Header
                sb.AppendLine($"Quote Number,{EscapeCsv(quote.QuoteNumber)}");
                sb.AppendLine($"Date,{quote.Date:yyyy-MM-dd}");
                sb.AppendLine($"Customer,{EscapeCsv(quote.CustomerName)}");
                if (!string.IsNullOrEmpty(quote.CustomerAddress))
                {
                    sb.AppendLine($"Customer Address,{EscapeCsv(quote.CustomerAddress)}");
                }
                if (!string.IsNullOrEmpty(quote.PONumber))
                {
                    sb.AppendLine($"PO Number,{EscapeCsv(quote.PONumber)}");
                }
                sb.AppendLine($"Status,{EscapeCsv(quote.Status)}");
                sb.AppendLine();

                // Company Info
                sb.AppendLine($"Company,{EscapeCsv(settings.CompanyName)}");
                sb.AppendLine($"Address,{EscapeCsv(settings.CompanyAddress)}");
                sb.AppendLine($"Email,{EscapeCsv(settings.ContactEmail)}");
                if (!string.IsNullOrEmpty(settings.CompanyPhone))
                {
                    sb.AppendLine($"Phone,{EscapeCsv(settings.CompanyPhone)}");
                }
                sb.AppendLine();

                // Line Items
                sb.AppendLine("Line Items");
                sb.AppendLine("Description,Quantity,Unit Price,Total");
                foreach (var item in quote.LineItems ?? Enumerable.Empty<LineItem>())
                {
                    sb.AppendLine($"{EscapeCsv(item.Description)},{item.Quantity},{item.UnitPrice:F2},{item.Total:F2}");
                }
                sb.AppendLine();

                // Totals
                sb.AppendLine("Totals");
                sb.AppendLine($"Subtotal,{quote.Subtotal:F2}");
                if (quote.DiscountPercent > 0)
                {
                    sb.AppendLine($"Discount ({quote.DiscountPercent:F2}%),{quote.DiscountAmount:F2}");
                    sb.AppendLine($"Subtotal After Discount,{quote.SubtotalAfterDiscount:F2}");
                }
                sb.AppendLine($"Tax ({quote.TaxRate:F2}%),{quote.Tax:F2}");
                sb.AppendLine($"Grand Total,{quote.GrandTotal:F2}");
                sb.AppendLine();

                // Part Details (if available)
                if (quote.PartDetails?.Any() == true)
                {
                    sb.AppendLine("Part Details");
                    sb.AppendLine("Part Name,Quantity,Cut Distance,Area,Laser Cost,Material Cost,Total Cost");
                    foreach (var part in quote.PartDetails)
                    {
                        sb.AppendLine($"{EscapeCsv(part.Name)},{part.Quantity},{part.CutDistance:F2},{part.Area:F2},{part.LaserCost:F2},{part.MaterialCost:F2},{part.TotalCost:F2}");
                    }
                    sb.AppendLine();
                }

                // Notes (if available)
                if (!string.IsNullOrEmpty(quote.Notes))
                {
                    sb.AppendLine("Notes");
                    sb.AppendLine(EscapeCsv(quote.Notes));
                    sb.AppendLine();
                }

                // Terms and Conditions
                if (!string.IsNullOrEmpty(settings.TermsAndConditions))
                {
                    sb.AppendLine("Terms and Conditions");
                    sb.AppendLine(EscapeCsv(settings.TermsAndConditions));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Logger.Information($"CSV exported successfully to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to export CSV to {filePath}", ex);
                throw;
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // If value contains comma, quote, or newline, wrap in quotes and escape existing quotes
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }
    }
}
