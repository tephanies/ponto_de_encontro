using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Expecting args: <connectionString> <path-to-exe> [pollSeconds] [batchSize]
        string connectionString = args.Length >= 1 ? args[0] : string.Empty;
        string exePath = args.Length >= 2 ? args[1] : string.Empty;
        int pollSeconds = args.Length >= 3 && int.TryParse(args[2], out var p) ? p : 3;
        int batchSize = args.Length >= 4 && int.TryParse(args[3], out var b) ? b : 50;

        services.AddSingleton(new WorkerServiceOptions(connectionString, exePath, pollSeconds, batchSize));
        services.AddHostedService(sp => new WorkerService(sp.GetRequiredService<ILogger<WorkerService>>(), connectionString, exePath, pollSeconds, batchSize));
    });

var host = builder.Build();
await host.RunAsync();

public record WorkerServiceOptions(string ConnectionString, string ExePath, int PollSeconds, int BatchSize);
