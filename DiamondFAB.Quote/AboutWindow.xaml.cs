using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace DiamondFAB.Quote
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            // Set version from assembly
            var fileVersion = FileVersionInfo
                .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                .FileVersion;

            string version = !string.IsNullOrWhiteSpace(fileVersion)
                ? fileVersion.Split('+', '-')[0]
                : (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

            VersionText.Text = $"Version {version}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
