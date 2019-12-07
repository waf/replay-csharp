using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Replay.Services.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Provides code completion (i.e. Intellisense) using Roslyn
    /// </summary>
    class CodeCompleter
    {
        public async Task<IReadOnlyList<ReplCompletion>> Complete(ReplSubmission submission, int caretIndex)
        {
            var service = CompletionService.GetService(submission.Document);
            var completions = await service.GetCompletionsAsync(submission.Document, caretIndex);

            if (completions?.Items == null)
            {
                return ImmutableArray.Create<ReplCompletion>();
            }
            return completions.Items
                .Select(item => new ReplCompletion(
                    submission,
                    item,
                    new Lazy<Task<string>>(() => GetQuickInfo(submission.Document, item))
                ))
                .ToList();
        }

        public async Task<string> GetQuickInfo(Document document, CompletionItem completion)
        {
            var infoService = QuickInfoService.GetService(document);
            string text = (await document.GetTextAsync()).ToString();
            string completedText = text.Substring(0, completion.Span.Start)
                + completion.DisplayText
                + (completion.Span.End == text.Length ? "" : text.Substring(completion.Span.End));
            var newDoc = document.WithText(SourceText.From(completedText));
            var info = await infoService.GetQuickInfoAsync(newDoc, completedText.Length - 1);

            if (info == null || info.Sections.Length == 0)
            {
                return null;
            }
            return string.Join(
                Environment.NewLine,
                info.Sections.Select(section => section.Text)
            );
        }
    }
    public class ReplCompletion
    {
        public ReplCompletion(ReplSubmission replSubmission, CompletionItem completionItem, Lazy<Task<string>> quickInfoTask)
        {
            ReplSubmission = replSubmission;
            CompletionItem = completionItem;
            QuickInfoTask = quickInfoTask;
        }

        public ReplSubmission ReplSubmission { get; }
        public CompletionItem CompletionItem { get; }
        public Lazy<Task<string>> QuickInfoTask { get; }
    }
}
