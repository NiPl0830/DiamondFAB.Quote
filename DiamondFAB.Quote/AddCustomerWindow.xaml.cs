using System;
using System.Globalization;
using System.Windows;
using DiamondFAB.Quote.Models;

namespace DiamondFAB.Quote
{
    public partial class AddCustomerWindow : Window
    {
        public Customer? Result { get; private set; }

        public AddCustomerWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = (CompanyNameBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Company Name is required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var address = (AddressBox.Text ?? "").Trim();
            var email = (EmailBox.Text ?? "").Trim();

            double tax = ParseDoubleOrZero(TaxBox.Text);
            double discount = ParseDoubleOrZero(DiscountBox.Text);

            if (tax < 0) tax = 0;
            if (discount < 0) discount = 0;
            if (discount > 100) discount = 100;

            Result = new Customer
            {
                Id = Guid.NewGuid().ToString(),
                CompanyName = name,
                Address = address,
                Email = email,
                DefaultTaxRate = tax,
                DefaultDiscountPercent = discount
            };

            DialogResult = true;
            Close();
        }

        private static double ParseDoubleOrZero(string? s)
        {
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var v)) return v;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return v;
            return 0;
        }
    }
}