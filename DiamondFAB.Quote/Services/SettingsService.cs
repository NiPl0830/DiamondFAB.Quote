using DiamondFAB.Quote.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                    catch
                    {
                        // ignore if copy fails
                    }
                }

                Settings settings;
                if (!File.Exists(FilePath))
                {
                    settings = new Settings();
                }
                else
                {
                    var json = File.ReadAllText(FilePath);
                    settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }

                // --- NEW: Seed default ExtraCharges if missing ---
                if (settings.ExtraCharges == null || settings.ExtraCharges.Count == 0)
                {
                    settings.ExtraCharges = new List<ExtraCharge>
                    {
                        new ExtraCharge { Key = "setup_handling", Name = "Setup / Handling", Amount = 25.00, IsEnabled = false },
                        new ExtraCharge { Key = "deburr",          Name = "Deburr",          Amount = 15.00, IsEnabled = false },
                        new ExtraCharge { Key = "welding",         Name = "Welding",         Amount = 35.00, IsEnabled = false },
                        new ExtraCharge { Key = "paint",           Name = "Paint",           Amount = 45.00, IsEnabled = false },
                    };

                    // Save once to persist these defaults
                    Save(settings);
                }

                return settings;
            }
            catch
            {
                // If anything goes sideways, fall back to defaults so the app still runs
                var safeDefaults = new Settings
                {
                    ExtraCharges = new List<ExtraCharge>
                    {
                        new ExtraCharge { Key = "setup_handling", Name = "Setup / Handling", Amount = 25.00, IsEnabled = false },
                        new ExtraCharge { Key = "deburr",          Name = "Deburr",          Amount = 15.00, IsEnabled = false },
                        new ExtraCharge { Key = "welding",         Name = "Welding",         Amount = 35.00, IsEnabled = false },
                        new ExtraCharge { Key = "paint",           Name = "Paint",           Amount = 45.00, IsEnabled = false },
                    }
                };
                return safeDefaults;
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