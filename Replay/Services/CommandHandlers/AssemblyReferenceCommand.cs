using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Replay.Model;

namespace Replay.Services.CommandHandlers
{
    class AssemblyReferenceCommand : ICommandHandler
    {
        private readonly ScriptEvaluator scriptEvaluator;
        private readonly WorkspaceManager workspaceManager;

        public AssemblyReferenceCommand(ScriptEvaluator scriptEvaluator, WorkspaceManager workspaceManager)
        {
            this.scriptEvaluator = scriptEvaluator;
            this.workspaceManager = workspaceManager;
        }

        public bool CanHandle(string input) => input.StartsWith("#r ");

        public async Task<LineEvaluationResult> HandleAsync(int lineId, string input, IReplLogger logger)
        {
            string assembly = input.Substring(3).Trim('"');
            var reference = MetadataReference.CreateFromFile(assembly);
            logger.LogOutput("Referencing " + reference.Display);
            await scriptEvaluator.AddReferences(reference);
            await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, string.Empty, reference);
            logger.LogOutput("Assembly successfully referenced");
            return LineEvaluationResult.NoOutput;
        }
    }
}
