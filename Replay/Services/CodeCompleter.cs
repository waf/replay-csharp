using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Provides code completion (i.e. Intellisense) using Roslyn
    /// </summary>
    class CodeCompleter
    {
        private Document document;

        public CodeCompleter(Document document)
        {
            this.document = document;
        }

        public async Task<ImmutableArray<CompletionItem>> Complete(string code)
        {
            string program = $"using System; namespace TestProject {{ public class CodeComplete {{ public void CodeCompleteMethod() {{ {code} }} }} }}";
            document = document.WithText(SourceText.From(program));
            int cursor = program.IndexOf(code) + code.Length;
            var service = CompletionService.GetService(document);
            var completions = await service.GetCompletionsAsync(document, cursor);
            var filtered = service.FilterItems(document, completions.Items, code);
            return filtered;
        }
    }
}
