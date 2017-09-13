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
    class CodeCompleter
    {
        private Document document;
        private string template;
        private AdhocWorkspace workspace;
        private Project project;

        public CodeCompleter()
        {
            //Workspace workspace = new AdhocWorkspace();
            //Solution solution = workspace.CurrentSolution;
            //Project project = solution.AddProject("ReplCodeComplete", "ReplCodeCompleteAssembly", LanguageNames.CSharp);
            //document = project.AddDocument("Complete.cs", "");
        }

        public async Task<ImmutableArray<CompletionItem>> Complete(string code)
        {
            string program = $"using System; namespace TestProject {{ public class CodeComplete {{ public void CodeCompleteMethod() {{ {code} }} }} }}";
            workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId("TestProject");
            project = workspace.AddProject("TestProject", LanguageNames.CSharp)
                .AddMetadataReference(CorlibReference)
                .AddMetadataReference(SystemCoreReference);
            document = project.AddDocument("CodeComplete.cs", program);
            int cursor = program.IndexOf(code) + code.Length;
            var service = CompletionService.GetService(document);
            var completions = await service.GetCompletionsAsync(document, cursor);
            var filtered = service.FilterItems(document, completions.Items, code);
            return filtered;
        }

        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        
    }
}
