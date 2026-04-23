using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Program
{
    static string? ConnectionString;
    static string? ExePath;
    static int PollSeconds = 3;
    static int BatchSize = 50;

    static void Main(string[] args)
    {
        if (args.Length >= 2)
        {
            ConnectionString = args[0];
            ExePath = args[1];
        }
        else
        {
            Console.WriteLine("Usage: Monitor <connectionString> <path-to-exe> [pollSeconds] [batchSize]");
            return;
        }

        if (args.Length >= 3 && int.TryParse(args[2], out var p)) PollSeconds = p;
        if (args.Length >= 4 && int.TryParse(args[3], out var b)) BatchSize = b;

        Console.WriteLine($"Monitor started. Polling every {PollSeconds}s. Exe: {ExePath}");

        var cancel = false;
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cancel = true; };

        while (!cancel)
        {
            try
            {
                var ids = ClaimPendingEvents();
                if (ids.Count > 0)
                {
                    Console.WriteLine($"Claimed {ids.Count} events.");
                    TryLaunchOnce(ExePath!);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }

            for (int i = 0; i < PollSeconds && !cancel; i++) Thread.Sleep(1000);
        }

        Console.WriteLine("Monitor stopping.");
    }

    static List<int> ClaimPendingEvents()
    {
        var ids = new List<int>();
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();

        var sql = $@"
WITH cte AS (
    SELECT TOP ({BatchSize}) Id FROM dbo.PontoEncontroEvents WHERE Processed = 0 ORDER BY CreatedAt
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

    static void TryLaunchOnce(string exePath)
    {
        try
        {
            var name = Path.GetFileNameWithoutExtension(exePath);
            var running = Process.GetProcessesByName(name);
            if (running.Length > 0)
            {
                Console.WriteLine($"Process {name} already running (instances={running.Length}). Skipping launch.");
                return;
            }

            var psi = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
            Console.WriteLine($"Started {exePath}.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to start {exePath}: {ex.Message}");
        }
    }
}
