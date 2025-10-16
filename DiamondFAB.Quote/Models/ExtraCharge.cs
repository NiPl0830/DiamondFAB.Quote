using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiamondFAB.Quote.Models
{
    public class ExtraCharge
    {
        public string Name { get; set; } = string.Empty;   // e.g., "Setup / Handling"
        public double Amount { get; set; }                 // flat $ per quote
        public bool IsEnabled { get; set; }                // whether to include on new quotes
        public string Key { get; set; } = string.Empty;    // stable id, e.g., "setup_handling"
    }
}
