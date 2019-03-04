using Microsoft.CodeAnalysis.Completion;
using Replay.Model;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private SyntaxHighlighter syntaxHighlighter;
        private ScriptEvaluator scriptEvaluator;
        private DotNetCommandEvaluator dotnetEvaluator;
        private CodeCompleter codeCompleter;
        private WorkspaceManager workspaceManager;

        public ReplServices()
        {
            // some of the initialization can be heavy, and causes slow startup time for the UI.
            // run it in a background thread so the UI can render immediately.
            initialization = Task.Run(() =>
            {
                this.dotnetEvaluator = new DotNetCommandEvaluator();
                string workingDirectory = this.dotnetEvaluator.CreateWorkingDirectory();

                this.syntaxHighlighter = new SyntaxHighlighter();
                this.scriptEvaluator = new ScriptEvaluator();
                this.codeCompleter = new CodeCompleter();
                this.workspaceManager = new WorkspaceManager();
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

        public async Task<EvaluationResult> EvaluateAsync(int id, string text)
        {
            await initialization;

            if(text.StartsWith("dotnet "))
            {
                var result = await dotnetEvaluator.EvaluateAsync(text);
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
