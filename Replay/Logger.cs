using Replay.Model;
using System;

namespace Replay
{
    internal interface IReplLogger
    {
        void LogOutput(string output);
        void LogError(string error);
    }

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

    internal class NullLogger : IReplLogger
    {
        public void LogError(string error) { }
        public void LogOutput(string output) { }
    }
}