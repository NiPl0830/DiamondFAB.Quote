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
        // Customer Information (General Tab - used for "Bill To" in PDF)
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? ContactEmail { get; set; }
        public string? CompanyPhone { get; set; }

        // Your Company Information (Preferences Tab - used for PDF Header)
        public string? YourCompanyName { get; set; }
        public string? YourCompanyAddress { get; set; }
        public string? YourContactEmail { get; set; }
        public string? YourCompanyPhone { get; set; }
        public string? YourLogoPath { get; set; }

        // Other Settings
        public double HourlyLaserRate { get; set; }
        public double TaxRate { get; set; }
        public string? TermsAndConditions { get; set; }
        public string? LogoPath { get; set; } // Legacy - can be removed later
        public int LastQuoteNumber { get; set; } = 1000; // starting point
        public double DiscountPercent { get; set; }
        public List<ExtraCharge> ExtraCharges { get; set; } = new();

        // New settings for improved functionality
        public string? DefaultImportFolder { get; set; }
        public bool PreserveHistoricalTaxRates { get; set; } = true;
        public bool PreserveHistoricalDiscounts { get; set; } = true;
        public bool ShowPartLevelBreakdown { get; set; } = true; // Show part details in PDF line items
    }
}
