using ICSharpCode.AvalonEdit;
using Replay.UI;
using System.Windows;
using Xunit;

namespace Replay.Tests.UI
{
    public class PromptAdornerTest
    {
        [WpfFact]
        public void PromptAdorner_RendersPrompt_WithoutError()
        {
            var editor = new TextEditor();
            var adorner = new PromptAdorner(editor)
            {
                Visibility = Visibility.Visible
            };

            adorner.InvalidateArrange();

            // system under test
            adorner.Arrange(new Rect(new Size(100, 100)));
        }
    }
}
