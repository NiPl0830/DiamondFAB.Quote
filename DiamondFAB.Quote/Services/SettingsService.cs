using DiamondFAB.Quote.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class SettingsService
    {
        // Standardized to %AppData%\DiamondFAB.Quote\settings.json
        private static readonly string BaseDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiamondFAB.Quote");

        private static readonly string FilePath =
            Path.Combine(BaseDir, "settings.json");

        // Old location for migration
        private static readonly string OldBaseDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiamondFAB", "Quote");

        // Old location (next to EXE) – used for one-time migration
        private static readonly string LegacyFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static Settings Load()
        {
            try
            {
                Directory.CreateDirectory(BaseDir);

                // Migration from old DiamondFAB\Quote path to new DiamondFAB.Quote path
                var oldFilePath = Path.Combine(OldBaseDir, "settings.json");
                if (!File.Exists(FilePath) && File.Exists(oldFilePath))
                {
                    try
                    {
                        File.Copy(oldFilePath, FilePath, overwrite: false);
                        Logger.Information($"Migrated settings from {oldFilePath} to {FilePath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to migrate settings from old location: {ex.Message}");
                    }
                }

                // One-time migration if an old file exists alongside the EXE
                if (!File.Exists(FilePath) && File.Exists(LegacyFilePath))
                {
                    try
                    {
                        File.Copy(LegacyFilePath, FilePath, overwrite: false);
                        Logger.Information($"Migrated settings from legacy location: {LegacyFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to migrate legacy settings: {ex.Message}");
                    }
                }

                Settings settings;
                if (!File.Exists(FilePath))
                {
                    settings = new Settings();
                    Logger.Information("No settings file found, creating defaults");
                }
                else
                {
                    var json = File.ReadAllText(FilePath);
                    settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                    Logger.Debug("Settings loaded successfully");
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
            catch (Exception ex)
            {
                Logger.Error($"Failed to load settings from {FilePath}", ex);
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
            try
            {
                Directory.CreateDirectory(BaseDir);

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(FilePath, json);
                Logger.Debug("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save settings to {FilePath}", ex);
                throw; // Re-throw so caller knows save failed
            }
        }
    }
}