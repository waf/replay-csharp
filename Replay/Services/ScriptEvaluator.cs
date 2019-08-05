using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Replay.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Evaluates C# code using the Roslyn Scripting API
    /// </summary>
    public class ScriptEvaluator
    {
        private ScriptOptions compilationOptions;
        private ScriptState<object> state;
        public readonly CSharpParseOptions parseOptions;

        public ScriptEvaluator()
        {
            this.compilationOptions = ScriptOptions.Default
                .WithReferences(DefaultAssemblies.Assemblies.Value)
                .WithImports(DefaultAssemblies.DefaultUsings);

            this.parseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        }

        public async Task<(bool Success, SyntaxTree NewTree)> TryCompleteStatementAsync(string text)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(text.ToString(), parseOptions);
            if (SyntaxFactory.IsCompleteSubmission(syntaxTree))
            {
                return (true, syntaxTree);
            }
            return await TryWithSemicolon(syntaxTree);
        }

        private async Task<(bool Success, SyntaxTree NewTree)> TryWithSemicolon(SyntaxTree syntaxTree)
        {
            var root = await syntaxTree.GetRootAsync();
            var nodes = root.ChildNodes().ToList();
            if (nodes.Any()
                && nodes.First() is FieldDeclarationSyntax declaration
                && declaration.SemicolonToken.IsMissing)
            {
                var withSemicolon = declaration.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                var newTree = syntaxTree.WithRootAndOptions(
                    root.ReplaceNode(declaration, withSemicolon),
                    parseOptions
                );
                if (SyntaxFactory.IsCompleteSubmission(newTree))
                {
                    return (true, newTree);
                }
            }
            return (false, syntaxTree);
        }

        /// <summary>
        /// Run the script and return the result, capturing any exceptions or standard output.
        /// </summary>
        public async Task<ScriptEvaluationResult> EvaluateAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new ScriptEvaluationResult();
            }

            using (var stdout = new ConsoleOutputWriter())
            {
                var evaluated = await EvaluateCapturingError(text);
                return new ScriptEvaluationResult
                {
                    ScriptResult = evaluated.Result,
                    Exception = evaluated.Exception,
                    StandardOutput = stdout.GetOutputOrNull()
                };
            }
        }

        public async Task AddReferences(params MetadataReference[] assemblies)
        {
            compilationOptions = compilationOptions.AddReferences(assemblies);
            state = await state.ContinueWithAsync(null, compilationOptions);
        }

        private async Task<(ScriptState<object> Result, Exception Exception)> EvaluateCapturingError(string text)
        {
            try
            {
                state = state == null
                    ? await CSharpScript.Create(text, compilationOptions).RunAsync()
                    : await state.ContinueWithAsync(text);

                return (state, state.Exception);
            }
            catch (Exception exception)
            {
                return (null, exception);
            }
        }
    }
}
