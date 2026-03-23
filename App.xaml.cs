using System;
using System.IO;
using System.Linq;
using System.Windows;
using PontoDeEncontro.Services;

namespace PontoDeEncontro
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configurationService = new AppConfigurationService();
            var connectionWindow = new ConnectionSettingsWindow(configurationService);
            var dialogResult = connectionWindow.ShowDialog();

            if (dialogResult != true || string.IsNullOrWhiteSpace(connectionWindow.ConnectionString))
            {
                Shutdown();
                return;
            }

            var databaseService = new DatabaseService(connectionWindow.ConnectionString);
            Window mainWindow = ShouldOpenDirectPontoEncontro(e.Args)
                ? new PontoDeEncontroWindow(databaseService)
                : new MainWindow(databaseService);

            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private static bool ShouldOpenDirectPontoEncontro(string[] args)
        {
            if (args.Any(arg => string.Equals(arg, "--ponto-encontro", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var processPath = Environment.ProcessPath;
            var executableName = processPath is null
                ? string.Empty
                : Path.GetFileNameWithoutExtension(processPath);

            return executableName.Contains("Direto", StringComparison.OrdinalIgnoreCase)
                || executableName.Contains("PontoEncontro", StringComparison.OrdinalIgnoreCase);
        }
    }
}
