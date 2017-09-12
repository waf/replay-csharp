using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Replay.UI
{
    /// <summary>
    /// Draws the ">" at the beginning of each prompt.
    /// </summary>
    public class PromptAdorner : Adorner
    {
        private readonly FormattedText prompt;

        public PromptAdorner(UIElement adornedElement) : base(adornedElement)
        {
            if (!(this.AdornedElement is TextEditor editor))
                return;
            SolidColorBrush color = new SolidColorBrush(Colors.White);
            var typeface = editor.FontFamily.GetTypefaces().First();

            prompt = new FormattedText(">",
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, editor.FontSize, color, 0);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            prompt.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            //var coords = this.AdornedElement
            //    .TransformToAncestor(App.Current.MainWindow)
            //    .Transform(new Point(0, 0));

            drawingContext.DrawText(prompt, new Point(-16, 0));

        }
    }
}
