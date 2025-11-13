using DiamondFAB.Quote.Models;
using DiamondFAB.Quote.Services;
using DiamondFAB.Quote.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiamondFAB.Quote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // F5 opens Settings
            if (e.Key == Key.F5)
            {
                SettingsButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm)
                return;

            var filterText = FilterBox.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                // Show all items
                LineItemsGrid.ItemsSource = vm.LineItems;
            }
            else
            {
                // Filter items by description
                var filtered = vm.LineItems.Where(item =>
                    item.Description != null &&
                    item.Description.ToLowerInvariant().Contains(filterText)).ToList();

                LineItemsGrid.ItemsSource = filtered;
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();

            if (DataContext is MainViewModel vm)
            {
                vm.AppSettings = SettingsService.Load();
                vm.CurrentQuote.TaxRate = vm.AppSettings.TaxRate;
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as DiamondFAB.Quote.ViewModels.MainViewModel;

            if (vm != null && vm.CurrentQuote.LineItems.Count > 0)
            {
                // Get company name and quote number, sanitize for file name
                string companyName = vm.AppSettings.CompanyName?.Trim() ?? "Company";
                string quoteNumber = vm.CurrentQuote.QuoteNumber?.Trim() ?? "Quote";

                // Remove invalid filename characters
                foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                {
                    companyName = companyName.Replace(c, '_');
                    quoteNumber = quoteNumber.Replace(c, '_');
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"{companyName}_{quoteNumber}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    PdfQuoteExporter.Export(vm.CurrentQuote, vm.AppSettings, saveDialog.FileName);
                    MessageBox.Show("PDF Exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("There is no quote to export. Please import a .PRT file first.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as DiamondFAB.Quote.ViewModels.MainViewModel;

            if (vm != null && vm.CurrentQuote.LineItems.Count > 0)
            {
                // Get company name and quote number, sanitize for file name
                string companyName = vm.AppSettings.CompanyName?.Trim() ?? "Company";
                string quoteNumber = vm.CurrentQuote.QuoteNumber?.Trim() ?? "Quote";

                // Remove invalid filename characters
                foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                {
                    companyName = companyName.Replace(c, '_');
                    quoteNumber = quoteNumber.Replace(c, '_');
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{companyName}_{quoteNumber}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    try
                    {
                        CsvExporter.ExportQuote(vm.CurrentQuote, vm.AppSettings, saveDialog.FileName);
                        Logger.Information($"CSV exported: {saveDialog.FileName}");
                        MessageBox.Show("CSV Exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("CSV export failed", ex);
                        MessageBox.Show($"Failed to export CSV:\n{ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("There is no quote to export. Please import a .PRT file first.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            var xmls = files.Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (xmls.Length > 0 && DataContext is MainViewModel vm)
            {
                vm.ImportXmlFiles(xmls);
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var result = MessageBox.Show(
                    "Clear current quote? This will remove all imported data and cannot be undone.",
                    "Confirm Clear",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    vm.StartNewQuoteCommand.Execute(null);
                }
            }
        }

        private void LineItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm)
                return;

            // Trigger totals recalculation after edit
            vm.CurrentQuote.NotifyTotalsChanged();
        }

        private void DeleteLineItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not LineItem lineItem)
                return;

            if (DataContext is not ViewModels.MainViewModel vm)
                return;

            var result = MessageBox.Show(
                $"Delete line item:\n\n{lineItem.Description}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                vm.LineItems.Remove(lineItem);
                vm.CurrentQuote.LineItems.Remove(lineItem);
                vm.CurrentQuote.NotifyTotalsChanged();
                Logger.Information($"Deleted line item: {lineItem.Description}");
            }
        }

        private void AddManualItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel vm)
                return;

            // Create a simple dialog to get item details
            var dialog = new Window
            {
                Title = "Add Line Item",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 400,
                Height = 280,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F1115"))
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var descLabel = new TextBlock { Text = "Description:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            var descBox = new TextBox { Margin = new Thickness(0, 0, 0, 15) };

            var qtyLabel = new TextBlock { Text = "Quantity:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            var qtyBox = new TextBox { Text = "1", Margin = new Thickness(0, 0, 0, 15) };

            var priceLabel = new TextBlock { Text = "Unit Price:", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 5) };
            var priceBox = new TextBox { Text = "0.00", Margin = new Thickness(0, 0, 0, 15) };

            Grid.SetRow(descLabel, 0);
            Grid.SetRow(descBox, 0);
            descBox.Margin = new Thickness(0, 25, 0, 15);

            Grid.SetRow(qtyLabel, 1);
            Grid.SetRow(qtyBox, 1);
            qtyBox.Margin = new Thickness(0, 25, 0, 15);

            Grid.SetRow(priceLabel, 2);
            Grid.SetRow(priceBox, 2);
            priceBox.Margin = new Thickness(0, 25, 0, 15);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttons, 4);

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            cancelBtn.Click += (s, args) => dialog.DialogResult = false;

            var addBtn = new Button
            {
                Content = "Add",
                Width = 80,
                Height = 32
            };
            addBtn.Click += (s, args) =>
            {
                var description = descBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(description))
                {
                    MessageBox.Show(dialog, "Please enter a description.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(qtyBox.Text, out var qty) || qty <= 0)
                {
                    MessageBox.Show(dialog, "Please enter a valid quantity.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(priceBox.Text, out var price) || price < 0)
                {
                    MessageBox.Show(dialog, "Please enter a valid price.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newItem = new LineItem
                {
                    Description = description,
                    Quantity = qty,
                    UnitPrice = Math.Round(price, 2)
                };

                vm.LineItems.Add(newItem);
                vm.CurrentQuote.LineItems.Add(newItem);
                vm.CurrentQuote.NotifyTotalsChanged();
                Logger.Information($"Added manual line item: {description}");

                dialog.DialogResult = true;
            };

            buttons.Children.Add(cancelBtn);
            buttons.Children.Add(addBtn);

            grid.Children.Add(descLabel);
            grid.Children.Add(descBox);
            grid.Children.Add(qtyLabel);
            grid.Children.Add(qtyBox);
            grid.Children.Add(priceLabel);
            grid.Children.Add(priceBox);
            grid.Children.Add(buttons);

            dialog.Content = grid;
            dialog.ShowDialog();
        }
    }

}