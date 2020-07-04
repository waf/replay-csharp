using Replay.Services.Logging;
using Replay.ViewModel;
using System;

namespace Replay.Logging
{
    internal class Logger : IReplLogger
    {
        private readonly LineViewModel line;

        public Logger(LineViewModel line)
        {
            this.line = line;
        }

        public void LogOutput(string output) =>
            line.StandardOutput += output + Environment.NewLine;
        public void LogError(string error) =>
            line.Error += error + Environment.NewLine;
    }
}