using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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

        static double ParseDouble(string str) =>
            double.TryParse(str, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0.0;

        static int ParseInt(string str) =>
            int.TryParse(str, out var i) ? i : 0;
    }
}