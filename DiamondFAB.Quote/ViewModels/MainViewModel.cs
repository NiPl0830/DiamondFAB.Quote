using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;
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
        partial void OnAppSettingsChanged(Settings value)
        {
            CurrentQuote.TaxRate = value.TaxRate;
            CurrentQuote.NotifyTotalsChanged();
        }

        public ObservableCollection<LineItem> LineItems { get; } = new();

        public MainViewModel()
        {
            AppSettings = SettingsService.Load();
            CurrentQuote.TaxRate = AppSettings.TaxRate;
        }

        [RelayCommand]
        private void ImportPrtFile()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "PRT Files (*.prt)|*.prt",
                InitialDirectory = @"C:\NestingSoftware\PRT_Output", // Change as needed
                Title = "Select a PRT File"
            };

            if (ofd.ShowDialog() == true)
            {
                var data = PrtFileParser.Parse(ofd.FileName);

                // 🧮 Calculate total sheet area
                double materialArea = data.RawLength * data.RawWidth;
                int sheetCount = data.RawMaterialQuantity;

                // ⏱️ Total cutting time
                double laserTimeMin = data.TotalCutDistance / data.FeedRate;
                double pierceTimeMin = (data.PierceRateSec * data.TotalPierces) / 60.0;
                double totalHours = (laserTimeMin + pierceTimeMin) / 60.0;

                // 💲 Apply quantity multiplier (sheet count)
                totalHours *= sheetCount;
                double laserCost = AppSettings.HourlyLaserRate * totalHours;

                double materialCost = data.MaterialCost * materialArea * sheetCount;

                var laserItem = new LineItem
                {
                    Description = $"Laser Cutting Time ({Math.Round(totalHours, 2)} hrs)",
                    Quantity = 1,
                    UnitPrice = Math.Round(laserCost, 2)
                };

                var materialItem = new LineItem
                {
                    Description = $"Material ({data.MaterialCode})",
                    Quantity = sheetCount,
                    UnitPrice = Math.Round(materialCost / sheetCount, 2)
                };

                LineItems.Clear();
                CurrentQuote.LineItems.Clear();

                LineItems.Add(laserItem);
                LineItems.Add(materialItem);

                CurrentQuote.LineItems.Add(laserItem);
                CurrentQuote.LineItems.Add(materialItem);

                // 💡 Refresh calculated totals
                CurrentQuote.NotifyTotalsChanged();
            }
        }
    }
}
