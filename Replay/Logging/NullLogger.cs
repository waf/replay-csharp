using Replay.Services.Logging;

namespace Replay.Logging
{
    /// <summary>
    /// Used in the rare case where we don't want to show the user
    /// any output (e.g in initialization / warm-up logic)
    /// </summary>
    internal class NullLogger : IReplLogger
    {
        public void LogError(string error) { }
        public void LogOutput(string output) { }
    }
}