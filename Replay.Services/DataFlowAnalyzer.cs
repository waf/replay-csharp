using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Replay.Services.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    public class DataFlowAnalyzer
    {
        private const string UndeclaredIdentifier = "CS0103";

        public async Task<IReadOnlyCollection<string>> GetUnboundVariables(ReplSubmission submission)
        {
            // originally tried using the dataflow roslyn APIs, but it was not accurate for small
            // snippets of text. This approach, using declaration diagnostics, is more accurate.

            var model = await submission.Document.GetSemanticModelAsync();
            var diags = model.GetDeclarationDiagnostics();

            var tree = await submission.Document.GetSyntaxRootAsync();
            return model.GetDiagnostics()
                .Where(diag => diag.Id == UndeclaredIdentifier)
                .Select(diag => LookupProblemNode(tree, diag))
                .OfType<IdentifierNameSyntax>()
                .Where(node => !(node.Parent is InvocationExpressionSyntax)) // filter out unknown functions
                .Select(node => node.Identifier.ValueText)
                .Distinct()
                .ToList();
        }

        private static SyntaxNode LookupProblemNode(SyntaxNode tree, Diagnostic diag)
        {
            var span = diag.Location.SourceSpan;
            var node = tree.FindNode(span);
            return node;
        }
    }
}
