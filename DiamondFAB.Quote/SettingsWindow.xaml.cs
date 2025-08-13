using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using DiamondFAB.Quote.ViewModels;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace DiamondFAB.Quote
{
    public partial class SettingsWindow : Window
    {
        private Settings _settings;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private async void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // show version in footer
            var version = Assembly.GetExecutingAssembly()
                                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                  .InformationalVersion
                           ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
                           ?? "1.0.0";
            VersionTextBlock.Text = $"Version: {version}";

            // load settings asynchronously
            var settings = await Task.Run(SettingsService.Load);
            ApplySettings(settings);
        }

        private void ApplySettings(Settings settings)
        {
            _settings = settings ?? new Settings();

            CompanyNameBox.Text = _settings.CompanyName;
            CompanyAddressBox.Text = _settings.CompanyAddress;
            ContactEmailBox.Text = _settings.ContactEmail;

            LaserRateBox.Text = _settings.HourlyLaserRate.ToString(CultureInfo.InvariantCulture);
            TaxRateBox.Text = _settings.TaxRate.ToString(CultureInfo.InvariantCulture);

            // NEW: discount percent textbox (0–100)
            DiscountPercentBox.Text = _settings.DiscountPercent.ToString("0.##", CultureInfo.InvariantCulture);

            TermsBox.Text = _settings.TermsAndConditions;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.CompanyName = CompanyNameBox.Text?.Trim() ?? string.Empty;
            _settings.CompanyAddress = CompanyAddressBox.Text?.Trim() ?? string.Empty;
            _settings.ContactEmail = ContactEmailBox.Text?.Trim() ?? string.Empty;
            _settings.TermsAndConditions = TermsBox.Text ?? string.Empty;

            if (TryParseDouble(LaserRateBox.Text, out var laserRate))
                _settings.HourlyLaserRate = laserRate;

            if (TryParseDouble(TaxRateBox.Text, out var taxRate))
                _settings.TaxRate = taxRate;

            // NEW: read discount from textbox, clamp 0–100
            if (TryParseDouble(DiscountPercentBox.Text, out var discount))
            {
                if (discount < 0) discount = 0;
                if (discount > 100) discount = 100;
                _settings.DiscountPercent = discount;
            }

            SettingsService.Save(_settings);

            // push into MainViewModel
            if (Owner is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel vm)
            {
                vm.AppSettings = _settings;               // will refresh tax etc.
                vm.CurrentQuote.TaxRate = _settings.TaxRate;

                // you’ll wire discount into totals next; for now settings is saved and available
                vm.CurrentQuote.NotifyTotalsChanged();
            }

            Close();
        }

        private static bool TryParseDouble(string? text, out double value)
        {
            // Support both current culture and invariant (for users typing 10,5 vs 10.5)
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return true;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return true;

            value = 0;
            return false;
        }
    }
}
