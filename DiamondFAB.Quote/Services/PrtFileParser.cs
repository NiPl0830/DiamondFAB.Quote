using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiamondFAB.Quote.Models;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class PrtFileParser
    {
        public static PrtData Parse(string filePath)
        {
            var data = new PrtData();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (line.Contains("Material Code")) data.MaterialCode = Extract(line);
                else if (line.Contains("Material Thickness")) data.MaterialThickness = double.Parse(Extract(line));
                else if (line.Contains("Material Feedrate")) data.FeedRate = double.Parse(Extract(line));
                else if (line.Contains("Material Piercerate")) data.PierceRateSec = double.Parse(Extract(line));
                else if (line.Contains("Raw Material Length")) data.RawLength = double.Parse(Extract(line));
                else if (line.Contains("Raw Material Width")) data.RawWidth = double.Parse(Extract(line));
                else if (line.Contains("Total No. Of Pierce")) data.TotalPierces = int.Parse(Extract(line));
                else if (line.Contains("Material Cost")) data.MaterialCost = double.Parse(Extract(line));
                else if (line.Contains("Total Cutting Dist.")) data.TotalCutDistance = double.Parse(Extract(line));
                else if (line.TrimStart().StartsWith("1 ") && line.Contains("AL_"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5 && int.TryParse(parts[^1], out int qty))
                    {
                        data.RawMaterialQuantity = qty;
                    }
                }
            }

            return data;
        }

        private static string Extract(string line)
        {
            var parts = line.Split(':');
            return parts.Length > 1 ? parts[1].Trim() : string.Empty;
        }
    }
}
