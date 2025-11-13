using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.Services
{
    public static class SavedQuotesService
    {
        private static readonly string BaseDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "DiamondFAB", "Quote", "SavedQuotes");

        public static string GetFolder() => BaseDir;

        public static string Save(QuoteModel quote, string? fileName = null)
        {
            Directory.CreateDirectory(BaseDir);

            // default file name: <CustomerName>_<QuoteNumber>.dfq.json
            if (string.IsNullOrWhiteSpace(fileName))
            {
                var safeCustomer = Sanitize(quote.CustomerName ?? "Unknown");
                var safeQuoteNo = Sanitize(quote.QuoteNumber ?? "NoNumber");
                fileName = $"{safeCustomer}_{safeQuoteNo}.dfq.json";
            }

            var path = Path.Combine(BaseDir, fileName);
            var json = JsonConvert.SerializeObject(quote, Formatting.Indented);
            File.WriteAllText(path, json);

            return path; // <-- add this
        }

        public static QuoteModel? Load(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
                return null;

            var json = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<QuoteModel>(json);
        }

        public static IEnumerable<(string FileName, string FullPath, string Customer, string QuoteNumber, DateTime Modified)>
            ListSaved()
        {
            Directory.CreateDirectory(BaseDir);

            foreach (var path in Directory.EnumerateFiles(BaseDir, "*.dfq.json"))
            {
                QuoteModel? q = null;
                try
                {
                    var text = File.ReadAllText(path);
                    q = JsonConvert.DeserializeObject<QuoteModel>(text);
                }
                catch
                {
                    // ignore corrupt files, but still list them
                }

                yield return (
                    FileName: Path.GetFileName(path),
                    FullPath: path,
                    Customer: q?.CustomerName ?? "Unknown",
                    QuoteNumber: q?.QuoteNumber ?? "",
                    Modified: File.GetLastWriteTime(path)
                );
            }
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }
        public static bool Delete(string? fullPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullPath)) return false;
                if (!File.Exists(fullPath)) return false;

                File.Delete(fullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
