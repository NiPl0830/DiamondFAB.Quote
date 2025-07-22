using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using DiamondFAB.Quote.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DiamondFAB.Quote
{
    public partial class SettingsWindow : Window
    {
        private Settings _settings;
        private ObservableCollection<KeyValuePair<string, double>> _materialRates;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = SettingsService.Load();

            CompanyNameBox.Text = _settings.CompanyName;
            CompanyAddressBox.Text = _settings.CompanyAddress;
            ContactEmailBox.Text = _settings.ContactEmail;
            LaserRateBox.Text = _settings.HourlyLaserRate.ToString();
            TaxRateBox.Text = _settings.TaxRate.ToString();
            TermsBox.Text = _settings.TermsAndConditions;

            _materialRates = new ObservableCollection<KeyValuePair<string, double>>(_settings.MaterialRates);
            MaterialGrid.ItemsSource = _materialRates;
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

            // Convert observable list back to dictionary
            var newRates = new Dictionary<string, double>();
            foreach (var kvp in _materialRates)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                    newRates[kvp.Key] = kvp.Value;
            }

            _settings.MaterialRates = newRates;

            SettingsService.Save(_settings);

            // 🔁 Push changes into MainViewModel
            if (Owner is MainWindow mainWindow &&
                mainWindow.DataContext is MainViewModel viewModel)
            {
                viewModel.AppSettings = _settings;
                viewModel.CurrentQuote.TaxRate = _settings.TaxRate;
            }

            //MessageBox.Show("Settings saved successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
