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

            return data;
        }

        public static List<PartDetail> ParsePartDetails(string filePath)
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

        static double ParseDouble(string? str) =>
            double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;

        static int ParseInt(string? str) =>
            int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
    }
}