using NuGet.Common;
using System.Threading.Tasks;

namespace Replay.Services
{
    internal class NugetLogger : ILogger
    {
        private IReplLogger logger;

        public NugetLogger(IReplLogger logger)
        {
            this.logger = logger;
        }

        public void Log(LogLevel level, string data)
        {
            switch (level)
            {
                case LogLevel.Debug: LogDebug(data); return;
                case LogLevel.Verbose: LogVerbose(data); return;
                case LogLevel.Information: LogInformation(data); return;
                case LogLevel.Minimal: LogMinimal(data); return;
                case LogLevel.Warning: LogWarning(data); return;
                case LogLevel.Error: LogError(data); return;
                default: return;
            }
        }

        public void Log(ILogMessage message) => Log(message.Level, message.Message);

        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public void LogDebug(string data) { }
        public void LogInformation(string data) => logger.LogOutput(data);
        public void LogInformationSummary(string data) => logger.LogOutput(data);
        public void LogMinimal(string data) { }
        public void LogVerbose(string data) { }
        public void LogWarning(string data) => logger.LogOutput(data);
        public void LogError(string data) => logger.LogError(data);
    }
}