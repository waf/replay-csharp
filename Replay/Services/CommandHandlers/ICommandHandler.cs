using Replay.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Replay.Services.CommandHandlers
{
    interface ICommandHandler
    {
        bool CanHandle(string input);
        Task<LineEvaluationResult> HandleAsync(int lineId, string text, IReplLogger logger);
    }
}
