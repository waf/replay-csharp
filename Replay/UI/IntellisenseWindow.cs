using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis.Completion;
using System;
using System.Collections.Immutable;
using System.Windows.Media;

namespace Replay.UI
{
    class IntellisenseWindow : CompletionWindow
    {
        public IntellisenseWindow(TextArea textArea, ImmutableArray<CompletionItem> completions)
            : base(textArea)
        {
            foreach (var completion in completions)
            {
                this.CompletionList.CompletionData
                    .Add(new CodeCompletionSuggestion(completion.DisplayText));
            }
            this.Show();
        }
    }

    /// <summary>
    /// A single suggestion in the Intellisense Window
    /// </summary>
    class CodeCompletionSuggestion : ICompletionData
    {
        public CodeCompletionSuggestion(string text)
        {
            this.Text = text;
        }

        public ImageSource Image => null;

        public string Text { get; }

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
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
