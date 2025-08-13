using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamondFAB.Quote.Models
{
    public class PartDetail
    {
        // XML extracted properties
        public string? Name { get; set; }            // formerly PartName
        public int Quantity { get; set; }           // formerly PartQty
        public double Area { get; set; }            // from AreaP or similar
        public double CutDistance { get; set; }
        public List<PartDetail> PartDetails { get; set; } = new();

        // Cost breakdown
        public double LaserCost { get; set; }
        public double MaterialCost { get; set; }

        // Convenience total
        public double TotalCost => LaserCost + MaterialCost;
    }
}