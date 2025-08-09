using System;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class QuoteNumberTracker
    {
        // New safe location under AppData\Roaming
        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiamondFAB", "Quote");

        private static readonly string NewFilePath = Path.Combine(AppDataDir, "quote_number.txt");

        // Old location (for migration)
        private static readonly string OldFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuoteNumber.txt");

        public static string GetNextFormattedQuoteNumber()
        {
            int next = GetNextQuoteNumber();
            return $"Q-{next:000000}";
        }

        public static int GetNextQuoteNumber()
        {
            EnsureLocationAndMigrate();

            int last = 0;
            if (File.Exists(NewFilePath))
            {
                var text = File.ReadAllText(NewFilePath).Trim();
                int.TryParse(text, out last);
            }

            int next = last + 1;
            File.WriteAllText(NewFilePath, next.ToString());
            return next;
        }

        private static void EnsureLocationAndMigrate()
        {
            Directory.CreateDirectory(AppDataDir);

            // If old file exists and new one doesn’t, migrate once
            if (File.Exists(OldFilePath) && !File.Exists(NewFilePath))
            {
                try
                {
                    File.Copy(OldFilePath, NewFilePath, overwrite: false);
                    // optional: delete the old one so it stops causing permission issues
                    // File.Delete(OldFilePath);
                }
                catch
                {
                    // swallow: if copy fails, we’ll just start at 0 in AppData
                }
            }
        }
    }
}
