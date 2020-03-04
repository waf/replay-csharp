using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Replay.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using Xunit;
using System.Windows.Documents;

namespace Replay.Tests.UI
{
    public class PromptAdornerTest
    {
        [WpfFact]
        public void PromptAdorner_RendersPrompt_WithoutError()
        {
            var editor = new TextEditor();
            var adorner = new PromptAdorner(editor);

            adorner.Visibility = Visibility.Visible;
            adorner.InvalidateArrange();

            // system under test
            adorner.Arrange(new Rect(new Size(100, 100)));
        }
    }
}
