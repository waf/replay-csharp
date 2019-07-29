using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace Replay.UI
{
    class IntellisenseWindow : CompletionWindow
    {
        public IntellisenseWindow(TextArea textArea, ImmutableArray<CompletionItem> completions)
            : base(textArea)
        {
            if (completions.Length == 0) return;

            TextSpan span = completions[0].Span;
            string textBeingCompleted = textArea.Document.Text.Substring(span.Start, span.Length);

            PopulateCompletionDropDown(completions, textBeingCompleted);
            SetCompletionBounds(textBeingCompleted, span);
            Show();
        }

        private void PopulateCompletionDropDown(ImmutableArray<CompletionItem> completions, string textBeingCompleted)
        {
            int maxLength = 0;
            foreach (var completion in completions)
            {
                var completionItem = new RoslynCompletionSuggestion(completion);
                this.CompletionList.CompletionData.Add(completionItem);
                maxLength = Math.Max(maxLength, completionItem.Text.Length);
            }
            this.Width = maxLength * 12;
            this.CompletionList.IsFiltering = true;
            this.CompletionList.SelectItem(textBeingCompleted);
        }

        private void SetCompletionBounds(string textBeingCompleted, TextSpan span)
        {
            if (span.Start < this.StartOffset && this.StartOffset < span.End)
            {
                // handle when we're completing an already complete word, e.g. Console.WriteLi|ne
                this.StartOffset = span.Start;
                this.EndOffset = span.End;
            }
            else
            {
                // handle when we're completing partially typed word, e.g. Console.WriteLi|
                this.StartOffset -= textBeingCompleted.Length;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.Key == Key.OemPeriod)
            {
                // while autocompleting, the user typed a period. This will trigger a new completion window, so this old one should close
                this.Hide();
            }

            // close completion window if there are no matches, or only 1 match that is exactly what the user already typed.
            // it seems like this should be handled by the CompletionWindow base class, but it isn't.
            string filter = this.TextArea.Document.Text.Substring(this.StartOffset, this.EndOffset - this.StartOffset);
            var matches = this.CompletionList.CompletionData
                .Where(completion => completion.Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
            
            if(!matches.Any() || (matches.Count == 1 && matches[0].Text.Equals(filter, StringComparison.CurrentCultureIgnoreCase)))
            {
                this.Hide();
            }
        }
    }

    /// <summary>
    /// A single suggestion in the Intellisense Window
    /// </summary>
    class RoslynCompletionSuggestion : ICompletionData
    {
        private readonly CompletionItem completion;

        public RoslynCompletionSuggestion(CompletionItem completion)
        {
            this.completion = completion;
        }

        public ImageSource Image => null;

        public string Text => completion.DisplayText;

        /// <summary>
        /// The UIElement to render
        /// </summary>
        public object Content => Text;

        /// <summary>
        /// Help text in tooltip
        /// </summary>
        public object Description { get; set; }

        public double Priority => 1;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
