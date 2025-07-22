using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using Microsoft.Win32;
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

        public MainViewModel()
        {
            AppSettings = SettingsService.Load();
            CurrentQuote.TaxRate = AppSettings.TaxRate;
        }

        partial void OnAppSettingsChanged(Settings value)
        {
            CurrentQuote.TaxRate = value.TaxRate;
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

            foreach (var file in dialog.FileNames)
            {
                var data = XmlFileParser.Parse(file);

                int qty = Math.Max(1, data.RawMaterialQuantity);
                double area = data.RawLength * data.RawWidth;
                double thickness = data.MaterialThickness;

                double sheetWeight = area * thickness * data.Density;
                double totalWeight = sheetWeight * qty;

                double cutTimeMin = data.TotalCutDistance / (data.FeedRate > 0 ? data.FeedRate : 1);
                double pierceTimeMin = data.PierceRateSec * data.TotalPierces / 60.0;
                double totalHours = (cutTimeMin + pierceTimeMin) / 60.0 * qty;

                double laserCost = Math.Round(AppSettings.HourlyLaserRate * totalHours, 2);
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
            }

            CurrentQuote.TaxRate = AppSettings.TaxRate;
            CurrentQuote.NotifyTotalsChanged();
        }
    }
}