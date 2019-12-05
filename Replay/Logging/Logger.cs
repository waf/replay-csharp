using Replay.Model;
using System;

namespace Replay.Logging
{
    internal class Logger : IReplLogger
    {
        private LineEditorViewModel line;

        public Logger(LineEditorViewModel line)
        {
            this.line = line;
        }

        public void LogOutput(string output) =>
            line.StandardOutput += output + Environment.NewLine;
        public void LogError(string error) =>
            line.Error += error + Environment.NewLine;
    }
}