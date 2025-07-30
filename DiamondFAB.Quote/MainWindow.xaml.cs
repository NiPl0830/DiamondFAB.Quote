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
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = "Quote.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var vm = this.DataContext as DiamondFAB.Quote.ViewModels.MainViewModel;

                if (vm != null && vm.CurrentQuote.LineItems.Count > 0)
                {
                    PdfQuoteExporter.Export(vm.CurrentQuote, vm.AppSettings, saveDialog.FileName);
                    MessageBox.Show("PDF Exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("There is no quote to export. Please import a .PRT file first.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e)
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
                vm.StartNewQuoteCommand.Execute(null);
            }
        }
    }

}