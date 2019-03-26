using Microsoft.CodeAnalysis.Completion;
using Replay.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Main access point for editor services.
    /// This is a stateful service (due to the contained WorkspaceManager) and is one-per-window.
    /// </summary>
    public class ReplServices
    {
        private readonly Task initialization;
        private readonly Task nugetInitialization;
        private SyntaxHighlighter syntaxHighlighter;
        private ScriptEvaluator scriptEvaluator;
        private NugetPackageResolver nugetResolver;
        private CodeCompleter codeCompleter;
        private WorkspaceManager workspaceManager;

        public event EventHandler<UserConfiguration> UserConfigurationLoaded;

        public ReplServices()
        {
            // some of the initialization can be heavy, and causes slow startup time for the UI.
            // run it in a background thread so the UI can render immediately.
            initialization = Task.Run(() =>
            {
                this.syntaxHighlighter = new SyntaxHighlighter("theme.vssettings");
                UserConfigurationLoaded?.Invoke(this, new UserConfiguration
                (
                    syntaxHighlighter.BackgroundColor,
                    syntaxHighlighter.ForegroundColor
                ));

                this.scriptEvaluator = new ScriptEvaluator();
                this.codeCompleter = new CodeCompleter();
                this.workspaceManager = new WorkspaceManager();
            });
            // nuget service has filesystem IO, so it takes a lot of time to resolve
            // run it separate from the other services so the other services aren't blocked.
            nugetInitialization = Task.Run(() =>
            {
                this.nugetResolver = new NugetPackageResolver();
            });
        }

        public async Task<ImmutableArray<CompletionItem>> CompleteCodeAsync(int lineId, string code)
        {
            await initialization;
            ReplSubmission replSubmission = workspaceManager.CreateOrUpdateSubmission(lineId, code);
            return await this.codeCompleter.Complete(replSubmission);
        }

        public async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(int lineId, string code)
        {
            await initialization;
            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, code);
            return await this.syntaxHighlighter.Highlight(submission);
        }

        public async Task<EvaluationResult> InstallNugetPackage(string name, string version = null, string source = null)
        {
            await initialization;
            await nugetInitialization;
            var nugetResult = this.nugetResolver.AddPackage(name, version, source);
            if(nugetResult.References.Any())
            {
                workspaceManager.AddReference(nugetResult.References);
                scriptEvaluator.AddReference(nugetResult.References);
            }
            return new EvaluationResult
            {
                StandardOutput = nugetResult.StandardOutput,
                Exception = nugetResult.Exception,
            };
        }

        public async Task<EvaluationResult> EvaluateAsync(int id, string text)
        {
            await initialization;

            if(text.StartsWith("nuget "))
            {
                string package = text.Split(' ')[1];
                var result = await this.InstallNugetPackage(package);
                _ = workspaceManager.CreateOrUpdateSubmission(id, string.Empty); // don't try to evaluate the text as C#
                return result;
            }

            // track the submission in our workspace. We won't use the
            // result because the Scripting API doesn't need it, but other
            // roslyn APIs like code completion and syntax highlighting will.
            _ = workspaceManager.CreateOrUpdateSubmission(id, text);

            return await scriptEvaluator.EvaluateAsync(text);
        }
    }
}
