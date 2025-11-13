using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Globalization;
using DiamondFAB.Quote.Models;

namespace DiamondFAB.Quote.Services
{
    public static class XmlFileParser
    {
        public static PrtData Parse(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                // Check file size (reject files > 50MB to prevent XML bomb attacks)
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 50 * 1024 * 1024)
                    throw new InvalidOperationException($"File too large: {Path.GetFileName(filePath)} ({fileInfo.Length / (1024 * 1024)}MB). Maximum size is 50MB.");

                var doc = XDocument.Load(filePath);

            string GetValue(string tagName) =>
                doc.Descendants(tagName).FirstOrDefault()?.Value.Trim() ?? string.Empty;

            // Target the <Nest> block for ProcessTime and NestQty specifically
            var nest = doc.Descendants("Nest").FirstOrDefault();

            string? processTimeStr = nest?.Element("ProcessTime")?.Value;
            string? nestQtyStr = nest?.Element("NestQty")?.Value;

            var data = new PrtData
            {
                MaterialCode = GetValue("StockID"),
                MaterialThickness = ParseDouble(GetValue("Thickness")),
                FeedRate = ParseDouble(GetValue("FeedRate")),
                PierceRateSec = ParseDouble(GetValue("PierceRate")),
                RawLength = ParseDouble(GetValue("SheetX")),
                RawWidth = ParseDouble(GetValue("SheetY")),
                TotalPierces = ParseInt(GetValue("PierceCount")),
                TotalCutDistance = ParseDouble(GetValue("CutDistance")),
                RawMaterialQuantity = ParseInt(nestQtyStr ?? GetValue("NestQty")),
                MaterialCost = ParseDouble(GetValue("MaterialCost")),
                Density = ParseDouble(GetValue("Density")),
                // NEW: decimal minutes (already totals across all sheets)
                ProcessTimeMinutes = ParseDouble(processTimeStr ?? GetValue("ProcessTime"))
            };

                Logger.Debug($"Successfully parsed XML file: {Path.GetFileName(filePath)}");
                return data;
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error($"XML file not found: {filePath}", ex);
                throw new InvalidOperationException($"File not found: {Path.GetFileName(filePath)}", ex);
            }
            catch (System.Xml.XmlException ex)
            {
                Logger.Error($"Invalid XML format in {Path.GetFileName(filePath)}", ex);
                throw new InvalidOperationException($"Invalid XML format in {Path.GetFileName(filePath)}: {ex.Message}", ex);
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our own exceptions (file too large, etc.)
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error parsing {Path.GetFileName(filePath)}", ex);
                throw new InvalidOperationException($"Error parsing {Path.GetFileName(filePath)}: {ex.Message}", ex);
            }
        }

        public static List<PartDetail> ParsePartDetails(string filePath)
        {
            try
            {
                var partDetails = new List<PartDetail>();
                var doc = XDocument.Load(filePath);

            var partElements = doc.Descendants("Parts");
            foreach (var part in partElements)
            {
                string name = part.Element("Part")?.Value ?? "N/A";
                int qty = int.TryParse(part.Element("PartQty")?.Value, out int q) ? q : 0;

                double cutDistance = 0.0;
                _ = double.TryParse(part.Element("CutDistance")?.Value,
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out cutDistance);

                double area = 0.0;
                _ = double.TryParse(part.Element("AreaT")?.Value,
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out area);

                partDetails.Add(new PartDetail
                {
                    Name = name,
                    Quantity = qty,
                    CutDistance = cutDistance,
                    Area = area
                });
            }

                return partDetails;
            }
            catch (System.Xml.XmlException ex)
            {
                Logger.Error($"Invalid XML format while parsing part details in {Path.GetFileName(filePath)}", ex);
                throw new InvalidOperationException($"Invalid XML format in {Path.GetFileName(filePath)}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error parsing part details from {Path.GetFileName(filePath)}", ex);
                throw new InvalidOperationException($"Error parsing part details from {Path.GetFileName(filePath)}: {ex.Message}", ex);
            }
        }

        static double ParseDouble(string? str) =>
            double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;

        static int ParseInt(string? str) =>
            int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
    }
}