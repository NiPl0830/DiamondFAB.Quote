using DiamondFAB.Quote.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiamondFAB.Quote
{
    public partial class SavedQuotesWindow : Window
    {
        private List<Row> _all = new();
        public string? SelectedPath { get; private set; }

        public SavedQuotesWindow()
        {
            InitializeComponent();
            LoadRows();
        }

        private void LoadRows()
        {
            // SavedQuotesService.ListSaved() returns:
            // (string FileName, string FullPath, string Customer, string QuoteNumber, DateTime Modified)
            _all = SavedQuotesService.ListSaved()
                .OrderByDescending(s => s.Modified)
                .Select(s =>
                {
                    string grandTotalDisplay = "";
                    try
                    {
                        // Load the quote to get the grand total
                        var quote = SavedQuotesService.Load(s.FullPath);
                        if (quote != null)
                        {
                            grandTotalDisplay = quote.GrandTotal.ToString("C", CultureInfo.CurrentCulture);
                        }
                    }
                    catch
                    {
                        // If we can't load the quote, just leave the total empty
                        grandTotalDisplay = "—";
                    }

                    return new Row
                    {
                        FullPath = s.FullPath,
                        CustomerName = string.IsNullOrWhiteSpace(s.Customer) ? "Unknown" : s.Customer,
                        QuoteNumber = s.QuoteNumber ?? "",
                        SavedAtDisplay = s.Modified.ToString("g", CultureInfo.CurrentCulture),
                        GrandTotalDisplay = grandTotalDisplay
                    };
                })
                .ToList();

            QuotesGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(q))
            {
                QuotesGrid.ItemsSource = _all;
                return;
            }

            QuotesGrid.ItemsSource = _all.Where(r =>
                (r.CustomerName ?? "").ToLowerInvariant().Contains(q) ||
                (r.QuoteNumber ?? "").ToLowerInvariant().Contains(q) ||
                (r.SavedAtDisplay ?? "").ToLowerInvariant().Contains(q));
        }

        private void QuotesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();

        private void Open_Click(object sender, RoutedEventArgs e) => OpenSelected();

        private void OpenSelected()
        {
            if (QuotesGrid.SelectedItem is Row row)
            {
                SelectedPath = row.FullPath;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a saved quote.", "Open Saved Quote",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (QuotesGrid.SelectedItem is not Row row)
            {
                MessageBox.Show("Please select a saved quote to delete.", "Delete Saved Quote",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete this saved quote?\n\nCustomer: {row.CustomerName}\nQuote #: {row.QuoteNumber}",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            bool ok = SavedQuotesService.Delete(row.FullPath);
            if (!ok)
            {
                MessageBox.Show("Could not delete the selected file. It may be open or locked.",
                    "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Remove from the list + refresh the view
            _all.Remove(row);
            QuotesGrid.ItemsSource = null;
            QuotesGrid.ItemsSource = _all;
        }

        private class Row
        {
            public string? FullPath { get; set; }
            public string? CustomerName { get; set; }
            public string? QuoteNumber { get; set; }
            public string? SavedAtDisplay { get; set; }
            public string? GrandTotalDisplay { get; set; }
        }
    }
}