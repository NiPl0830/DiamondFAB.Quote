using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DiamondFAB.Quote.Models
{
    public class Settings
    {
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string ContactEmail { get; set; }
        public double HourlyLaserRate { get; set; }
        public double TaxRate { get; set; }
        public Dictionary<string, double> MaterialRates { get; set; } = new(); // material code -> cost per square inch
        public string TermsAndConditions { get; set; }
        public string LogoPath { get; set; }
    }
}
