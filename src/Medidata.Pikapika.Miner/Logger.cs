using NuGet.Common;
using System;
using System.Threading.Tasks;

namespace Medidata.Pikapika.Miner
{
    public class Logger : ILogger
    {
        public void LogDebug(string data) => Console.WriteLine($"DEBUG: {data}");
        public void LogVerbose(string data) => Console.WriteLine($"VERBOSE: {data}");
        public void LogInformation(string data) => Console.WriteLine($"INFORMATION: {data}");
        public void LogMinimal(string data) => Console.WriteLine($"MINIMAL: {data}");
        public void LogWarning(string data) => Console.WriteLine($"WARNING: {data}");
        public void LogError(string data) => Console.WriteLine($"ERROR: {data}");
        public void LogSummary(string data) => Console.WriteLine($"SUMMARY: {data}");

        public void LogInformationSummary(string data) => Console.WriteLine($"INFORMATION_SUMMARY: {data}");

        public void LogErrorSummary(string data) => Console.WriteLine($"ERROR_SUMMARY: {data}");

        public void Log(LogLevel level, string data) => Console.WriteLine($"{level.ToString()?.ToUpper()}_SUMMARY: {data}");

        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public void Log(ILogMessage message) => Console.WriteLine($"{message?.Level.ToString()?.ToUpper()}_SUMMARY: {message?.Message}");

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}
