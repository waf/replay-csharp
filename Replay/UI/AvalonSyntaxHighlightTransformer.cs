using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Replay.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Replay.UI
{
    /// <summary>
    /// Integration point with Avalon editor for syntax highlighting.
    /// </summary>
    class AvalonSyntaxHighlightTransformer : AsyncDocumentColorizingTransformer
    {
        private readonly IReplServices replServices;
        private readonly Guid lineNumber;

        public AvalonSyntaxHighlightTransformer(IReplServices replServices, Guid lineNumber)
        {
            this.replServices = replServices;
            this.lineNumber = lineNumber;
        }

        protected async override Task ColorizeLineAsync(DocumentLine line, DocumentContext context, IList<VisualLineElement> elements)
        {
            if (line.Length == 0) return;

            string text = context.CurrentContext.Document.GetText(line);
            IReadOnlyCollection<ColorSpan> spans = await HighlightAsync(text);

            int offset = line.Offset;
            foreach (var span in spans)
            {
                base.ChangeLinePart(offset + span.Start, offset + span.End, elements, context, part =>
                {
                    part.TextRunProperties.SetForegroundBrush(new SolidColorBrush(span.Color));
                });
            }
        }

        private async Task<IReadOnlyCollection<ColorSpan>> HighlightAsync(string text)
        {
            try
            {
                return await replServices.HighlightAsync(lineNumber, text);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return Array.Empty<ColorSpan>();
            }
        }
    }
}
