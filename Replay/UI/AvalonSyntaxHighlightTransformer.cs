using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using System.Windows.Media;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Replay.UI
{
    /// <summary>
    /// Integration point with Avalon editor for syntax highlighting.
    /// </summary>
    class AvalonSyntaxHighlightTransformer : AsyncDocumentColorizingTransformer
    {
        private readonly ReplServices replServices;
        private readonly int lineNumber;

        public AvalonSyntaxHighlightTransformer(ReplServices replServices, int lineNumber)
        {
            this.replServices = replServices;
            this.lineNumber = lineNumber;
        }

        protected async override Task ColorizeLineAsync(DocumentLine line, DocumentContext context, IList<VisualLineElement> elements)
        {
            if (line.Length == 0) return;

            string text = context.CurrentContext.Document.GetText(line);

            var spans = await replServices.HighlightAsync(lineNumber, text);
            foreach (var span in spans)
            {
                base.ChangeLinePart(line.Offset + span.Start, line.Offset + span.End, elements, context, part =>
                {
                    part.TextRunProperties.SetForegroundBrush(new SolidColorBrush(span.Color));
                });
            }
        }
    }
}
