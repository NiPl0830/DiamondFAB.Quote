using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Bind your Window Title to this: Title="{Binding AppTitle}"
        [ObservableProperty]
        private string appTitle;

        [ObservableProperty]
        private QuoteModel currentQuote = new();

        [ObservableProperty]
        private Settings appSettings = new();

        public ObservableCollection<LineItem> LineItems { get; } = new();
        public ObservableCollection<PartDetail> PartDetails { get; } = new();

        public MainViewModel()
        {
            // Prefer informational version, trim +metadata and -pre-release
            var infoVersion = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            string version;
            if (!string.IsNullOrWhiteSpace(infoVersion))
            {
                version = infoVersion;
                int plus = version.IndexOf('+');
                if (plus >= 0) version = version.Substring(0, plus);
                int dash = version.IndexOf('-');
                if (dash >= 0) version = version.Substring(0, dash);
            }
            else
            {
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
            }

            AppTitle = $"DiamondFAB Quote v{version}";

            AppSettings = SettingsService.Load();
            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;   // ← apply discount from settings
            CurrentQuote.QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber();
        }

        partial void OnAppSettingsChanged(Settings value)
        {
            CurrentQuote.TaxRate = value.TaxRate;
            CurrentQuote.DiscountPercent = value.DiscountPercent;         // ← keep in sync with Settings
            CurrentQuote.NotifyTotalsChanged();
        }

        [RelayCommand]
        private void StartNewQuote()
        {
            LineItems.Clear();
            PartDetails.Clear();

            CurrentQuote = new QuoteModel
            {
                TaxRate = AppSettings.TaxRate,
                DiscountPercent = AppSettings.DiscountPercent,            // ← new quotes inherit discount
                QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber()
            };
        }

        [RelayCommand]
        private void ImportXmlFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml",
                InitialDirectory = @"C:\NestingSoftware\XML_Output",
                Multiselect = true,
                Title = "Select XML File(s)"
            };

            if (dialog.ShowDialog() != true)
                return;

            ImportXmlFiles(dialog.FileNames);
        }

        // Pretty "Xh Ym" formatting for minute values
        private static string FormatMinutes(double minutes)
        {
            if (minutes <= 0) return "0m";
            var total = Math.Round(minutes);  // whole minutes
            int h = (int)(total / 60);
            int m = (int)(total % 60);
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }

        public void ImportXmlFiles(string[] filePaths)
        {
            foreach (var file in filePaths)
            {
                var data = XmlFileParser.Parse(file);

                int qty = Math.Max(1, data.RawMaterialQuantity);

                // --- Laser time: prefer <ProcessTime> (minutes) from <Nest>, else fallback to legacy calc ---
                double perSheetMinutes = data.ProcessTimeMinutes;  // <-- new field from XML
                if (perSheetMinutes <= 0)
                {
                    double feed = data.FeedRate > 0 ? data.FeedRate : 1;                 // in/min
                    double timeCutMin = data.TotalCutDistance / feed;                    // minutes
                    double timePierceMin = (data.PierceRateSec * data.TotalPierces) / 60.0; // minutes
                    perSheetMinutes = timeCutMin + timePierceMin;
                }

                double totalMinutes = perSheetMinutes * qty;
                double totalHours = totalMinutes / 60.0;
                string humanTime = FormatMinutes(totalMinutes);

                double laserCost = Math.Round(AppSettings.HourlyLaserRate * totalHours, 2);

                // --- Material cost (volume * density * $/lb) ---
                double volumePerSheet = data.RawLength * data.RawWidth * data.MaterialThickness; // in^3
                double weightPerSheet = volumePerSheet * data.Density;                            // lbs
                double totalWeight = weightPerSheet * qty;                                        // lbs
                double materialCost = Math.Round(totalWeight * data.MaterialCost, 2);             // $/lb

                var laserItem = new LineItem
                {
                    Description = $"{data.MaterialCode} – Laser Time ({humanTime})",
                    Quantity = 1,
                    UnitPrice = laserCost
                };

                var materialItem = new LineItem
                {
                    Description = $"{data.MaterialCode} – Material Cost ({Math.Round(totalWeight, 2)} lbs)",
                    Quantity = qty,
                    UnitPrice = Math.Round(materialCost / qty, 2)
                };

                LineItems.Add(laserItem);
                LineItems.Add(materialItem);
                CurrentQuote.LineItems.Add(laserItem);
                CurrentQuote.LineItems.Add(materialItem);

                // --- Part-level details (for page 2 of PDF) ---
                var partList = XmlFileParser.ParsePartDetails(file);
                if (CurrentQuote.PartDetails == null)
                    CurrentQuote.PartDetails = new List<PartDetail>();

                // Keep part-level laser calc based on per-part cut distance (feed fallback from file)
                double feedForParts = data.FeedRate > 0 ? data.FeedRate : 1; // in/min
                foreach (var part in partList)
                {
                    // inches / (in/min) = minutes
                    double minutesPerPart = part.CutDistance / feedForParts;
                    double hoursPerPart = minutesPerPart / 60.0;

                    part.LaserCost = Math.Round(hoursPerPart * AppSettings.HourlyLaserRate * part.Quantity, 2);

                    double partVolume = part.Area * data.MaterialThickness; // in^3
                    double weightPerPart = partVolume * data.Density;       // lbs
                    part.MaterialCost = Math.Round(weightPerPart * data.MaterialCost * part.Quantity, 2);

                    CurrentQuote.PartDetails.Add(part);
                }
            }

            // ensure totals (including discount) refresh
            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;
            CurrentQuote.NotifyTotalsChanged();
        }
    }
}