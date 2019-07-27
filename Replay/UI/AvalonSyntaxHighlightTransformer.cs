using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using System.Windows.Media;

namespace Replay.UI
{
    /// <summary>
    /// Integration point with Avalon editor for syntax highlighting.
    /// </summary>
    class AvalonSyntaxHighlightTransformer : DocumentColorizingTransformer
    {
        private readonly ReplServices replServices;
        private readonly int lineNumber;

        public AvalonSyntaxHighlightTransformer(ReplServices replServices, int lineNumber)
        {
            this.replServices = replServices;
            this.lineNumber = lineNumber;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.Length == 0) return;

            string text = CurrentContext.Document.GetText(line);

            var spans = replServices.HighlightAsync(lineNumber, text).Result;
            foreach (var span in spans)
            {
                base.ChangeLinePart(line.Offset + span.Start, line.Offset + span.End, part =>
                {
                    part.TextRunProperties.SetForegroundBrush(new SolidColorBrush(span.Color));
                });
            }
        }

    }
}
