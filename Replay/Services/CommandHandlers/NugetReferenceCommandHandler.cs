using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Replay.Model;

namespace Replay.Services.CommandHandlers
{
    class NugetReferenceCommandHandler : ICommandHandler
    {
        private readonly ScriptEvaluator scriptEvaluator;
        private readonly WorkspaceManager workspaceManager;
        private readonly NugetPackageInstaller nugetInstaller;

        public NugetReferenceCommandHandler(ScriptEvaluator scriptEvaluator, WorkspaceManager workspaceManager, NugetPackageInstaller nugetInstaller)
        {
            this.scriptEvaluator = scriptEvaluator;
            this.workspaceManager = workspaceManager;
            this.nugetInstaller = nugetInstaller;
        }

        public bool CanHandle(string input) => input.StartsWith("#nuget ");

        public async Task<LineEvaluationResult> HandleAsync(int lineId, string text, IReplLogger logger)
        {
            string package = text.Substring(7).Trim('"');
            logger.LogOutput("Adding NuGet package " + package);
            var assemblies = (await nugetInstaller.Install(package, logger)).ToArray();
            await scriptEvaluator.AddReferences(assemblies);
            await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, string.Empty, assemblies);
            logger.LogOutput("Added NuGet package successfully");
            return LineEvaluationResult.NoOutput;
        }
    }
}
