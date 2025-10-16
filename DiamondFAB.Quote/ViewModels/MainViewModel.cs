using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // ===== Bindables =====
        [ObservableProperty]
        private string appTitle;

        [ObservableProperty]
        private QuoteModel currentQuote = new();

        [ObservableProperty]
        private Settings appSettings = new();

        public ObservableCollection<LineItem> LineItems { get; } = new();
        public ObservableCollection<PartDetail> PartDetails { get; } = new();

        // ===== ctor =====
        public MainViewModel()
        {
            // Version for window title
            var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            string version = !string.IsNullOrWhiteSpace(fileVersion)
                ? fileVersion.Split('+', '-')[0]
                : (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");
            AppTitle = $"DiamondFAB Quote v{version}";

            // Load settings and seed quote
            AppSettings = SettingsService.Load();

            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;
            CurrentQuote.QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber();

            // Ensure any enabled extra charges appear immediately
            SyncExtraCharges();
            CurrentQuote.NotifyTotalsChanged();
        }

        // Keep quote math and charge lines synced to settings changes
        partial void OnAppSettingsChanged(Settings value)
        {
            CurrentQuote.TaxRate = value.TaxRate;
            CurrentQuote.DiscountPercent = value.DiscountPercent;

            SyncExtraCharges();
            CurrentQuote.NotifyTotalsChanged();
        }

        // ===== Commands =====

        [RelayCommand]
        private void StartNewQuote()
        {
            LineItems.Clear();
            PartDetails.Clear();

            CurrentQuote = new QuoteModel
            {
                TaxRate = AppSettings.TaxRate,
                DiscountPercent = AppSettings.DiscountPercent,
                QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber()
            };

            // Add any enabled extra charges on a fresh quote
            SyncExtraCharges();
            CurrentQuote.NotifyTotalsChanged();
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
            var total = Math.Round(minutes);
            int h = (int)(total / 60);
            int m = (int)(total % 60);
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }

        public void ImportXmlFiles(string[] filePaths)
        {
            foreach (var file in filePaths)
            {
                var data = XmlFileParser.Parse(file);

                // Quantity of sheets/nests for MATERIAL math
                int qty = Math.Max(1, data.RawMaterialQuantity);

                // ---- LASER TIME (already full across all sheets) ----
                // Use ProcessTimeMinutes directly; DO NOT multiply by qty
                double totalMinutes = data.ProcessTimeMinutes > 0
                                    ? data.ProcessTimeMinutes
                                    : FallbackMinutes(data);  // safety fallback if XML missing

                double totalHours = totalMinutes / 60.0;
                string humanTime = FormatMinutes(totalMinutes);
                double laserCost = Math.Round(AppSettings.HourlyLaserRate * totalHours, 2);

                var laserItem = new LineItem
                {
                    Description = $"{data.MaterialCode} – Laser Time ({humanTime})",
                    Quantity = 1,
                    UnitPrice = laserCost
                };

                // ---- MATERIAL COST (qty * volume * density * $/lb) ----
                double volumePerSheet = data.RawLength * data.RawWidth * data.MaterialThickness; // in^3
                double weightPerSheet = volumePerSheet * data.Density;                            // lbs
                double totalWeight = weightPerSheet * qty;                                        // lbs
                double materialCost = Math.Round(totalWeight * data.MaterialCost, 2);             // $ total

                var materialItem = new LineItem
                {
                    Description = $"{data.MaterialCode} – Material Cost ({Math.Round(totalWeight, 2)} lbs)",
                    Quantity = qty,
                    UnitPrice = Math.Round(materialCost / qty, 2)
                };

                // Add to UI collection and the quote model
                LineItems.Add(laserItem);
                LineItems.Add(materialItem);
                CurrentQuote.LineItems.Add(laserItem);
                CurrentQuote.LineItems.Add(materialItem);

                // ---- PART-LEVEL DETAILS (page 2 of PDF) ----
                var partList = XmlFileParser.ParsePartDetails(file);
                if (CurrentQuote.PartDetails == null)
                    CurrentQuote.PartDetails = new List<PartDetail>();

                // For part laser estimate, still fall back to feed rate + per-part cut distance
                double feedForParts = data.FeedRate > 0 ? data.FeedRate : 1; // in/min guard
                foreach (var part in partList)
                {
                    // Laser estimate per part
                    double minutesPerPart = part.CutDistance / feedForParts;
                    double hoursPerPart = minutesPerPart / 60.0;
                    part.LaserCost = Math.Round(hoursPerPart * AppSettings.HourlyLaserRate * part.Quantity, 2);

                    // Material per part
                    double partVolume = part.Area * data.MaterialThickness; // in^3
                    double weightPerPart = partVolume * data.Density;       // lbs
                    part.MaterialCost = Math.Round(weightPerPart * data.MaterialCost * part.Quantity, 2);

                    CurrentQuote.PartDetails.Add(part);
                }
            }

            // Ensure extra charges remain represented (no duplicates)
            SyncExtraCharges();

            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;
            CurrentQuote.NotifyTotalsChanged();
        }

        private static double FallbackMinutes(PrtData data)
        {
            // Old-school estimate if ProcessTimeMinutes missing
            double feed = data.FeedRate > 0 ? data.FeedRate : 1;                 // in/min
            double timeCutMin = data.TotalCutDistance / feed;                    // minutes
            double timePierceMin = (data.PierceRateSec * data.TotalPierces) / 60.0; // minutes
            return timeCutMin + timePierceMin;
        }

        // ===== Extra Charges wiring =====

        private static bool IsLegacyChargeLine(LineItem item)
        {
            // Detect legacy suffix to clean them up
            return item.Description != null &&
                   item.Description.EndsWith(" (Charge)", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return s.Replace("(Charge)", "", StringComparison.OrdinalIgnoreCase).Trim();
        }

        /// <summary>
        /// Removes any existing charge lines and adds the currently enabled charges
        /// from AppSettings. Keeps UI and model lists in sync and avoids duplicates.
        /// </summary>
        private void SyncExtraCharges()
        {
            if (CurrentQuote is null) return;

            // Remove legacy "(Charge)" lines from both collections
            var legacyModel = CurrentQuote.LineItems.Where(IsLegacyChargeLine).ToList();
            foreach (var li in legacyModel) CurrentQuote.LineItems.Remove(li);

            var legacyUi = LineItems.Where(IsLegacyChargeLine).ToList();
            foreach (var li in legacyUi) LineItems.Remove(li);

            // If we have defined charges, also remove any lines whose description equals a charge Name
            if (AppSettings?.ExtraCharges != null && AppSettings.ExtraCharges.Any())
            {
                var chargeNames = AppSettings.ExtraCharges
                    .Select(c => Normalize(c.Name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var modelMatches = CurrentQuote.LineItems
                    .Where(li => chargeNames.Contains(Normalize(li.Description)))
                    .ToList();
                foreach (var li in modelMatches) CurrentQuote.LineItems.Remove(li);

                var uiMatches = LineItems
                    .Where(li => chargeNames.Contains(Normalize(li.Description)))
                    .ToList();
                foreach (var li in uiMatches) LineItems.Remove(li);
            }

            // Add enabled charges (no suffix), de-duped
            if (AppSettings?.ExtraCharges == null) return;

            foreach (var charge in AppSettings.ExtraCharges.Where(c => c.IsEnabled && c.Amount > 0))
            {
                var label = Normalize(charge.Name);

                bool exists = CurrentQuote.LineItems.Any(li =>
                    string.Equals(Normalize(li.Description), label, StringComparison.OrdinalIgnoreCase));

                if (exists) continue;

                var li = new LineItem
                {
                    Description = label,
                    Quantity = 1,
                    UnitPrice = Math.Round(charge.Amount, 2)
                };

                LineItems.Add(li);
                CurrentQuote.LineItems.Add(li);
            }
        }
    }
}