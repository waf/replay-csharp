using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using NSubstitute;
using Replay.Model;
using Replay.Services;
using Replay.ViewModel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xunit;

namespace Replay.Tests.Integration
{
    public class KeyboardDrivenTest :  IClassFixture<ReplServicesFixture>
    {
        // system under test
        private readonly ViewModelService viewModelService;

        private readonly KeyConverter keyConverter = new KeyConverter();
        private readonly IReadOnlyCollection<Key> NoOutputKeys = new[]
        {
            Key.Tab,
            Key.LeftAlt, Key.RightAlt,
            Key.Enter, Key.Return,
            Key.LeftCtrl, Key.RightCtrl,
            Key.Left, Key.Right, Key.Up, Key.Down
        };
        private readonly IReadOnlyDictionary<string, Key> CharToKey = new Dictionary<string, Key>
        {
            { "\"", Key.OemQuotes },
            { ".", Key.OemPeriod },
        };

        public KeyboardDrivenTest(ReplServicesFixture replServicesFixture)
        {
            this.viewModelService = new ViewModelService(replServicesFixture.ReplServices);
        }

        [WpfFact]
        public async Task ExecuteHelp_CompletionsViaTab_ProvidesCompletions()
        {
            string input = "help~Enter~~Tab~";

            var vm = new WindowViewModel();
            IReadOnlyCollection<ReplCompletion> completions = null;

            // system under test
            await TypeInput(input, vm, (c, _) => completions = c);

            Assert.NotEmpty(completions);
        }

        [WpfFact]
        public async Task SystemConsole_CompletionsViaDot_ProvidesCompletions()
        {
            string input = "Console.";

            var vm = new WindowViewModel();
            IReadOnlyCollection<ReplCompletion> completions = null;

            // system under test
            await TypeInput(input, vm, (c, _) => completions = c);

            var names = completions.Select(c => c.CompletionItem.DisplayText).ToArray();

            Assert.Contains("Write", names);
            Assert.Contains("WriteLine", names);
            Assert.DoesNotContain("abstract", names);
        }

        [WpfFact]
        public async Task BlankLines_ThenString_ReturnsString()
        {
            string input = "~Enter~~Enter~\"Hello World\"~Enter~";

            var vm = new WindowViewModel();

            // system under test
            await TypeInput(input, vm);

            Assert.Equal("\"Hello World\"", vm.Entries[2].Result);
        }

        private async Task TypeInput(string input, WindowViewModel vm, TriggerIntellisense callback = null)
        {
            var keyboard = Keyboard.PrimaryDevice;
            var source = Substitute.For<PresentationSource>();
            foreach (var character in ConvertToKeys(input))
            {
                var currentLine = vm.Entries[vm.FocusIndex];
                if(currentLine.Document is null)
                {
                    // this would normally be done by databinding
                    var editor = new TextEditor { Document = new TextDocument() };
                    currentLine.Document ??= editor.Document;
                    currentLine.SetEditor(editor);
                    currentLine.TriggerIntellisense ??= callback ?? ((items, onClose) => { });
                }

                // convert a character like '.' to OemPeriod
                Key key = CharToKey.ContainsKey(character)
                    ? CharToKey[character]
                    : (Key)keyConverter.ConvertFromString(character);

                // type the key
                if (!NoOutputKeys.Contains(key))
                {
                    currentLine.Document.Text += character;
                }
                currentLine.CaretOffset = currentLine.Document.Text.Length;

                var keyDown = new KeyEventArgs(keyboard, source, 0, key) { RoutedEvent = Keyboard.KeyDownEvent };
                await viewModelService.HandleKeyDown(vm, currentLine, keyDown);
                var keyUp = new KeyEventArgs(keyboard, source, 0, key) { RoutedEvent = Keyboard.KeyUpEvent };
                await viewModelService.HandleKeyUp(vm, currentLine, keyUp);
            }
        }

        /// <summary>
        /// Map a string to a series of key presses that would type that string.
        /// If the string contains a sequence like ~Enter~ or ~Tab~ it is mapped to that key.
        /// e.g. maps "Consol~Tab~" to ["C" "o" "n" "s" "o" "l" "Tab" ]
        /// </summary>
        private IEnumerable<string> ConvertToKeys(string input)
        {
            List<char> specialKey = null;
            foreach (var character in input)
            {
                if(character is '~')
                {
                    if(specialKey is null)
                    {
                        specialKey = new List<char>();
                    }
                    else
                    {
                        yield return string.Join("", specialKey);
                        specialKey = null;
                    }
                    continue;
                }

                if (specialKey is null)
                {
                    yield return character.ToString();
                }
                else
                {
                    specialKey.Add(character);
                }
            }
        }
    }
}
