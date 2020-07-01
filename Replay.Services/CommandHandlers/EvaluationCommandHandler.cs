using Replay.Services.Logging;
using Replay.Services.Model;
using System;
using System.Threading.Tasks;

namespace Replay.Services.CommandHandlers
{
    /// <summary>
    /// An <see cref="ICommandHandler" /> for evaluating code.
    /// This command handler runs last and handles anything that can't be handled by other command handlers.
    /// </summary>
    class EvaluationCommandHandler : ICommandHandler
    {
        private readonly ScriptEvaluator scriptEvaluator;
        private readonly WorkspaceManager workspaceManager;
        private readonly PrettyPrinter prettyPrinter;

        public EvaluationCommandHandler(ScriptEvaluator scriptEvaluator, WorkspaceManager workspaceManager, PrettyPrinter prettyPrinter)
        {
            this.scriptEvaluator = scriptEvaluator;
            this.workspaceManager = workspaceManager;
            this.prettyPrinter = prettyPrinter;
        }

        public bool CanHandle(string input) => true;

        public async Task<LineEvaluationResult> HandleAsync(Guid lineId, string text, IReplLogger logger)
        {
            // bail out if it's not a complete statement, but first try automatic completions
            var (success, newTree) = await scriptEvaluator.TryCompleteStatementAsync(text);
            if (!success)
            {
                return LineEvaluationResult.IncompleteInput;
            }
            text = (await newTree.GetRootAsync())
                .ToFullString();

            // track the submission in our workspace. We won't need the
            // result for script evaluation, but other roslyn APIs like
            // code completion and syntax highlighting will need it.
            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, text);
            var scriptResult = await scriptEvaluator.EvaluateAsync(text);
            var output = await prettyPrinter.FormatAsync(submission.Document, scriptResult);

            return output;
        }
    }
}
