using QuestPDF.Infrastructure;
using System.Configuration;
using System.Data;
using System.Windows;

namespace DiamondFAB.Quote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Tell QuestPDF what license mode you're using
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }

}
