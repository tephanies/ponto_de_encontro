using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PontoDeEncontro.Models;

namespace PontoDeEncontro.Services
{
    public class AppConfigurationService
    {
        private readonly string _basePath;
        private string AppSettingsPath => Path.Combine(_basePath, "appsettings.json");

        public AppConfigurationService(string? basePath = null)
        {
            _basePath = basePath ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        public DatabaseSettings LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(_basePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            return config.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? new DatabaseSettings();
        }

        public void SaveSettings(DatabaseSettings settings)
        {
            var json = JsonSerializer.Serialize(
                new { DatabaseSettings = settings },
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(AppSettingsPath, json);
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

        public List<string> GetAvailableDatabases(DatabaseSettings serverSettings)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverSettings.Server,
                InitialCatalog = "master",
                IntegratedSecurity = serverSettings.UseIntegratedSecurity,
                TrustServerCertificate = true,
                ConnectTimeout = 5
            };

            if (!serverSettings.UseIntegratedSecurity)
            {
                builder.UserID = serverSettings.UserId;
                builder.Password = serverSettings.Password;
            }

            var databases = new List<string>();
            const string sql = @"
                SELECT name FROM sys.databases
                WHERE name NOT IN ('master','tempdb','model','msdb')
                ORDER BY name";

            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                databases.Add(reader.GetString(0));

            return databases;
        }

        public void ValidateSettings(DatabaseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Server))
                throw new InvalidOperationException("Informe o servidor SQL.");

            if (!settings.UseIntegratedSecurity && string.IsNullOrWhiteSpace(settings.UserId))
                throw new InvalidOperationException("Informe o usuario do banco de dados.");

            if (string.IsNullOrWhiteSpace(settings.Database))
                throw new InvalidOperationException("Informe o nome do banco de dados.");
        }

        public void ValidateServerSettings(DatabaseSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Server))
                throw new InvalidOperationException("Informe o servidor SQL.");

            if (!settings.UseIntegratedSecurity && string.IsNullOrWhiteSpace(settings.UserId))
                throw new InvalidOperationException("Informe o usuario do banco de dados.");
        }

        public bool TryGetSavedConnectionString(out string connectionString)
        {
            connectionString = string.Empty;

            try
            {
                var settings = LoadSettings();
                ValidateSettings(settings);
                connectionString = BuildConnectionString(settings);

                using var conn = new SqlConnection(connectionString);
                conn.Open();
                return true;
            }
            catch
            {
                connectionString = string.Empty;
                return false;
            }
        }
    }
}
