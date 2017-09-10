using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace Replay.UI
{
    /// <summary>
    /// Integration point with Avalon editor for syntax highlighting.
    /// </summary>
    class AvalonSyntaxHighlightTransformer : DocumentColorizingTransformer
    {
        private readonly SyntaxHighlighter highlighter;

        public AvalonSyntaxHighlightTransformer(SyntaxHighlighter highlighter)
        {
            this.highlighter = highlighter;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            var spans = highlighter.Highlight(text);
            foreach (var span in spans)
            {
                base.ChangeLinePart(line.Offset + span.Start, line.Offset + span.End, part =>
                {
                    part.TextRunProperties.SetForegroundBrush(new SolidColorBrush(span.Color));
                });
            }
        }

        public static void Register(TextEditor editor, SyntaxHighlighter syntaxHighlighter)
        {
            editor.TextArea.TextView.LineTransformers
                .Add(new AvalonSyntaxHighlightTransformer(syntaxHighlighter));
        }
    }
}
