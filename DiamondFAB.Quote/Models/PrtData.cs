using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamondFAB.Quote.Models
{
    public class PrtData
    {
        public string MaterialCode { get; set; }
        public double MaterialThickness { get; set; }
        public double FeedRate { get; set; } // (kept for future/fallback)
        public double PierceRateSec { get; set; }
        public double RawLength { get; set; }
        public double RawWidth { get; set; }
        public int TotalPierces { get; set; }
        public double TotalCutDistance { get; set; }
        public int RawMaterialQuantity { get; set; }
        public double MaterialCost { get; set; }
        public double Density { get; set; } // lbs/in³

        // NEW: minutes from <Nest><ProcessTime>
        public double ProcessTimeMinutes { get; set; }
    }
}
