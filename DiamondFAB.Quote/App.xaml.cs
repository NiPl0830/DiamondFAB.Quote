using DiamondFAB.Quote.Services;
using QuestPDF.Infrastructure;
using System;
using System.Windows;

namespace DiamondFAB.Quote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            // Initialize logger (creates log directory and configuration)
            Logger.Information("Application starting");

            // Tell QuestPDF what license mode you're using
            QuestPDF.Settings.License = LicenseType.Community;

            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease check the log file for details.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.Error("Unhandled application exception", ex);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Information("Application shutting down");
            Logger.CloseAndFlush();
            base.OnExit(e);
        }
    }

}
