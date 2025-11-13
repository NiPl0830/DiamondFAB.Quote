using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using QuoteModel = DiamondFAB.Quote.Models.Quote;


namespace DiamondFAB.Quote.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string appTitle;

        [ObservableProperty]
        private QuoteModel currentQuote = new();

        [ObservableProperty]
        private Settings appSettings = new();

        public ObservableCollection<LineItem> LineItems { get; } = new();
        public ObservableCollection<PartDetail> PartDetails { get; } = new();

        // Track imported files to prevent duplicates
        private HashSet<string> _importedFileHashes = new();

        public MainViewModel()
        {
            var fileVersion = FileVersionInfo
                .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                .FileVersion;

            string version = !string.IsNullOrWhiteSpace(fileVersion)
                ? fileVersion.Split('+', '-')[0]
                : (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

            AppTitle = $"DiamondFAB Quote v{version}";

            AppSettings = SettingsService.Load();

            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;
            CurrentQuote.QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber();
            CurrentQuote.AppSettings = AppSettings; // Store settings reference for PDF export

            // ensure any enabled extra charges are present at startup
            ApplyExtraChargesToQuote();
            CurrentQuote.NotifyTotalsChanged();
        }

        partial void OnAppSettingsChanged(Settings value)
        {
            CurrentQuote.TaxRate = value.TaxRate;
            CurrentQuote.DiscountPercent = value.DiscountPercent;
            CurrentQuote.AppSettings = value; // Update settings reference

            ApplyExtraChargesToQuote();
            CurrentQuote.NotifyTotalsChanged();
        }

        [RelayCommand]
        private void StartNewQuote()
        {
            LineItems.Clear();
            PartDetails.Clear();
            _importedFileHashes.Clear();

            CurrentQuote = new QuoteModel
            {
                TaxRate = AppSettings.TaxRate,
                DiscountPercent = AppSettings.DiscountPercent,
                QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber(),
                AppSettings = AppSettings // Store settings reference for PDF export
            };

            ApplyExtraChargesToQuote();
            CurrentQuote.NotifyTotalsChanged();
            Logger.Debug("Started new quote");
        }

        [RelayCommand]
        private void ImportXmlFiles()
        {
            // Use configurable import folder from settings, with fallback
            string? initialDir = AppSettings.DefaultImportFolder;

            if (string.IsNullOrEmpty(initialDir) || !Directory.Exists(initialDir))
            {
                initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var dialog = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                InitialDirectory = initialDir,
                Multiselect = true,
                Title = "Select XML File(s)"
            };

            if (dialog.ShowDialog() != true)
                return;

            ImportXmlFiles(dialog.FileNames);
        }

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
            int successCount = 0;
            int duplicateCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            foreach (var file in filePaths)
            {
                try
                {
                    // Check for duplicate imports
                    string fileHash = ComputeFileHash(file);
                    if (_importedFileHashes.Contains(fileHash))
                    {
                        var result = MessageBox.Show(
                            $"The file '{Path.GetFileName(file)}' appears to have been already imported. Import again?",
                            "Duplicate File Detected",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                        {
                            duplicateCount++;
                            Logger.Information($"Skipped duplicate import: {file}");
                            continue;
                        }
                    }

                    var data = XmlFileParser.Parse(file);

                int qty = Math.Max(1, data.RawMaterialQuantity);

                double totalMinutes = data.ProcessTimeMinutes > 0
                                    ? data.ProcessTimeMinutes
                                    : FallbackMinutes(data);

                double totalHours = totalMinutes / 60.0;
                string humanTime = FormatMinutes(totalMinutes);
                double laserCost = Math.Round(AppSettings.HourlyLaserRate * totalHours, 2);

                var laserItem = new LineItem
                {
                    Description = $"{data.MaterialCode} – Laser Time ({humanTime})",
                    Quantity = 1,
                    UnitPrice = laserCost
                };

                double volumePerSheet = data.RawLength * data.RawWidth * data.MaterialThickness; // in^3
                double weightPerSheet = volumePerSheet * data.Density;                            // lbs
                double totalWeight = weightPerSheet * qty;                                        // lbs
                double materialCost = Math.Round(totalWeight * data.MaterialCost, 2);             // $

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

                var partList = XmlFileParser.ParsePartDetails(file);
                if (CurrentQuote.PartDetails == null)
                    CurrentQuote.PartDetails = new List<PartDetail>();

                double feedForParts = data.FeedRate > 0 ? data.FeedRate : 1; // in/min
                foreach (var part in partList)
                {
                    double minutesPerPart = part.CutDistance / feedForParts;
                    double hoursPerPart = minutesPerPart / 60.0;
                    part.LaserCost = Math.Round(hoursPerPart * AppSettings.HourlyLaserRate * part.Quantity, 2);

                    double partVolume = part.Area * data.MaterialThickness; // in^3
                    double weightPerPart = partVolume * data.Density;       // lbs
                    part.MaterialCost = Math.Round(weightPerPart * data.MaterialCost * part.Quantity, 2);

                    CurrentQuote.PartDetails.Add(part);
                }

                    // Mark file as imported
                    _importedFileHashes.Add(fileHash);
                    successCount++;
                    Logger.Information($"Successfully imported: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                    Logger.Error($"Failed to import {file}", ex);
                }
            }

            ApplyExtraChargesToQuote();

            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;
            CurrentQuote.NotifyTotalsChanged();

            // Show import summary
            if (filePaths.Length > 1 || errorCount > 0 || duplicateCount > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Import complete:");
                sb.AppendLine($"  • Imported: {successCount}");
                if (duplicateCount > 0) sb.AppendLine($"  • Skipped (duplicates): {duplicateCount}");
                if (errorCount > 0)
                {
                    sb.AppendLine($"  • Failed: {errorCount}");
                    sb.AppendLine();
                    sb.AppendLine("Errors:");
                    foreach (var error in errors)
                    {
                        sb.AppendLine($"  • {error}");
                    }
                }

                MessageBox.Show(sb.ToString(), "Import Summary",
                    MessageBoxButton.OK,
                    errorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
        }

        private static string ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }

        private static double FallbackMinutes(PrtData data)
        {
            double feed = data.FeedRate > 0 ? data.FeedRate : 1;
            double timeCutMin = data.TotalCutDistance / feed;
            double timePierceMin = (data.PierceRateSec * data.TotalPierces) / 60.0;
            return timeCutMin + timePierceMin;
        }

        // ================= Extra Charges wiring =================

        private static bool IsExtraChargeLine(LineItem item) =>
            item.Description != null &&
            item.Description.EndsWith(" (Charge)", StringComparison.OrdinalIgnoreCase);

        private void ApplyExtraChargesToQuote()
        {
            if (CurrentQuote == null) return;

            // Remove existing charge lines
            foreach (var li in CurrentQuote.LineItems.Where(IsExtraChargeLine).ToList())
                CurrentQuote.LineItems.Remove(li);
            foreach (var li in LineItems.Where(IsExtraChargeLine).ToList())
                LineItems.Remove(li);

            // Add enabled charges
            if (AppSettings?.ExtraCharges != null)
            {
                foreach (var charge in AppSettings.ExtraCharges.Where(c => c.IsEnabled && c.Amount > 0))
                {
                    var li = new LineItem
                    {
                        Description = $"{charge.Name} (Charge)",
                        Quantity = 1,
                        UnitPrice = Math.Round(charge.Amount, 2)
                    };

                    LineItems.Add(li);
                    CurrentQuote.LineItems.Add(li);
                }
            }
        }

        // ================= Save / Load (in-app) =================

        [RelayCommand]
        private void SaveQuote()
        {
            try
            {
                // keep the model in sync with UI-bound collections
                CurrentQuote.LineItems = LineItems.ToList();
                CurrentQuote.PartDetails = PartDetails.ToList();

                // Capture current customer info from Settings when saving
                CurrentQuote.CustomerName = string.IsNullOrWhiteSpace(AppSettings.CompanyName)
                    ? "Unknown"
                    : AppSettings.CompanyName!;
                CurrentQuote.CustomerAddress = AppSettings.CompanyAddress;
                CurrentQuote.CustomerEmail = AppSettings.ContactEmail;
                CurrentQuote.CustomerPhone = AppSettings.CompanyPhone;

                // save (void return)
                SavedQuotesService.Save(CurrentQuote);

                MessageBox.Show("Quote saved.", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save quote:\n{ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void LoadSavedQuotes()
        {
            var picker = new SavedQuotesWindow();
            // Set owner if you create from MainWindow code-behind, not needed here

            if (picker.ShowDialog() == true && !string.IsNullOrWhiteSpace(picker.SelectedPath))
            {
                try
                {
                    var loaded = SavedQuotesService.Load(picker.SelectedPath);
                    ApplyLoadedQuote(loaded);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load quote:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ApplyLoadedQuote(QuoteModel? loaded)
        {
            if (loaded == null) return;

            CurrentQuote = loaded;

            // refresh observable collections for UI
            LineItems.Clear();
            foreach (var li in loaded.LineItems ?? Enumerable.Empty<LineItem>())
                LineItems.Add(li);

            PartDetails.Clear();
            foreach (var pd in loaded.PartDetails ?? Enumerable.Empty<PartDetail>())
                PartDetails.Add(pd);

            // Respect settings for preserving historical tax/discount rates
            // Only override if the loaded quote has no rates set (0), or if preservation is disabled
            if (!AppSettings.PreserveHistoricalTaxRates || CurrentQuote.TaxRate == 0)
            {
                CurrentQuote.TaxRate = AppSettings.TaxRate;
            }

            if (!AppSettings.PreserveHistoricalDiscounts || CurrentQuote.DiscountPercent == 0)
            {
                CurrentQuote.DiscountPercent = AppSettings.DiscountPercent;
            }

            ApplyExtraChargesToQuote();
            CurrentQuote.NotifyTotalsChanged();
            Logger.Information($"Loaded quote: {CurrentQuote.QuoteNumber}");
        }

        // ================= Export Commands (for keyboard shortcuts) =================

        [RelayCommand]
        private void ExportPdf()
        {
            if (CurrentQuote.LineItems.Count == 0)
            {
                MessageBox.Show("There is no quote to export. Please import a .PRT file first.",
                    "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get company name and quote number, sanitize for file name
            string companyName = AppSettings.CompanyName?.Trim() ?? "Company";
            string quoteNumber = CurrentQuote.QuoteNumber?.Trim() ?? "Quote";

            // Remove invalid filename characters
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                companyName = companyName.Replace(c, '_');
                quoteNumber = quoteNumber.Replace(c, '_');
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"{companyName}_{quoteNumber}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    PdfQuoteExporter.Export(CurrentQuote, AppSettings, saveDialog.FileName);
                    Logger.Information($"PDF exported: {saveDialog.FileName}");
                    MessageBox.Show("PDF Exported successfully!", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.Error("PDF export failed", ex);
                    MessageBox.Show($"Failed to export PDF:\n{ex.Message}", "Export Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ExportCsv()
        {
            if (CurrentQuote.LineItems.Count == 0)
            {
                MessageBox.Show("There is no quote to export. Please import a .PRT file first.",
                    "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get company name and quote number, sanitize for file name
            string companyName = AppSettings.CompanyName?.Trim() ?? "Company";
            string quoteNumber = CurrentQuote.QuoteNumber?.Trim() ?? "Quote";

            // Remove invalid filename characters
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                companyName = companyName.Replace(c, '_');
                quoteNumber = quoteNumber.Replace(c, '_');
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"{companyName}_{quoteNumber}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    CsvExporter.ExportQuote(CurrentQuote, AppSettings, saveDialog.FileName);
                    Logger.Information($"CSV exported: {saveDialog.FileName}");
                    MessageBox.Show("CSV Exported successfully!", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Logger.Error("CSV export failed", ex);
                    MessageBox.Show($"Failed to export CSV:\n{ex.Message}", "Export Failed",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}