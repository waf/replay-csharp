using Replay.Services.AssemblyLoading;
using Replay.Services.Logging;
using Replay.Services.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IFileIO io;
        private const string CommandPrefix = "#r ";

        public AssemblyReferenceCommandHandler(ScriptEvaluator scriptEvaluator, WorkspaceManager workspaceManager, IFileIO io)
        {
            this.scriptEvaluator = scriptEvaluator;
            this.workspaceManager = workspaceManager;
            this.io = io;
        }

        public bool CanHandle(string input) => input.StartsWith(CommandPrefix);

        public async Task<LineEvaluationResult> HandleAsync(Guid lineId, string input, IReplLogger logger)
        {
            string assemblyFile = input.Substring(CommandPrefix.Length).Trim('"');
            var assemblies = DotNetAssemblyLocator
                .GroupDirectoryContentsIntoAssemblies(ReadAssembly(assemblyFile))
                .Select(assembly => io.CreateMetadataReferenceWithDocumentation(assembly));

            foreach (var assembly in assemblies)
            {
                logger.LogOutput("Referencing " + assembly.Display);
                await scriptEvaluator.AddReferences(assembly);
                workspaceManager.CreateOrUpdateSubmission(lineId, assemblyReferences: assembly);
                logger.LogOutput("Assembly successfully referenced");
            }
            return LineEvaluationResult.NoOutput;
        }

        private IEnumerable<string> ReadAssembly(string assembly)
        {
            yield return assembly;

            var documentation = assembly.Replace(".dll", ".xml", StringComparison.OrdinalIgnoreCase);
            if (io.DoesFileExist(documentation))
            {
                yield return documentation;
            }
        }
    }
}
