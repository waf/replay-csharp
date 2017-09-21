using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Replay.Model;
using Replay.UI;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Main access point for editor services.
    /// Handles service initialization in a way that doesn't bloat startup time.
    /// </summary>
    public class ReplServices
    {
        const string InitializationCode = @"using System; Console.WriteLine(""Hello""); ""World""";
        Document document; //roslyn document, not avalonedit document.
        SyntaxHighlighter syntaxHighlighter;
        ScriptEvaluator scriptEvaluator;
        CodeCompleter codeCompleter;

        readonly Task RoslynInitialization;
        readonly Task SyntaxHighlightInitialization;
        readonly Task EvaluationInitialization;

        public ReplServices()
        {
            RoslynInitialization = Task.Run(() => RoslynInitialize());
            SyntaxHighlightInitialization = Task.Run(SyntaxHighlightInitialize);
            EvaluationInitialization = Task.Run(EvaluationInitialize);
        }

        private void RoslynInitialize()
        {
            MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
            MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);

            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
                .AddMetadataReference(CorlibReference)
                .AddMetadataReference(SystemCoreReference);
            document = project.AddDocument("CodeComplete.cs", "");
        }

        /// <summary>
        /// Initializes the required services, in a background thread.
        /// </summary>
        private async Task SyntaxHighlightInitialize()
        {
            await RoslynInitialization;

            syntaxHighlighter = new SyntaxHighlighter(document);
            // some of the roslyn infrastructure is slow on first run, so run a little test program through it first.
            syntaxHighlighter.Highlight(InitializationCode);
        }

        private async Task EvaluationInitialize()
        {
            await RoslynInitialization;

            scriptEvaluator = new ScriptEvaluator();
            codeCompleter = new CodeCompleter(document);
            await scriptEvaluator.Evaluate(InitializationCode);
            await codeCompleter.Complete(InitializationCode);
        }

        public async Task<ImmutableArray<CompletionItem>> CompleteCode(string code)
        {
            await EvaluationInitialization;
            return await codeCompleter.Complete(code);
        }

        public async Task<EvaluationResult> Evaluate(string text)
        {
            await EvaluationInitialization;
            return await scriptEvaluator.Evaluate(text);
        }

        public async Task ConfigureSyntaxHighlighting(TextEditor repl)
        {
            await SyntaxHighlightInitialization;
            repl.TextArea.TextView.LineTransformers
                .Add(new AvalonSyntaxHighlightTransformer(syntaxHighlighter));
        }
    }
}
