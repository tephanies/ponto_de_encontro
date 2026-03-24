using System;
using System.IO;

namespace PontoDeEncontro.Services
{
    public static class LogService
    {
        private static readonly string LogDirectory;

        static LogService()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            LogDirectory = Path.Combine(exeDir, "logs");
            Directory.CreateDirectory(LogDirectory);
        }

        public static void LogError(string context, Exception ex)
        {
            try
            {
                var fileName = $"error_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(LogDirectory, fileName);
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(filePath, entry);
            }
            catch
            {
                // Não falhar se o log falhar
            }
        }

        public static void LogInfo(string context, string message)
        {
            try
            {
                var fileName = $"error_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(LogDirectory, fileName);
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] INFO: {message}{Environment.NewLine}";
                File.AppendAllText(filePath, entry);
            }
            catch
            {
                // Não falhar se o log falhar
            }
        }
    }
}
