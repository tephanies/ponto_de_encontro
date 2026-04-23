using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly string _connectionString;
    private readonly string _exePath;
    private readonly int _pollSeconds;
    private readonly int _batchSize;

    public WorkerService(ILogger<WorkerService> logger, string connectionString, string exePath, int pollSeconds = 3, int batchSize = 50)
    {
        _logger = logger;
        _connectionString = connectionString;
        _exePath = exePath;
        _pollSeconds = pollSeconds;
        _batchSize = batchSize;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkerService starting. Exe: {exe}", _exePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ids = ClaimPendingEvents();
                if (ids.Count > 0)
                {
                    _logger.LogInformation("Claimed {count} events.", ids.Count);
                    TryLaunchOnce(_exePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollSeconds), stoppingToken);
        }
    }

    private List<int> ClaimPendingEvents()
    {
        var ids = new List<int>();
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        var sql = $@"
WITH cte AS (
    SELECT TOP ({_batchSize}) Id FROM dbo.PontoEncontroEvents WHERE Processed = 0 ORDER BY CreatedAt
)
UPDATE dbo.PontoEncontroEvents
SET Processed = 1, ProcessedAt = SYSUTCDATETIME()
OUTPUT inserted.Id
WHERE Id IN (SELECT Id FROM cte);";

        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        return ids;
    }

    private void TryLaunchOnce(string exePath)
    {
        try
        {
            var name = Path.GetFileNameWithoutExtension(exePath);
            var running = Process.GetProcessesByName(name);
            if (running.Length > 0)
            {
                _logger.LogInformation("Process {name} already running ({count}). Skipping.", name, running.Length);
                return;
            }

            var psi = new ProcessStartInfo(exePath) { UseShellExecute = true };
            Process.Start(psi);
            _logger.LogInformation("Started {exePath}.", exePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start {exePath}", exePath);
        }
    }
}
