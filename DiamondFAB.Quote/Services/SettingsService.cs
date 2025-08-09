using DiamondFAB.Quote.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class SettingsService
    {
        // %AppData%\DiamondFAB\Quote\settings.json
        private static readonly string BaseDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiamondFAB", "Quote");

        private static readonly string FilePath =
            Path.Combine(BaseDir, "settings.json");

        // Old location (next to EXE) – used for one-time migration
        private static readonly string LegacyFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static Settings Load()
        {
            try
            {
                Directory.CreateDirectory(BaseDir);

                // One-time migration if an old file exists alongside the EXE
                if (!File.Exists(FilePath) && File.Exists(LegacyFilePath))
                {
                    try
                    {
                        File.Copy(LegacyFilePath, FilePath, overwrite: false);
                    }
                    catch { /* ignore if copy fails */ }
                }

                if (!File.Exists(FilePath))
                    return new Settings(); // fresh defaults

                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
            }
            catch
            {
                // If anything goes sideways, fall back to defaults so the app still runs
                return new Settings();
            }
        }

        public static void Save(Settings settings)
        {
            Directory.CreateDirectory(BaseDir);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }
    }
}