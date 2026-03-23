using System;
using System.Data.SqlClient;
using System.Windows;
using PontoDeEncontro.Models;
using PontoDeEncontro.Services;

namespace PontoDeEncontro
{
    public partial class ConnectionSettingsWindow : Window
    {
        private readonly AppConfigurationService _configurationService;

        public ConnectionSettingsWindow(AppConfigurationService configurationService)
        {
            InitializeComponent();
            _configurationService = configurationService;
            LoadCurrentSettings();
        }

        public string? ConnectionString { get; private set; }

        private void LoadCurrentSettings()
        {
            var settings = _configurationService.LoadSettings();
            TxtServer.Text = settings.Server;
            TxtDatabase.Text = settings.Database;
            TxtUser.Text = settings.UserId;
            TxtPassword.Password = settings.Password;
            ChkIntegratedSecurity.IsChecked = settings.UseIntegratedSecurity;
            UpdateCredentialFields();
        }

        private DatabaseSettings GetSettingsFromForm()
        {
            return new DatabaseSettings
            {
                Server = TxtServer.Text.Trim(),
                Database = TxtDatabase.Text.Trim(),
                UserId = TxtUser.Text.Trim(),
                Password = TxtPassword.Password,
                UseIntegratedSecurity = ChkIntegratedSecurity.IsChecked == true
            };
        }

        private void UpdateCredentialFields()
        {
            var useIntegratedSecurity = ChkIntegratedSecurity.IsChecked == true;
            TxtUser.IsEnabled = !useIntegratedSecurity;
            TxtPassword.IsEnabled = !useIntegratedSecurity;
        }

        private bool TryBuildValidatedConnectionString(out DatabaseSettings settings, out string connectionString)
        {
            settings = GetSettingsFromForm();
            connectionString = string.Empty;

            try
            {
                _configurationService.ValidateSettings(settings);
                connectionString = _configurationService.BuildConnectionString(settings);
                return true;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = ex.Message;
                return false;
            }
        }

        private void ChkIntegratedSecurity_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCredentialFields();
        }

        private void BtnTestarConexao_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildValidatedConnectionString(out _, out var connectionString))
            {
                return;
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                TxtStatus.Text = "Conexao realizada com sucesso. Pode salvar e entrar.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Falha ao conectar: {ex.Message}";
            }
        }

        private void BtnSalvarEntrar_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildValidatedConnectionString(out var settings, out var connectionString))
            {
                return;
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                _configurationService.SaveSettings(settings);
                ConnectionString = connectionString;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Nao foi possivel salvar porque a conexao falhou: {ex.Message}";
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
