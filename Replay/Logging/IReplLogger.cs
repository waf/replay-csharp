namespace Replay.Logging
{
    /// <summary>
    /// A logger that logs to the REPL UI (user visible)
    /// </summary>
    internal interface IReplLogger
    {
        void LogOutput(string output);
        void LogError(string error);
    }
}