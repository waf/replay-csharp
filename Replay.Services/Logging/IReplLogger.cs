namespace Replay.Services.Logging
{
    /// <summary>
    /// A logger that logs to the REPL UI (user visible)
    /// </summary>
    public interface IReplLogger
    {
        void LogOutput(string output);
        void LogError(string error);
    }
}