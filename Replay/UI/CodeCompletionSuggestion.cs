using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows.Media;

namespace Replay.UI
{
    class CodeCompletionSuggestion : ICompletionData
    {

        public CodeCompletionSuggestion(string text)
        {
            this.Text = text;
        }

        public ImageSource Image => null;

        public string Text { get; }

        public object Content => Text;

        public object Description => $"help text for {Text}";

        public double Priority => 1;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
