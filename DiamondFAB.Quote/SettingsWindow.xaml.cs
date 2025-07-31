using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using DiamondFAB.Quote.ViewModels;
using System;
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

        private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load settings in background
            var settings = await Task.Run(SettingsService.Load);
            ApplySettings(settings);

            // Display version label safely after initialization
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N/A";
            VersionTextBlock.Text = $"Version: {version}";
        }

        private void ApplySettings(Settings settings)
        {
            _settings = settings;

            CompanyNameBox.Text = _settings.CompanyName;
            CompanyAddressBox.Text = _settings.CompanyAddress;
            ContactEmailBox.Text = _settings.ContactEmail;
            LaserRateBox.Text = _settings.HourlyLaserRate.ToString();
            TaxRateBox.Text = _settings.TaxRate.ToString();
            TermsBox.Text = _settings.TermsAndConditions;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.CompanyName = CompanyNameBox.Text;
            _settings.CompanyAddress = CompanyAddressBox.Text;
            _settings.ContactEmail = ContactEmailBox.Text;
            _settings.TermsAndConditions = TermsBox.Text;

            if (double.TryParse(LaserRateBox.Text, out double laserRate))
                _settings.HourlyLaserRate = laserRate;

            if (double.TryParse(TaxRateBox.Text, out double taxRate))
                _settings.TaxRate = taxRate;

            SettingsService.Save(_settings);

            if (Owner is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel viewModel)
            {
                viewModel.AppSettings = _settings;
                viewModel.CurrentQuote.TaxRate = _settings.TaxRate;
            }

            this.Close();
        }
    }
}
