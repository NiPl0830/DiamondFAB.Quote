using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DiamondFAB.Quote
{
    public partial class CustomerPickerWindow : Window
    {
        private List<Customer> _all = new();
        public Customer? SelectedCustomer { get; private set; }

        public CustomerPickerWindow()
        {
            InitializeComponent();
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            _all = CustomerRepository.LoadAll();
            CustomerList.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q))
            {
                CustomerList.ItemsSource = _all;
                return;
            }

            CustomerList.ItemsSource = _all
                .Where(c =>
                    (!string.IsNullOrEmpty(c.CompanyName) && c.CompanyName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Email) && c.Email.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Address) && c.Address.ToLowerInvariant().Contains(q)))
                .ToList();
        }

        private void CustomerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomerList.SelectedItem is Customer c)
            {
                SelectedCustomer = c;
                DialogResult = true;
                Close();
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerList.SelectedItem is Customer c)
            {
                SelectedCustomer = c;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a customer.", "Select Customer",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ================== NEW: Add Customer ==================
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Add Customer",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 460,
                Height = 380,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F1115")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E9EDF5")),
                ShowInTaskbar = false
            };

            var cardBrush = (Brush?)new BrushConverter().ConvertFromString("#181B22") ?? Brushes.Black;
            var cardBorderBrush = (Brush?)new BrushConverter().ConvertFromString("#242937") ?? Brushes.DimGray;
            var inputBgBrush = (Brush?)new BrushConverter().ConvertFromString("#111319") ?? Brushes.Black;
            var inputBorder = (Brush?)new BrushConverter().ConvertFromString("#2A3040") ?? Brushes.SlateGray;
            var accentBrush = (Brush?)new BrushConverter().ConvertFromString("#4C8DF6") ?? Brushes.SteelBlue;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock
            {
                Text = "New Customer",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 0, 0, 10)
            };
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var card = new Border
            {
                Background = cardBrush,
                BorderBrush = cardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14)
            };
            Grid.SetRow(card, 1);
            root.Children.Add(card);

            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBox MakeBox() => new TextBox
            {
                Background = inputBgBrush,
                BorderBrush = inputBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var nameLbl = new TextBlock { Text = "Company Name", Margin = new Thickness(0, 10, 12, 0) };
            var addrLbl = new TextBlock { Text = "Address", Margin = new Thickness(0, 10, 12, 0) };
            var emailLbl = new TextBlock { Text = "Email", Margin = new Thickness(0, 10, 12, 0) };
            var taxLbl = new TextBlock { Text = "Default Tax (%)", Margin = new Thickness(0, 10, 12, 0) };
            var discLbl = new TextBlock { Text = "Default Discount (%)", Margin = new Thickness(0, 10, 12, 0) };

            var nameBox = MakeBox();
            var addrBox = MakeBox();
            var emailBox = MakeBox();
            var taxBox = MakeBox();
            var discBox = MakeBox();

            Grid.SetRow(nameLbl, 0); Grid.SetColumn(nameLbl, 0);
            Grid.SetRow(nameBox, 0); Grid.SetColumn(nameBox, 1);
            Grid.SetRow(addrLbl, 1); Grid.SetColumn(addrLbl, 0);
            Grid.SetRow(addrBox, 1); Grid.SetColumn(addrBox, 1);
            Grid.SetRow(emailLbl, 2); Grid.SetColumn(emailLbl, 0);
            Grid.SetRow(emailBox, 2); Grid.SetColumn(emailBox, 1);
            Grid.SetRow(taxLbl, 3); Grid.SetColumn(taxLbl, 0);
            Grid.SetRow(taxBox, 3); Grid.SetColumn(taxBox, 1);
            Grid.SetRow(discLbl, 4); Grid.SetColumn(discLbl, 0);
            Grid.SetRow(discBox, 4); Grid.SetColumn(discBox, 1);

            grid.Children.Add(nameLbl); grid.Children.Add(nameBox);
            grid.Children.Add(addrLbl); grid.Children.Add(addrBox);
            grid.Children.Add(emailLbl); grid.Children.Add(emailBox);
            grid.Children.Add(taxLbl); grid.Children.Add(taxBox);
            grid.Children.Add(discLbl); grid.Children.Add(discBox);

            card.Child = grid;

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttons, 2);

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B4358")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Height = 36,
                MinWidth = 110,
                Margin = new Thickness(0, 12, 8, 0)
            };
            cancelBtn.Click += (_, __) => dlg.DialogResult = false;

            var addBtn = new Button
            {
                Content = "Add",
                Background = accentBrush,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Height = 36,
                MinWidth = 110,
                Margin = new Thickness(0, 12, 0, 0)
            };
            addBtn.Click += (_, __) =>
            {
                var company = (nameBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(company))
                {
                    MessageBox.Show(dlg, "Company name is required.", "Validation",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double ParsePercent(string? s)
                {
                    s ??= string.Empty;
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var v)) return Clamp01(v);
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return Clamp01(v);
                    return 0;
                }

                static double Clamp01(double v)
                {
                    if (v < 0) return 0;
                    if (v > 100) return 100;
                    return v;
                }

                var tax = ParsePercent(taxBox.Text);
                var disc = ParsePercent(discBox.Text);

                var newCustomer = new Customer
                {
                    // If your model uses string Id:
                    Id = Guid.NewGuid().ToString(),

                    CompanyName = company,
                    Address = (addrBox.Text ?? string.Empty).Trim(),
                    Email = (emailBox.Text ?? string.Empty).Trim(),
                    DefaultTaxRate = tax,
                    DefaultDiscountPercent = disc
                };

                CustomerRepository.Add(newCustomer);
                dlg.DialogResult = true;
            };

            buttons.Children.Add(cancelBtn);
            buttons.Children.Add(addBtn);

            root.Children.Add(buttons);
            dlg.Content = root;

            // Show and refresh list if something was added
            var result = dlg.ShowDialog();
            if (result == true)
            {
                var all = CustomerRepository.LoadAll();
                var term = (SearchBox.Text ?? string.Empty).Trim();

                if (!string.IsNullOrEmpty(term))
                {
                    all = all.Where(c =>
                        (!string.IsNullOrEmpty(c.CompanyName) && c.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(c.Address) && c.Address.Contains(term, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                _all = all;
                CustomerList.ItemsSource = _all;
            }
        }
    }
}
