using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
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
    internal class ReplServices
    {
        private readonly Task initialization;
        private SyntaxHighlighter syntaxHighlighter;
        private ScriptEvaluator scriptEvaluator;
        private CodeCompleter codeCompleter;
        private WorkspaceManager workspaceManager;
        private PrettyPrinter prettyPrinter;

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
                this.prettyPrinter = new PrettyPrinter();
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

        public async Task<LineOutput> EvaluateAsync(int id, string text)
        {
            await initialization;

            // track the submission in our workspace. We won't use the
            // result because the Scripting API doesn't need it, but other
            // roslyn APIs like code completion and syntax highlighting will.
            _ = workspaceManager.CreateOrUpdateSubmission(id, text);

            var scriptResult = await scriptEvaluator.EvaluateAsync(text);
            return prettyPrinter.Format(scriptResult);
        }
    }
}
