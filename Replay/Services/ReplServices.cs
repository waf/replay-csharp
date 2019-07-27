using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Replay.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            ReplSubmission replSubmission = await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, code);
            return await this.codeCompleter.Complete(replSubmission);
        }

        public async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(int lineId, string code)
        {
            await initialization;
            var submission = await workspaceManager.CreateOrUpdateSubmissionAsync(lineId, code);
            return await this.syntaxHighlighter.HighlightAsync(submission);
        }

        public async Task<(bool IsComplete, FormattedLine Output)> EvaluateAsync(int id, string text)
        {
            await initialization;

            // bail out if it's not a complete statement, but first try automatic completions
            var (success, newTree) = await scriptEvaluator.TryCompleteStatementAsync(text);
            if(!success)
            {
                return (false, null);
            }
            text = (await newTree.GetRootAsync())
                .NormalizeWhitespace()
                .ToFullString();

            // track the submission in our workspace. We won't use the
            // result because the Scripting API doesn't need it, but other
            // roslyn APIs like code completion and syntax highlighting will.
            var submission = await workspaceManager.CreateOrUpdateSubmissionAsync(id, text);
            var scriptResult = await scriptEvaluator.EvaluateAsync(text);
            var output = await prettyPrinter.FormatAsync(submission.Document, scriptResult);

            return (true, output);
        }
    }
}
