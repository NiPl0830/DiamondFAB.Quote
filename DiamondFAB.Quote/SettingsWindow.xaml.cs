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
        private Settings _settings = new();

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private async void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Show version (trim metadata)
            string version = Assembly.GetExecutingAssembly()
                                     .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                     .InformationalVersion
                           ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
                           ?? "1.0.0";
            int plus = version.IndexOf('+'); if (plus >= 0) version = version[..plus];
            int dash = version.IndexOf('-'); if (dash >= 0) version = version[..dash];
            VersionTextBlock.Text = $"Version: {version}";

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

            if (TryParseDouble(DiscountPercentBox.Text, out var discount))
                _settings.DiscountPercent = Math.Clamp(discount, 0, 100);

            SettingsService.Save(_settings);

            if (Owner is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel vm)
            {
                vm.AppSettings = _settings;
                vm.CurrentQuote.TaxRate = _settings.TaxRate;
                vm.CurrentQuote.DiscountPercent = _settings.DiscountPercent;
                vm.CurrentQuote.NotifyTotalsChanged();
            }

            Close();
        }

        private static bool TryParseDouble(string? text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value)) return true;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
            value = 0;
            return false;
        }

        // NEW: open customer picker
        private void SelectCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new CustomerPickerWindow
            {
                Owner = this
            };

            if (picker.ShowDialog() == true && picker.SelectedCustomer != null)
            {
                var c = picker.SelectedCustomer;

                // Push into the Settings UI fields
                CompanyNameBox.Text = c.CompanyName ?? string.Empty;
                CompanyAddressBox.Text = c.Address ?? string.Empty;
                ContactEmailBox.Text = c.Email ?? string.Empty;

                // Non-nullable doubles: assign directly
                TaxRateBox.Text = c.DefaultTaxRate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                DiscountPercentBox.Text = c.DefaultDiscountPercent.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}