using Microsoft.CodeAnalysis.Completion;
using Replay.Model;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Provides code completion (i.e. Intellisense) using Roslyn
    /// </summary>
    class CodeCompleter
    {
        public async Task<ImmutableArray<CompletionItem>> Complete(ReplSubmission submission)
        {
            var service = CompletionService.GetService(submission.Document);
            var completions = await service.GetCompletionsAsync(submission.Document, submission.Code.Length);
            if(completions?.Items == null)
            {
                return ImmutableArray.Create<CompletionItem>();
            }
            return completions.Items;
        }
    }
}
