﻿using ICSharpCode.AvalonEdit;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Replay.UI
{
    /// <summary>
    /// Draws the ">" at the beginning of each prompt.
    /// </summary>
    public class PromptAdorner : Adorner
    {
        private readonly TextEditor editor;
        private readonly Brush color;
        private readonly Typeface typeface;
        private static readonly double PromptOffset = -8 / 9d;

        public PromptAdorner(TextEditor editor) : base(editor)
        {
            this.typeface = editor.FontFamily.GetTypefaces().First();
            this.editor = editor;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            double fontSize = editor.FontSize;
            Brush foreground = editor.Foreground;

            var prompt = new FormattedText(">",
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, fontSize, foreground, 0)
            {
                PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip
            };
            drawingContext.DrawText(prompt, new Point(PromptOffset * fontSize, 0));
        }

        internal static void AddTo(TextEditor element)
        {
            AdornerLayer
                .GetAdornerLayer(element)
                .Add(new PromptAdorner(element));
        }
    }
}
