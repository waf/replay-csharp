using Replay.Services.Logging;
using Replay.Services.Model;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Replay.Services.CommandHandlers
{
    /// <summary>
    /// Exits the application
    /// </summary>
    class ExitCommandHandler : ICommandHandler
    {
        public bool CanHandle(string input) => "exit".Equals(input, StringComparison.OrdinalIgnoreCase);

        public Task<LineEvaluationResult> HandleAsync(int lineId, string text, IReplLogger logger)
        {
            // this line exits the application. The subsequent lines don't run.
            Application.Current.Shutdown();

            return Task.FromResult(LineEvaluationResult.NoOutput);
        }
    }
}
