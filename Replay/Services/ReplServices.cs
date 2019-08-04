using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Replay.Model;
using Replay.Services.CommandHandlers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private ICommandHandler[] commandHandlers;

        public event EventHandler<UserConfiguration> UserConfigurationLoaded;

        public ReplServices()
        {
            // some of the initialization can be heavy, and causes slow startup time for the UI.
            // run it in a background thread so the UI can render immediately.
            initialization = Task.Run(() =>
            {
                this.syntaxHighlighter = new SyntaxHighlighter("Themes/dracula.vssettings");
                UserConfigurationLoaded?.Invoke(this, new UserConfiguration
                (
                    syntaxHighlighter.BackgroundColor,
                    syntaxHighlighter.ForegroundColor
                ));

                this.scriptEvaluator = new ScriptEvaluator();
                this.codeCompleter = new CodeCompleter();
                this.workspaceManager = new WorkspaceManager();

                this.commandHandlers = new ICommandHandler[]
                {
                    new AssemblyReferenceCommand(scriptEvaluator, workspaceManager),
                    new NugetReferenceCommandHandler(scriptEvaluator, workspaceManager, new NugetPackageInstaller()),
                    new EvaluationCommandHandler(scriptEvaluator, workspaceManager, new PrettyPrinter())
                };
            });
        }

        public async Task<IReadOnlyList<ReplCompletion>> CompleteCodeAsync(int lineId, string code, int caretIndex)
        {
            await initialization;
            ReplSubmission replSubmission = await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, code);
            return await this.codeCompleter.Complete(replSubmission, caretIndex);
        }

        public async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(int lineId, string code)
        {
            await initialization;
            var submission = await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, code);
            return await this.syntaxHighlighter.HighlightAsync(submission);
        }

        public async Task<LineEvaluationResult> EvaluateAsync(int id, string text, IReplLogger logger)
        {
            await initialization;
            return await commandHandlers
                .First(handler => handler.CanHandle(text))
                .HandleAsync(id, text, logger);
        }
    }
}
