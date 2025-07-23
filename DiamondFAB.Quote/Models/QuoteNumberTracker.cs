using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class QuoteNumberTracker
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuoteNumber.txt");

        public static int GetNextQuoteNumber()
        {
            int lastNumber = 0;

            if (File.Exists(FilePath))
            {
                string content = File.ReadAllText(FilePath);
                if (int.TryParse(content, out int parsed))
                {
                    lastNumber = parsed;
                }
            }

            int nextNumber = lastNumber + 1;
            File.WriteAllText(FilePath, nextNumber.ToString());
            return nextNumber;
        }

        public static string GetNextFormattedQuoteNumber()
        {
            int next = GetNextQuoteNumber();
            return $"Q-{next:000000}";
        }
    }
}
