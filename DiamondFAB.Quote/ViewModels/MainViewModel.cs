using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using QuoteModel = DiamondFAB.Quote.Models.Quote;

namespace DiamondFAB.Quote.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "DiamondFAB Quote";

        [ObservableProperty]
        private QuoteModel currentQuote = new();

        [ObservableProperty]
        private Settings appSettings = new();

        public ObservableCollection<LineItem> LineItems { get; } = new();
        public ObservableCollection<PartDetail> PartDetails { get; } = new();

        public MainViewModel()
        {
            AppSettings = SettingsService.Load();
            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.QuoteNumber = QuoteNumberTracker.GetNextFormattedQuoteNumber();
        }

        partial void OnAppSettingsChanged(Settings value)
        {
            CurrentQuote.TaxRate = value.TaxRate;
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

        public void ImportXmlFiles(string[] filePaths)
        {
            foreach (var file in filePaths)
            {
                var data = XmlFileParser.Parse(file);

                int qty = Math.Max(1, data.RawMaterialQuantity);
                double feed = data.FeedRate > 0 ? data.FeedRate : 1;
                double timeCutMin = data.TotalCutDistance / feed;
                double timePierceMin = (data.PierceRateSec * data.TotalPierces) / 60.0;
                double totalHours = (timeCutMin + timePierceMin) / 60.0 * qty;

                double laserCost = Math.Round(AppSettings.HourlyLaserRate * totalHours, 2);

                double volumePerSheet = data.RawLength * data.RawWidth * data.MaterialThickness;
                double weightPerSheet = volumePerSheet * data.Density;
                double totalWeight = weightPerSheet * qty;
                double materialCost = Math.Round(totalWeight * data.MaterialCost, 2);

                var laserItem = new LineItem
                {
                    Description = $"{data.MaterialCode} – Laser Time ({Math.Round(totalHours, 2)} hrs)",
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

                // 👇 Add part-level detail parsing
                var partList = XmlFileParser.ParsePartDetails(file);

                if (CurrentQuote.PartDetails == null)
                    CurrentQuote.PartDetails = new List<PartDetail>();

                foreach (var part in partList)
                {
                    CurrentQuote.PartDetails.Add(part);
                }

                MessageBox.Show($"DEBUG: PartDetails count = {CurrentQuote.PartDetails?.Count}", "Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.NotifyTotalsChanged();
        }
    }
}