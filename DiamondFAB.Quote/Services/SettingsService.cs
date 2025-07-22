using DiamondFAB.Quote.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class SettingsService
    {
        private static readonly string FilePath = "settings.json";

        public static Settings Load()
        {
            if (!File.Exists(FilePath))
                return new Settings(); // default blank

            var json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
        }

        public static void Save(Settings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }
    }
}
