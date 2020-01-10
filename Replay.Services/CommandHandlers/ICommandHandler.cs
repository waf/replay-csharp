using Replay.Services.Logging;
using Replay.Services.Model;
using System;
using System.Threading.Tasks;

namespace Replay.Services.CommandHandlers
{
    /// <summary>
    /// A handler that can handle a line of input from the REPL
    /// </summary>
    interface ICommandHandler
    {
        bool CanHandle(string input);
        Task<LineEvaluationResult> HandleAsync(Guid lineId, string text, IReplLogger logger);
    }
}
