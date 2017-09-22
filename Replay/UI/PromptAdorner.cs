using ICSharpCode.AvalonEdit;
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
            drawingContext.DrawText(prompt, new Point(-16, 0));
        }

        internal static void AddTo(UIElement element)
        {
            AdornerLayer
                .GetAdornerLayer(element)
                .Add(new PromptAdorner(element));
        }
    }
}
