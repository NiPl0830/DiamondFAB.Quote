using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using DiamondFAB.Quote.ViewModels;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace DiamondFAB.Quote
{
    public partial class SettingsWindow : Window
    {
        private Settings _settings = new();

        // Keys we’ll use inside the ExtraCharges list
        private const string KeySetup = "SetupHandling";
        private const string KeyDeburr = "Deburr";
        private const string KeyWelding = "Welding";
        private const string KeyPaint = "Paint";

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private async void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Show version (trim +metadata / -prerelease)
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

            // Make sure the standard 4 charges exist
            EnsureDefaultCharges(_settings);

            // Company
            CompanyNameBox.Text = _settings.CompanyName ?? string.Empty;
            CompanyAddressBox.Text = _settings.CompanyAddress ?? string.Empty;
            ContactEmailBox.Text = _settings.ContactEmail ?? string.Empty;

            // Rates
            LaserRateBox.Text = _settings.HourlyLaserRate.ToString(CultureInfo.InvariantCulture);
            TaxRateBox.Text = _settings.TaxRate.ToString(CultureInfo.InvariantCulture);
            DiscountPercentBox.Text = _settings.DiscountPercent.ToString("0.##", CultureInfo.InvariantCulture);

            // Extra Charges (read from list by key)
            var setup = GetCharge(KeySetup) ?? new ExtraCharge { Key = KeySetup, Name = "Setup / Handling" };
            var deburr = GetCharge(KeyDeburr) ?? new ExtraCharge { Key = KeyDeburr, Name = "Deburr" };
            var welding = GetCharge(KeyWelding) ?? new ExtraCharge { Key = KeyWelding, Name = "Welding" };
            var paint = GetCharge(KeyPaint) ?? new ExtraCharge { Key = KeyPaint, Name = "Paint" };

            // NOTE: use IsEnabled (not Enabled)
            SetupHandlingCheck.IsChecked = setup.IsEnabled;
            DeburrCheck.IsChecked = deburr.IsEnabled;
            WeldingCheck.IsChecked = welding.IsEnabled;
            PaintCheck.IsChecked = paint.IsEnabled;

            SetupHandlingAmountBox.Text = setup.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            DeburrAmountBox.Text = deburr.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            WeldingAmountBox.Text = welding.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            PaintAmountBox.Text = paint.Amount.ToString("0.##", CultureInfo.InvariantCulture);

            // Terms
            TermsBox.Text = _settings.TermsAndConditions ?? string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Company
            _settings.CompanyName = CompanyNameBox.Text?.Trim() ?? string.Empty;
            _settings.CompanyAddress = CompanyAddressBox.Text?.Trim() ?? string.Empty;
            _settings.ContactEmail = ContactEmailBox.Text?.Trim() ?? string.Empty;
            _settings.TermsAndConditions = TermsBox.Text ?? string.Empty;

            // Rates
            if (TryParseDouble(LaserRateBox.Text, out var laserRate))
                _settings.HourlyLaserRate = Math.Max(0, laserRate);

            if (TryParseDouble(TaxRateBox.Text, out var taxRate))
                _settings.TaxRate = Math.Clamp(taxRate, 0, 100);

            if (TryParseDouble(DiscountPercentBox.Text, out var discount))
                _settings.DiscountPercent = Math.Clamp(discount, 0, 100);

            // Extra Charges (update list entries)
            var setup = GetCharge(KeySetup) ?? new ExtraCharge { Key = KeySetup, Name = "Setup / Handling" };
            var deburr = GetCharge(KeyDeburr) ?? new ExtraCharge { Key = KeyDeburr, Name = "Deburr" };
            var welding = GetCharge(KeyWelding) ?? new ExtraCharge { Key = KeyWelding, Name = "Welding" };
            var paint = GetCharge(KeyPaint) ?? new ExtraCharge { Key = KeyPaint, Name = "Paint" };

            // Ensure they exist in the list if they were missing
            void EnsureInList(ExtraCharge c)
            {
                if (!_settings.ExtraCharges.Any(x => x.Key == c.Key))
                    _settings.ExtraCharges.Add(c);
            }
            EnsureInList(setup);
            EnsureInList(deburr);
            EnsureInList(welding);
            EnsureInList(paint);

            // NOTE: ExtraCharge uses IsEnabled (not Enabled)
            setup.IsEnabled = SetupHandlingCheck.IsChecked == true;
            deburr.IsEnabled = DeburrCheck.IsChecked == true;
            welding.IsEnabled = WeldingCheck.IsChecked == true;
            paint.IsEnabled = PaintCheck.IsChecked == true;

            if (TryParseMoney(SetupHandlingAmountBox.Text, out var setupAmt)) setup.Amount = Math.Max(0, setupAmt);
            if (TryParseMoney(DeburrAmountBox.Text, out var deburrAmt)) deburr.Amount = Math.Max(0, deburrAmt);
            if (TryParseMoney(WeldingAmountBox.Text, out var weldingAmt)) welding.Amount = Math.Max(0, weldingAmt);
            if (TryParseMoney(PaintAmountBox.Text, out var paintAmt)) paint.Amount = Math.Max(0, paintAmt);

            // Save to disk
            SettingsService.Save(_settings);

            // Push to main VM so totals refresh
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

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        // ---------- Helpers ----------

        // Ensure list exists and has the 4 default keys with stable labels
        private void EnsureDefaultCharges(Settings s)
        {
            s.ExtraCharges ??= new System.Collections.Generic.List<ExtraCharge>();

            EnsureCharge(KeySetup, "Setup / Handling");
            EnsureCharge(KeyDeburr, "Deburr");
            EnsureCharge(KeyWelding, "Welding");
            EnsureCharge(KeyPaint, "Paint");
        }

        // Returns an existing charge by key or creates one with defaults
        private ExtraCharge EnsureCharge(string key, string name)
        {
            var charge = _settings.ExtraCharges.FirstOrDefault(c => c.Key == key);
            if (charge == null)
            {
                charge = new ExtraCharge
                {
                    Key = key,
                    Name = name,
                    IsEnabled = false,
                    Amount = 0
                };
                _settings.ExtraCharges.Add(charge);
            }

            // Backfill display name if missing
            if (string.IsNullOrWhiteSpace(charge.Name))
                charge.Name = name;

            return charge;
        }

        private ExtraCharge GetCharge(string key)
        {
            var c = _settings.ExtraCharges
                .FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

            if (c == null)
            {
                // Shouldn’t happen after EnsureDefaultCharges, but be defensive:
                c = new ExtraCharge
                {
                    Key = key,
                    Name = key,        // changed from Label → Name
                    IsEnabled = false, // changed from Enabled → IsEnabled
                    Amount = 0.0
                };
                _settings.ExtraCharges.Add(c);
            }

            return c;
        }

        private static bool TryParseDouble(string? text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value)) return true;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
            value = 0;
            return false;
        }

        private static bool TryParseMoney(string? text, out double value)
        {
            // allow $, commas, both cultures
            var cleaned = (text ?? string.Empty).Trim().TrimStart('$');
            if (double.TryParse(cleaned, NumberStyles.Currency, CultureInfo.CurrentCulture, out value)) return true;
            if (double.TryParse(cleaned, NumberStyles.Currency, CultureInfo.InvariantCulture, out value)) return true;
            value = 0;
            return false;
        }

        // Customer picker
        private void SelectCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new CustomerPickerWindow { Owner = this };

            if (picker.ShowDialog() == true && picker.SelectedCustomer != null)
            {
                var c = picker.SelectedCustomer;

                CompanyNameBox.Text = c.CompanyName ?? string.Empty;
                CompanyAddressBox.Text = c.Address ?? string.Empty;
                ContactEmailBox.Text = c.Email ?? string.Empty;

                TaxRateBox.Text = c.DefaultTaxRate.ToString(CultureInfo.InvariantCulture);
                DiscountPercentBox.Text = c.DefaultDiscountPercent.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}