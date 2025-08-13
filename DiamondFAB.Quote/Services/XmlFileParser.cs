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
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var doc = XDocument.Load(filePath);

            // Return the first matching element value (trimmed), or null
            string? GetValue(string tagName) =>
                doc.Descendants(tagName).FirstOrDefault()?.Value?.Trim();

            // Prefer values specifically under <Nest> when needed
            var nest = doc.Descendants("Nest").FirstOrDefault();

            var data = new PrtData
            {
                MaterialCode = GetValue("StockID") ?? string.Empty,
                MaterialThickness = ParseDouble(GetValue("Thickness")),
                FeedRate = ParseDouble(GetValue("FeedRate")),
                PierceRateSec = ParseDouble(GetValue("PierceRate")),
                RawLength = ParseDouble(GetValue("SheetX")),
                RawWidth = ParseDouble(GetValue("SheetY")),
                TotalPierces = ParseInt(GetValue("PierceCount")),
                TotalCutDistance = ParseDouble(GetValue("CutDistance")),
                RawMaterialQuantity = ParseInt(GetValue("NestQty")),
                MaterialCost = ParseDouble(GetValue("MaterialCost")),
                Density = ParseDouble(GetValue("Density")),

                // NEW: ProcessTime from <Nest> in minutes (falls back to any ProcessTime tag)
                ProcessTimeMinutes = nest != null
                                        ? ParseDouble(nest.Element("ProcessTime")?.Value)
                                        : ParseDouble(GetValue("ProcessTime"))
            };

            Console.WriteLine($"🧩 Parsed XML – Code: {data.MaterialCode}, Thickness: {data.MaterialThickness}, " +
                              $"Density: {data.Density}, Cost/lb: {data.MaterialCost}, Qty: {data.RawMaterialQuantity}, " +
                              $"ProcTime(min): {data.ProcessTimeMinutes}");

            return data;
        }

        public static List<PartDetail> ParsePartDetails(string filePath)
        {
            var partDetails = new List<PartDetail>();
            var doc = XDocument.Load(filePath);

            var partElements = doc.Descendants("Parts");
            foreach (var part in partElements)
            {
                string name = part.Element("Part")?.Value?.Trim() ?? "N/A";
                int qty = ParseInt(part.Element("PartQty")?.Value);
                double cutDistance = ParseDouble(part.Element("CutDistance")?.Value);
                // Use AreaT (true cut area) by default
                double area = ParseDouble(part.Element("AreaT")?.Value);

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

        // ---- Null-safe helpers ----
        static double ParseDouble(string? str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0.0;
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
        }

        static int ParseInt(string? str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;
            return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;
        }
    }
}