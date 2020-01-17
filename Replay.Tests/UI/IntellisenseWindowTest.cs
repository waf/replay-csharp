using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using Replay.Tests.TestHelpers;
using Replay.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace Replay.Tests.Services
{
    public class IntellisenseWindowTest : IClassFixture<ReplServicesFixture>
    {
        private readonly ReplServices replServices;

        public IntellisenseWindowTest(ReplServicesFixture replServicesFixture)
        {
            this.replServices = replServicesFixture.ReplServices;
        }

        [WpfFact]
        public async Task IntellisenseWindow_ProvidedCompletions_ShowsCompletions()
        {
            const string code = "Console.";
            var completions = await this.replServices.CompleteCodeAsync(Guid.NewGuid(), code, code.Length);
            Assert.NotEmpty(completions);

            var editor = new TextEditor { Document = new TextDocument() };
            editor.Document.Text = code;

            var window = new IntellisenseWindow(editor.TextArea, completions, () => { });

            var items = window.CompletionList.ListBox.Items;
            Assert.NotEmpty(items);

            var first = items[0] as RoslynCompletionSuggestion;
            Assert.NotNull(first);
            Assert.Equal("BackgroundColor", first.Text);
            Assert.Contains("Gets or sets the background color", first.Description.ToString());
        }
    }
}
