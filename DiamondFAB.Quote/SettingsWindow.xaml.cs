using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using DiamondFAB.Quote.ViewModels;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace DiamondFAB.Quote
{
    public partial class SettingsWindow : Window
    {
        private Settings _settings = new();

        // Keys we'll use inside the ExtraCharges list
        private const string KeySetup = "SetupHandling";
        private const string KeyDeburr = "Deburr";
        private const string KeyWelding = "Welding";
        private const string KeyPaint = "Paint";

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;

            // Add validation on text changed for real-time feedback
            ContactEmailBox.TextChanged += (s, e) => ValidateEmail();
            LaserRateBox.TextChanged += (s, e) => ValidateLaserRate();
            TaxRateBox.TextChanged += (s, e) => ValidateTaxRate();
            DiscountPercentBox.TextChanged += (s, e) => ValidateDiscountPercent();
        }

        private void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Show version
            string version = Assembly.GetExecutingAssembly()
                                     .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                     .InformationalVersion
                           ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
                           ?? "1.0.0";
            int plus = version.IndexOf('+'); if (plus >= 0) version = version[..plus];
            int dash = version.IndexOf('-'); if (dash >= 0) version = version[..dash];
            VersionTextBlock.Text = $"Version: {version}";

            _settings = SettingsService.Load();
            ApplySettings(_settings);
        }

        private void ApplySettings(Settings settings)
        {
            _settings = settings ?? new Settings();

            // Make sure the standard 4 charges exist
            EnsureDefaultCharges(_settings);

            // General Tab - Customer Info
            CompanyNameBox.Text = _settings.CompanyName ?? string.Empty;
            CompanyAddressBox.Text = _settings.CompanyAddress ?? string.Empty;
            ContactEmailBox.Text = _settings.ContactEmail ?? string.Empty;
            CompanyPhoneBox.Text = _settings.CompanyPhone ?? string.Empty;
            LogoPathBox.Text = _settings.LogoPath ?? string.Empty;

            // Preferences Tab - Your Company Info
            YourCompanyNameBox.Text = _settings.YourCompanyName ?? string.Empty;
            YourCompanyAddressBox.Text = _settings.YourCompanyAddress ?? string.Empty;
            YourContactEmailBox.Text = _settings.YourContactEmail ?? string.Empty;
            YourCompanyPhoneBox.Text = _settings.YourCompanyPhone ?? string.Empty;
            YourLogoPathBox.Text = _settings.YourLogoPath ?? string.Empty;

            // Rates Tab
            LaserRateBox.Text = _settings.HourlyLaserRate.ToString(CultureInfo.InvariantCulture);
            TaxRateBox.Text = _settings.TaxRate.ToString(CultureInfo.InvariantCulture);
            DiscountPercentBox.Text = _settings.DiscountPercent.ToString("0.##", CultureInfo.InvariantCulture);

            // Extra Charges
            var setup = GetCharge(KeySetup) ?? new ExtraCharge { Key = KeySetup, Name = "Setup / Handling" };
            var deburr = GetCharge(KeyDeburr) ?? new ExtraCharge { Key = KeyDeburr, Name = "Deburr" };
            var welding = GetCharge(KeyWelding) ?? new ExtraCharge { Key = KeyWelding, Name = "Welding" };
            var paint = GetCharge(KeyPaint) ?? new ExtraCharge { Key = KeyPaint, Name = "Paint" };

            SetupHandlingCheck.IsChecked = setup.IsEnabled;
            DeburrCheck.IsChecked = deburr.IsEnabled;
            WeldingCheck.IsChecked = welding.IsEnabled;
            PaintCheck.IsChecked = paint.IsEnabled;

            SetupHandlingAmountBox.Text = setup.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            DeburrAmountBox.Text = deburr.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            WeldingAmountBox.Text = welding.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            PaintAmountBox.Text = paint.Amount.ToString("0.##", CultureInfo.InvariantCulture);

            // Preferences Tab
            DefaultImportFolderBox.Text = _settings.DefaultImportFolder ?? string.Empty;
            PreserveTaxRateCheck.IsChecked = _settings.PreserveHistoricalTaxRates;
            PreserveDiscountCheck.IsChecked = _settings.PreserveHistoricalDiscounts;
            ShowPartBreakdownCheck.IsChecked = _settings.ShowPartLevelBreakdown;
            TermsBox.Text = _settings.TermsAndConditions ?? string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate all inputs before saving
            bool isValid = true;

            if (!ValidateEmail()) isValid = false;
            if (!ValidateLaserRate()) isValid = false;
            if (!ValidateTaxRate()) isValid = false;
            if (!ValidateDiscountPercent()) isValid = false;

            if (!isValid)
            {
                MessageBox.Show(
                    "Please correct the validation errors before saving.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                // General Tab - Customer Info
                _settings.CompanyName = CompanyNameBox.Text?.Trim() ?? string.Empty;
                _settings.CompanyAddress = CompanyAddressBox.Text?.Trim() ?? string.Empty;
                _settings.ContactEmail = ContactEmailBox.Text?.Trim() ?? string.Empty;
                _settings.CompanyPhone = CompanyPhoneBox.Text?.Trim() ?? string.Empty;
                _settings.LogoPath = LogoPathBox.Text?.Trim() ?? string.Empty;

                // Preferences Tab - Your Company Info
                _settings.YourCompanyName = YourCompanyNameBox.Text?.Trim() ?? string.Empty;
                _settings.YourCompanyAddress = YourCompanyAddressBox.Text?.Trim() ?? string.Empty;
                _settings.YourContactEmail = YourContactEmailBox.Text?.Trim() ?? string.Empty;
                _settings.YourCompanyPhone = YourCompanyPhoneBox.Text?.Trim() ?? string.Empty;
                _settings.YourLogoPath = YourLogoPathBox.Text?.Trim() ?? string.Empty;
                _settings.TermsAndConditions = TermsBox.Text ?? string.Empty;

                // Rates
                if (TryParseDouble(LaserRateBox.Text, out var laserRate))
                    _settings.HourlyLaserRate = Math.Max(0, laserRate);

                if (TryParseDouble(TaxRateBox.Text, out var taxRate))
                    _settings.TaxRate = Math.Clamp(taxRate, 0, 100);

                if (TryParseDouble(DiscountPercentBox.Text, out var discount))
                    _settings.DiscountPercent = Math.Clamp(discount, 0, 100);

                // Extra Charges
                var setup = GetCharge(KeySetup) ?? new ExtraCharge { Key = KeySetup, Name = "Setup / Handling" };
                var deburr = GetCharge(KeyDeburr) ?? new ExtraCharge { Key = KeyDeburr, Name = "Deburr" };
                var welding = GetCharge(KeyWelding) ?? new ExtraCharge { Key = KeyWelding, Name = "Welding" };
                var paint = GetCharge(KeyPaint) ?? new ExtraCharge { Key = KeyPaint, Name = "Paint" };

                void EnsureInList(ExtraCharge c)
                {
                    if (!_settings.ExtraCharges.Any(x => x.Key == c.Key))
                        _settings.ExtraCharges.Add(c);
                }
                EnsureInList(setup);
                EnsureInList(deburr);
                EnsureInList(welding);
                EnsureInList(paint);

                setup.IsEnabled = SetupHandlingCheck.IsChecked == true;
                deburr.IsEnabled = DeburrCheck.IsChecked == true;
                welding.IsEnabled = WeldingCheck.IsChecked == true;
                paint.IsEnabled = PaintCheck.IsChecked == true;

                if (TryParseMoney(SetupHandlingAmountBox.Text, out var setupAmt)) setup.Amount = Math.Max(0, setupAmt);
                if (TryParseMoney(DeburrAmountBox.Text, out var deburrAmt)) deburr.Amount = Math.Max(0, deburrAmt);
                if (TryParseMoney(WeldingAmountBox.Text, out var weldingAmt)) welding.Amount = Math.Max(0, weldingAmt);
                if (TryParseMoney(PaintAmountBox.Text, out var paintAmt)) paint.Amount = Math.Max(0, paintAmt);

                // Preferences Tab
                _settings.DefaultImportFolder = DefaultImportFolderBox.Text?.Trim() ?? string.Empty;
                _settings.PreserveHistoricalTaxRates = PreserveTaxRateCheck.IsChecked == true;
                _settings.PreserveHistoricalDiscounts = PreserveDiscountCheck.IsChecked == true;
                _settings.ShowPartLevelBreakdown = ShowPartBreakdownCheck.IsChecked == true;

                // Save to disk
                SettingsService.Save(_settings);
                Logger.Information("Settings saved successfully");

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
            catch (Exception ex)
            {
                Logger.Error("Failed to save settings", ex);
                MessageBox.Show(
                    $"Failed to save settings:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

        // ========== VALIDATION ==========

        private bool ValidateEmail()
        {
            var email = ContactEmailBox.Text?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                EmailErrorText.Visibility = Visibility.Collapsed;
                return true; // Empty is allowed
            }

            // Simple email regex
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            bool isValid = regex.IsMatch(email);

            EmailErrorText.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            return isValid;
        }

        private bool ValidateLaserRate()
        {
            bool isValid = TryParseDouble(LaserRateBox.Text, out var value) && value >= 0;
            LaserRateErrorText.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            return isValid;
        }

        private bool ValidateTaxRate()
        {
            bool isValid = TryParseDouble(TaxRateBox.Text, out var value) && value >= 0 && value <= 100;
            TaxRateErrorText.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            return isValid;
        }

        private bool ValidateDiscountPercent()
        {
            bool isValid = TryParseDouble(DiscountPercentBox.Text, out var value) && value >= 0 && value <= 100;
            DiscountErrorText.Visibility = isValid ? Visibility.Collapsed : Visibility.Visible;
            return isValid;
        }

        // ========== FILE PICKERS ==========

        private void BrowseLogoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Select Customer Logo",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                LogoPathBox.Text = dialog.FileName;
                Logger.Information($"Customer logo selected: {dialog.FileName}");
            }
        }

        private void BrowseYourLogoButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
                Title = "Select Your Company Logo",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                YourLogoPathBox.Text = dialog.FileName;
                Logger.Information($"Your company logo selected: {dialog.FileName}");
            }
        }

        private void BrowseImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "Select default folder for importing XML files",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            // Set initial directory if exists
            if (!string.IsNullOrEmpty(DefaultImportFolderBox.Text) && Directory.Exists(DefaultImportFolderBox.Text))
            {
                dialog.SelectedPath = DefaultImportFolderBox.Text;
            }

            if (dialog.ShowDialog(this) == true)
            {
                DefaultImportFolderBox.Text = dialog.SelectedPath;
                Logger.Information($"Default import folder set: {dialog.SelectedPath}");
            }
        }

        // ========== CUSTOMER SELECTION ==========

        private void SelectCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new CustomerPickerWindow { Owner = this };

            if (picker.ShowDialog() == true && picker.SelectedCustomer != null)
            {
                var c = picker.SelectedCustomer;

                CompanyNameBox.Text = c.CompanyName ?? string.Empty;
                CompanyAddressBox.Text = c.Address ?? string.Empty;
                ContactEmailBox.Text = c.Email ?? string.Empty;
                CompanyPhoneBox.Text = c.Phone ?? string.Empty;

                TaxRateBox.Text = c.DefaultTaxRate.ToString(CultureInfo.InvariantCulture);
                DiscountPercentBox.Text = c.DefaultDiscountPercent.ToString(CultureInfo.InvariantCulture);

                Logger.Information($"Loaded customer: {c.CompanyName}");
            }
        }

        // ========== HELPERS ==========

        private void EnsureDefaultCharges(Settings s)
        {
            s.ExtraCharges ??= new System.Collections.Generic.List<ExtraCharge>();

            EnsureCharge(KeySetup, "Setup / Handling");
            EnsureCharge(KeyDeburr, "Deburr");
            EnsureCharge(KeyWelding, "Welding");
            EnsureCharge(KeyPaint, "Paint");
        }

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
                c = new ExtraCharge
                {
                    Key = key,
                    Name = key,
                    IsEnabled = false,
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
            var cleaned = (text ?? string.Empty).Trim().TrimStart('$');
            if (double.TryParse(cleaned, NumberStyles.Currency, CultureInfo.CurrentCulture, out value)) return true;
            if (double.TryParse(cleaned, NumberStyles.Currency, CultureInfo.InvariantCulture, out value)) return true;
            value = 0;
            return false;
        }
    }
}
