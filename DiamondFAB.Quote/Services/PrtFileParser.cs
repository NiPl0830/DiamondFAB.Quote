using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DiamondFAB.Quote.Models;
using System.Xml.Linq;
using DiamondFAB.Quote.Models;
using System.Collections.Generic;
using System.Globalization;

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
                RawMaterialQuantity = ParseInt(GetValue("NestQty")),
                MaterialCost = ParseDouble(GetValue("MaterialCost")),
                Density = ParseDouble(GetValue("Density")) // NEW
            };

            Console.WriteLine($"🧩 Parsed XML – Code: {data.MaterialCode}, Thickness: {data.MaterialThickness}, " +
                              $"Density: {data.Density}, Cost/lb: {data.MaterialCost}, Qty: {data.RawMaterialQuantity}");
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
                double cutDistance = double.TryParse(part.Element("CutDistance")?.Value, out double c) ? c : 0.0;
                double area = double.TryParse(part.Element("AreaT")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double a) ? a : 0.0;

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

        static double ParseDouble(string str) =>
            double.TryParse(str, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0.0;

        static int ParseInt(string str) =>
            int.TryParse(str, out var i) ? i : 0;
    }
}