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
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                Run(e.Args);
            }
            catch (Exception ex)
            {
                LogService.LogError("App.OnStartup", ex);
                MessageBox.Show(
                    $"Erro ao iniciar o aplicativo:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Ponto de Encontro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void Run(string[] args)
        {
            var configService = new AppConfigurationService();
            var mode = DetectMode(args);

            if (mode == LaunchMode.Configuracao)
            {
                var connectionString = ShowConnectionDialog(configService);
                if (connectionString == null)
                {
                    Shutdown();
                    return;
                }
                ShowMainWindow(new PontoDeEncontroWindow(new DatabaseService(connectionString)));
                return;
            }

            // Direto ou Monitor: tenta usar conexao salva, senao pede configuracao
            if (!TryGetConnectionString(configService, out var connStr))
            {
                Shutdown();
                return;
            }

            var db = new DatabaseService(connStr);
            var window = mode == LaunchMode.Direto
                ? (Window)new PontoDeEncontroWindow(db)
                : new MainWindow(db);

            ShowMainWindow(window);
        }

        private void ShowMainWindow(Window window)
        {
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }

        private static string? ShowConnectionDialog(AppConfigurationService configService)
        {
            var dialog = new ConnectionSettingsWindow(configService);
            return dialog.ShowDialog() == true ? dialog.ConnectionString : null;
        }

        private static bool TryGetConnectionString(AppConfigurationService configService, out string connectionString)
        {
            if (configService.TryGetSavedConnectionString(out connectionString))
                return true;

            var result = ShowConnectionDialog(configService);
            if (result != null)
            {
                connectionString = result;
                return true;
            }

            connectionString = string.Empty;
            return false;
        }

        private static LaunchMode DetectMode(string[] args)
        {
            var exeName = GetExecutableName();

            if (args.Contains("--configure", StringComparer.OrdinalIgnoreCase)
                || exeName.Contains("Config", StringComparison.OrdinalIgnoreCase)
                || exeName.Contains("Conexao", StringComparison.OrdinalIgnoreCase))
                return LaunchMode.Configuracao;

            if (args.Contains("--ponto-encontro", StringComparer.OrdinalIgnoreCase)
                || exeName.Contains("Direto", StringComparison.OrdinalIgnoreCase))
                return LaunchMode.Direto;

            return LaunchMode.Monitor;
        }

        private static string GetExecutableName()
        {
            var path = Environment.ProcessPath;
            return path is null ? string.Empty : Path.GetFileNameWithoutExtension(path);
        }

        private enum LaunchMode { Monitor, Direto, Configuracao }
    }
}
