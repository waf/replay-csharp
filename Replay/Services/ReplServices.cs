using Replay.Logging;
using Replay.Model;
using Replay.Services.AssemblyLoading;
using Replay.Services.CommandHandlers;
using Replay.Services.Nuget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Main access point for editor services.
    /// Manages background initialization for all services in a way that doesn't increase startup time.
    /// This is a stateful service (due to the contained WorkspaceManager) and is one-per-window.
    /// </summary>
    internal class ReplServices
    {
        private readonly Task initialization;
        private SyntaxHighlighter syntaxHighlighter;
        private ScriptEvaluator scriptEvaluator;
        private CodeCompleter codeCompleter;
        private WorkspaceManager workspaceManager;
        private IReadOnlyCollection<ICommandHandler> commandHandlers;

        public event EventHandler<UserConfiguration> UserConfigurationLoaded;

        public ReplServices() =>
            // some of the initialization can be heavy, and causes slow startup time for the UI.
            // run it in a background thread so the UI can render immediately.
            initialization = Task.Run(() =>
            {
                this.syntaxHighlighter = new SyntaxHighlighter("Themes/theme.vssettings");
                UserConfigurationLoaded?.Invoke(this, new UserConfiguration
                (
                    syntaxHighlighter.BackgroundColor,
                    syntaxHighlighter.ForegroundColor
                ));

                var io = new FileIO();
                var assemblies = new DefaultAssemblies(new DotNetAssemblyLocator(() => new Process(), io));
                this.codeCompleter = new CodeCompleter();
                this.scriptEvaluator = new ScriptEvaluator(assemblies);
                this.workspaceManager = new WorkspaceManager(assemblies);

                this.commandHandlers = new ICommandHandler[]
                {
                    new ExitCommandHandler(),
                    new HelpCommandHandler(),
                    new AssemblyReferenceCommandHandler(scriptEvaluator, workspaceManager, io),
                    new NugetReferenceCommandHandler(scriptEvaluator, workspaceManager, new NugetPackageInstaller(io)),
                    new EvaluationCommandHandler(scriptEvaluator, workspaceManager, new PrettyPrinter())
                };
            });

        public async Task<IReadOnlyList<ReplCompletion>> CompleteCodeAsync(int lineId, string code, int caretIndex)
        {
            await initialization;
            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, code);
            return await codeCompleter.Complete(submission, caretIndex);
        }

        public async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(int lineId, string code)
        {
            await initialization;
            var submission = workspaceManager.CreateOrUpdateSubmission(lineId, code);
            return await syntaxHighlighter.HighlightAsync(submission);
        }

        public async Task<LineEvaluationResult> EvaluateAsync(int lineId, string code, IReplLogger logger)
        {
            await initialization;
            try
            {
                return await commandHandlers
                    .First(handler => handler.CanHandle(code))
                    .HandleAsync(lineId, code, logger);
            }
            catch (Exception ex)
            {
                return new LineEvaluationResult(code, null, "Error: " + ex.Message, null);
            }
        }
    }
}
