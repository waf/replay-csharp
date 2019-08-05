using Replay.Model;
using System.Threading.Tasks;

namespace Replay.Services.CommandHandlers
{
    /// <summary>
    /// A handler that can handle a line of input from the REPL
    /// </summary>
    interface ICommandHandler
    {
        bool CanHandle(string input);
        Task<LineEvaluationResult> HandleAsync(int lineId, string text, IReplLogger logger);
    }
}
