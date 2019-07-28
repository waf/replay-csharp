using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis.Completion;
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
            this.CompletionList.IsFiltering = true;

            foreach (var completion in completions)
            {
                var completionItem = new RoslynCompletionSuggestion(completion);
                this.CompletionList.CompletionData.Add(completionItem);
            }

            string textBeingCompleted = textArea.Document.Text.Substring(completions[0].Span.Start, completions[0].Span.Length);
            this.CompletionList.SelectItem(textBeingCompleted);

            this.Show();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // it seems like this should be handled by the CompletionWindow base class, but it isn't.
            string filter = this.TextArea.Document.Text.Substring(this.StartOffset, this.EndOffset - this.StartOffset);
            if(!this.CompletionList.CompletionData.Any(completion => completion.Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
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
        public object Description => Text;

        public double Priority => 1;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completion.Span.Start, completion.Span.Length, this.Text);
        }
    }
}
