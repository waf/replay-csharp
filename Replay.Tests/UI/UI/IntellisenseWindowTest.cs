using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using Replay.Tests.TestHelpers;
using Replay.UI;
using Replay.ViewModel;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace Replay.Tests.Services
{
    public class IntellisenseWindowTest : IClassFixture<ReplServicesFixture>
    {
        private readonly ReplServices replServices;
        private readonly MockKeyboardDevice keyboard;

        public IntellisenseWindowTest(ReplServicesFixture replServicesFixture)
        {
            this.replServices = replServicesFixture.ReplServices;
            this.keyboard = new MockKeyboardDevice();
        }

        [WpfFact]
        public async Task IntellisenseWindow_ProvidedCompletions_ShowsCompletions()
        {
            IntellisenseWindow window = await CompleteText("Console.");
            PressKey(window, Key.OemPeriod);

            var first = GetFirstSuggestion(window);
            Assert.NotNull(first);
            Assert.Equal("BackgroundColor", first.Text);
            Assert.Contains("Gets or sets the background color", first.Description.ToString());
        }

        [WpfFact]
        public async Task IntellisenseWindow_AlreadyTypedOnlyCompletion_DoesNotShowWindow()
        {
            IntellisenseWindow window = await CompleteText("Console.WriteLine");

            bool closed = false;
            window.Closed += (_, __) => closed = true;

            PressKey(window, Key.E);
            Assert.True(closed);
        }

        private void PressKey(IntellisenseWindow window, Key key)
        {
            var keyDown = keyboard.CreateKeyEventArgs(key, Keyboard.KeyDownEvent);
            window.RaiseEvent(keyDown);
            var keyUp = keyboard.CreateKeyEventArgs(key, Keyboard.KeyUpEvent);
            window.RaiseEvent(keyUp);
        }

        private static RoslynCompletionSuggestion GetFirstSuggestion(IntellisenseWindow window)
        {
            var items = window.CompletionList.ListBox.Items;
            Assert.NotEmpty(items);
            var first = items[0] as RoslynCompletionSuggestion;
            return first;
        }

        private async Task<IntellisenseWindow> CompleteText(string text)
        {
            var editor = new TextEditor { Document = new TextDocument() };
            editor.Document.Text = text;
            var completions = await this.replServices.CompleteCodeAsync(Guid.NewGuid(), editor.Document.Text, editor.Document.Text.Length);
            var window = new IntellisenseWindow(new WindowViewModel().Intellisense, editor.TextArea, completions);
            return window;
        }
    }
}
