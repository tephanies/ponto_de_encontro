using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PontoDeEncontro.Models;

namespace PontoDeEncontro.Services
{
    public class AppConfigurationService
    {
        private const string UserSettingsFileName = "appsettings.user.json";
        private readonly string _basePath;

        public AppConfigurationService(string? basePath = null)
        {
            _basePath = basePath ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        public string UserSettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PontoDeEncontro");

        public string UserSettingsPath => Path.Combine(UserSettingsDirectory, UserSettingsFileName);

        public DatabaseSettings LoadSettings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(_basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile(UserSettingsPath, optional: true, reloadOnChange: false)
                .Build();

            return configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? new DatabaseSettings();
        }

        public string BuildConnectionString(DatabaseSettings settings)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = settings.Server,
                InitialCatalog = settings.Database,
                IntegratedSecurity = settings.UseIntegratedSecurity,
                TrustServerCertificate = true,
                ConnectTimeout = 5
            };

            if (!settings.UseIntegratedSecurity)
            {
                builder.UserID = settings.UserId;
                builder.Password = settings.Password;
            }

            return builder.ConnectionString;
        }

        public void SaveSettings(DatabaseSettings settings)
        {
            Directory.CreateDirectory(UserSettingsDirectory);

            var payload = new
            {
                DatabaseSettings = settings
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(UserSettingsPath, json);
        }

        public void ValidateSettings(DatabaseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Server))
            {
                throw new InvalidOperationException("Informe o servidor SQL.");
            }

            if (string.IsNullOrWhiteSpace(settings.Database))
            {
                throw new InvalidOperationException("Informe o nome do banco de dados.");
            }

            if (!settings.UseIntegratedSecurity && string.IsNullOrWhiteSpace(settings.UserId))
            {
                throw new InvalidOperationException("Informe o usuário do banco de dados.");
            }
        }
    }
}
