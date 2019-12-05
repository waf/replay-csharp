using Microsoft.CodeAnalysis;
using Replay.Logging;
using Replay.Model;
using System.Threading.Tasks;

namespace Replay.Services.CommandHandlers
{
    /// <summary>
    /// An <see cref="ICommandHandler" /> for referencing assemblies.
    /// Handles lines like "#r path/to/my.dll"
    /// </summary>
    class AssemblyReferenceCommandHandler : ICommandHandler
    {
        private readonly ScriptEvaluator scriptEvaluator;
        private readonly WorkspaceManager workspaceManager;
        private const string CommandPrefix = "#r ";

        public AssemblyReferenceCommandHandler(ScriptEvaluator scriptEvaluator, WorkspaceManager workspaceManager)
        {
            this.scriptEvaluator = scriptEvaluator;
            this.workspaceManager = workspaceManager;
        }

        public bool CanHandle(string input) => input.StartsWith(CommandPrefix);

        public async Task<LineEvaluationResult> HandleAsync(int lineId, string input, IReplLogger logger)
        {
            string assembly = input.Substring(CommandPrefix.Length).Trim('"');
            var reference = MetadataReference.CreateFromFile(assembly);
            logger.LogOutput("Referencing " + reference.Display);
            await scriptEvaluator.AddReferences(reference);
            await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, string.Empty, reference);
            logger.LogOutput("Assembly successfully referenced");
            return LineEvaluationResult.NoOutput;
        }
    }
}
