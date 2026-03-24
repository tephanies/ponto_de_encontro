using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using PontoDeEncontro.Models;
using PontoDeEncontro.Services;

namespace PontoDeEncontro
{
    public partial class ConnectionSettingsWindow : Window
    {
        private readonly AppConfigurationService _configService;
        private bool _initializing = true;

        public ConnectionSettingsWindow(AppConfigurationService configService)
        {
            InitializeComponent();
            _configService = configService;
            MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
            LoadCurrentSettings();
            _initializing = false;
        }

        public string? ConnectionString { get; private set; }

        private void LoadCurrentSettings()
        {
            var settings = _configService.LoadSettings();

            TxtServer.Text = settings.Server;
            TxtUser.Text = settings.UserId;
            TxtPassword.Password = settings.Password;

            // Tenta carregar lista de bancos se o servidor esta preenchido
            if (!string.IsNullOrWhiteSpace(settings.Server))
            {
                TryLoadDatabases(settings, settings.Database);
            }

            // Garante que o nome do banco apareca mesmo se a lista nao carregou
            if (CmbDatabase.SelectedItem == null && !string.IsNullOrWhiteSpace(settings.Database))
            {
                CmbDatabase.Text = settings.Database;
            }
        }

        private DatabaseSettings GetSettingsFromForm()
        {
            return new DatabaseSettings
            {
                Server = TxtServer.Text.Trim(),
                Database = GetSelectedDatabase(),
                UserId = TxtUser.Text.Trim(),
                Password = TxtPassword.Password,
                UseIntegratedSecurity = false
            };
        }

        private string GetSelectedDatabase()
        {
            // Prioriza item selecionado, depois texto digitado
            return (CmbDatabase.SelectedItem?.ToString() ?? CmbDatabase.Text ?? string.Empty).Trim();
        }

        private void TryLoadDatabases(DatabaseSettings serverSettings, string? preselect = null)
        {
            try
            {
                _configService.ValidateServerSettings(serverSettings);
                List<string> databases = _configService.GetAvailableDatabases(serverSettings);
                CmbDatabase.ItemsSource = databases;

                if (!string.IsNullOrWhiteSpace(preselect) && databases.Contains(preselect))
                    CmbDatabase.SelectedItem = preselect;
                else if (!string.IsNullOrWhiteSpace(preselect))
                    CmbDatabase.Text = preselect;
                else if (databases.Count > 0)
                    CmbDatabase.SelectedIndex = 0;

                TxtStatus.Text = databases.Count > 0
                    ? "Bancos carregados com sucesso. Selecione o banco desejado."
                    : "Nenhum banco de dados de usuario foi encontrado nesse servidor.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Erro ao carregar bancos: {ex.Message}";
            }
        }

        // ==================== Eventos ====================

        private void ConnectionField_Changed(object sender, RoutedEventArgs e)
        {
            if (!_initializing) ClearDatabaseList();
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_initializing) ClearDatabaseList();
        }

        private void ClearDatabaseList()
        {
            string currentDb = GetSelectedDatabase();
            CmbDatabase.ItemsSource = null;
            CmbDatabase.Text = currentDb;
        }

        private void BtnCarregarBancos_Click(object sender, RoutedEventArgs e)
        {
            var serverSettings = new DatabaseSettings
            {
                Server = TxtServer.Text.Trim(),
                UserId = TxtUser.Text.Trim(),
                Password = TxtPassword.Password,
                UseIntegratedSecurity = false
            };
            TryLoadDatabases(serverSettings, GetSelectedDatabase());
        }

        private void BtnTestarConexao_Click(object sender, RoutedEventArgs e)
        {
            var settings = GetSettingsFromForm();

            if (!ValidateAndShowError(settings))
                return;

            try
            {
                var connStr = _configService.BuildConnectionString(settings);
                using var conn = new SqlConnection(connStr);
                conn.Open();
                TxtStatus.Text = "Conexao realizada com sucesso. Pode salvar e entrar.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Falha ao conectar: {ex.Message}";
            }
        }

        private void BtnSalvarEntrar_Click(object sender, RoutedEventArgs e)
        {
            var settings = GetSettingsFromForm();

            if (!ValidateAndShowError(settings))
                return;

            try
            {
                var connStr = _configService.BuildConnectionString(settings);
                using var conn = new SqlConnection(connStr);
                conn.Open();

                _configService.SaveSettings(settings);
                ConnectionString = connStr;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Nao foi possivel salvar porque a conexao falhou: {ex.Message}";
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool ValidateAndShowError(DatabaseSettings settings)
        {
            try
            {
                _configService.ValidateSettings(settings);
                return true;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = ex.Message;
                return false;
            }
        }
    }
}
