using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using Replay.UI;
using System;
using System.Linq;
using Xunit;

namespace Replay.Tests.UI
{
    public class AvalonSyntaxHighlightTransformerTest : IClassFixture<ReplServicesFixture>
    {
        private readonly ReplServices replServices;
        private readonly AvalonSyntaxHighlightTransformer transformer;

        public AvalonSyntaxHighlightTransformerTest(ReplServicesFixture replServicesFixture)
        {
            this.replServices = replServicesFixture.ReplServices;
            this.transformer = new AvalonSyntaxHighlightTransformer(replServices, Guid.Empty);
        }

        [WpfFact]
        public void HighlightTransformer_ProvidedLine_TransformsLine()
        {
            var editor = new TextEditor { Document = new TextDocument() };
            editor.TextArea.TextView.LineTransformers.Add(
                transformer
            );
            editor.Document.Text = @"var x = ""Hello World""";
            var line = editor.Document.Lines.First();

            // trigger syntax highlighting using the transformer
            var visualLine = editor.TextArea.TextView.GetOrConstructVisualLine(line);

            var expectedTokens = new[] { "var", " ", "x", " ", "=", " ", @"""Hello World""" };

            Assert.Equal(
                expectedTokens.Select(token => token.Length),
                visualLine.Elements.Select(elem => elem.VisualLength)
            );
            Assert.All(
                visualLine.Elements,
                element => Assert.NotNull(element.TextRunProperties.ForegroundBrush)
            );
        }
    }
}
