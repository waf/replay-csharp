// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ICSharpCode.AvalonEdit.Rendering
{
    /// <summary>
    /// Base class for <see cref="IVisualLineTransformer"/> that helps
    /// colorizing the document. Derived classes can work with document lines
    /// and text offsets and this class takes care of the visual lines and visual columns.
    /// </summary>
    public abstract class AsyncDocumentColorizingTransformer : AsyncColorizingTransformer
    {
        /// <inheritdoc/>
        protected override async Task ColorizeAsync(ITextRunConstructionContext context, IList<VisualLineElement> elements)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var currentDocumentLine = context.VisualLine.FirstDocumentLine;
            var firstLineStart = currentDocumentLine.Offset;
            var currentDocumentLineStartOffset = currentDocumentLine.Offset;
            var currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
            int currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
            var documentContext = new DocumentContext
            {
                CurrentContext = context,
                CurrentDocumentLineStartOffset = currentDocumentLineStartOffset,
                CurrentDocumentLineEndOffset = currentDocumentLineEndOffset,
                FirstLineStart = firstLineStart
            };

            if (context.VisualLine.FirstDocumentLine == context.VisualLine.LastDocumentLine)
            {
                await ColorizeLineAsync(currentDocumentLine, documentContext, elements);
            }
            else
            {
                await ColorizeLineAsync(currentDocumentLine, documentContext, elements);
                // ColorizeLine modifies the visual line elements, loop through a copy of the line elements
                foreach (VisualLineElement e in context.VisualLine.Elements.ToArray())
                {
                    int elementOffset = firstLineStart + e.RelativeTextOffset;
                    if (elementOffset >= currentDocumentLineTotalEndOffset)
                    {
                        currentDocumentLine = context.Document.GetLineByOffset(elementOffset);
                        currentDocumentLineStartOffset = currentDocumentLine.Offset;
                        currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
                        currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
                        await ColorizeLineAsync(currentDocumentLine, documentContext, elements);
                    }
                }
            }
        }

        /// <summary>
        /// Override this method to colorize an individual document line.
        /// </summary>
        protected abstract Task ColorizeLineAsync(DocumentLine line, DocumentContext context, IList<VisualLineElement> elements);

        /// <summary>
        /// Changes a part of the current document line.
        /// </summary>
        /// <param name="startOffset">Start offset of the region to change</param>
        /// <param name="endOffset">End offset of the region to change</param>
        /// <param name="action">Action that changes an individual <see cref="VisualLineElement"/>.</param>
        protected void ChangeLinePart(int startOffset, int endOffset, IList<VisualLineElement> elements, DocumentContext context, Action<VisualLineElement> action)
        {
            if (startOffset < context.CurrentDocumentLineStartOffset || startOffset > context.CurrentDocumentLineEndOffset)
                throw new ArgumentOutOfRangeException("startOffset", startOffset, "Value must be between " + context.CurrentDocumentLineStartOffset + " and " + context.CurrentDocumentLineEndOffset);
            if (endOffset < startOffset || endOffset > context.CurrentDocumentLineEndOffset)
                throw new ArgumentOutOfRangeException("endOffset", endOffset, "Value must be between " + startOffset + " and " + context.CurrentDocumentLineEndOffset);
            VisualLine vl = context.CurrentContext.VisualLine;
            int visualStart = vl.GetVisualColumn(startOffset - context.FirstLineStart);
            int visualEnd = vl.GetVisualColumn(endOffset - context.FirstLineStart);
            if (visualStart < visualEnd)
            {
                ChangeVisualElements(visualStart, visualEnd, elements, action);
            }
        }
    }

    public class DocumentContext
    {
        public int CurrentDocumentLineStartOffset { get; internal set; }
        public int CurrentDocumentLineEndOffset { get; internal set; }
        public int FirstLineStart { get; internal set; }
        public ITextRunConstructionContext CurrentContext { get; internal set; }
    }
}
