using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using NSubstitute;
using Replay.Model;
using Replay.Services;
using Replay.Tests.TestHelpers;
using Replay.ViewModel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Input.Key;
using static System.Windows.Input.ModifierKeys;
using Xunit;

namespace Replay.Tests.Integration
{
    public class KeyboardDrivenTest :  IClassFixture<ReplServicesFixture>
    {
        // system under test
        private readonly ViewModelService viewModelService;

        private readonly KeyConverter keyConverter = new KeyConverter();
        /*
        private readonly IReadOnlyCollection<Key> NoOutputKeys = new[]
        {
            Key.Tab,
            Key.LeftAlt, Key.RightAlt,
            Key.Enter, Key.Return,
            Key.LeftCtrl, Key.RightCtrl,
            Key.Left, Key.Right, Key.Up, Key.Down
        };
        */
        private readonly IReadOnlyDictionary<char, Key> CharToKey = new Dictionary<char, Key>
        {
            { '"', OemQuotes },
            { '#', D3 },
            { '.', OemPeriod },
        };

        public KeyboardDrivenTest(ReplServicesFixture replServicesFixture)
        {
            this.viewModelService = new ViewModelService(replServicesFixture.ReplServices);
        }

        [WpfFact]
        public async Task ExecuteHelp_CompletionsViaTab_ProvidesCompletions()
        {
            IReadOnlyCollection<ReplCompletion> completions = null;

            // system under test
            await TypeInput(
                $"help{Enter}{Tab}",
                new WindowViewModel(),
                (c, _) => completions = c
            );

            Assert.NotEmpty(completions);
        }

        [WpfFact]
        public async Task ExecuteNuget_ValidNugetPackage_InstallsPackage()
        {
            var vm = new WindowViewModel();

            // system under test
            await TypeInput(
                $"#nuget newtonsoft.json{Enter}",
                vm
            );

            Assert.Contains("Installation complete for Newtonsoft.Json", vm.Entries[0].StandardOutput);
        }

        [WpfFact]
        public async Task SystemConsole_CompletionsViaDot_ProvidesCompletions()
        {
            IReadOnlyCollection<ReplCompletion> completions = null;

            // system under test
            await TypeInput(
                $"Console.",
                new WindowViewModel(),
                (c, _) => completions = c
            );

            var names = completions.Select(c => c.CompletionItem.DisplayText).ToArray();

            Assert.Contains("Write", names);
            Assert.Contains("WriteLine", names);
            Assert.DoesNotContain("abstract", names);
        }

        [WpfFact]
        public async Task BlankLines_ThenString_ReturnsString()
        {
            var vm = new WindowViewModel();

            // system under test
            await TypeInput(
                $"{Enter}{Enter}\"Hello World\"{Enter}",
                vm
            );

            Assert.Equal("\"Hello World\"", vm.Entries[2].Result);
        }

        [WpfFact]
        public async Task Content_ThenClearScreen_ClearsTheScreen()
        {
            var vm = new WindowViewModel();

            // system under test
            await TypeInput(
                $"{Enter}{Enter}\"Hello\"{Enter}\"World\"{Control}{L}",
                vm
            );

            Assert.Equal(3, vm.MinimumFocusIndex);
            Assert.Equal(3, vm.FocusIndex);
            Assert.Equal("\"World\"", vm.Entries[3].Document.Text);
            // even though cleared, we should still have the earlier history
            Assert.Equal("\"Hello\"", vm.Entries[2].Document.Text);
        }

        [WpfFact]
        public async Task SmartPaste_UndefinedVariable_DefinesVariable()
        {
            var vm = new WindowViewModel();
            const string code = "var myvar = undefined + 5;";
            Clipboard.SetText($"  {code}  ");

            // system under test
            await TypeInput(
                $"{Control}{Shift}{V}",
                vm
            );

            Assert.Equal(
                "var undefined = ;" + Environment.NewLine + code,
                vm.Entries[0].Document.Text
            );
            Assert.Equal("var undefined = ".Length, vm.Entries[0].CaretOffset);
        }

        private async Task TypeInput(FormattableString inputSequence, WindowViewModel vm, TriggerIntellisense callback = null)
        {
            var device = new MockKeyboardDevice(InputManager.Current);
            ModifierKeys modifier = ModifierKeys.None;
            foreach (var input in ConvertToKeys(inputSequence))
            {
                var currentLine = vm.Entries[vm.FocusIndex];

                // set up document state; in the real app this is done by databinding
                if(currentLine.Document is null)
                {
                    var editor = new TextEditor { Document = new TextDocument() };
                    currentLine.Document ??= editor.Document;
                    currentLine.SetEditor(editor);
                    currentLine.TriggerIntellisense ??= callback ?? ((items, onClose) => { });
                }

                // convert to input to the appropriate key press
                Key key;
                switch (input)
                {
                    case char ch: // type the character into the editor and set up the key event
                        currentLine.Document.Text += ch;
                        currentLine.CaretOffset = currentLine.Document.Text.Length;
                        key = CharToKey.ContainsKey(ch)
                            ? CharToKey[ch]
                            : (Key)keyConverter.ConvertFromString(ch.ToString());
                        break;
                    case Key k:
                        key = k;
                        break;
                    case ModifierKeys mod:
                        modifier |= mod;
                        continue;
                    default:
                        throw new InvalidOperationException("Unhandled type: " + input.GetType());
                }

                device.ModifierKeysImpl = modifier;
                var keyDown = device.CreateKeyEventArgs(key, Keyboard.KeyDownEvent);
                await viewModelService.HandleKeyDown(vm, currentLine, keyDown);
                var keyUp = device.CreateKeyEventArgs(key, Keyboard.KeyUpEvent);
                await viewModelService.HandleKeyUp(vm, currentLine, keyUp);

                modifier = ModifierKeys.None;
            }
        }

        /// <summary>
        /// Map a string to a series of key presses that would type that string.
        /// If the string contains a sequence like ~Enter~ or ~Tab~ it is mapped to that key.
        /// e.g. maps "Consol~Tab~" to ["C" "o" "n" "s" "o" "l" "Tab" ]
        /// </summary>
        private IEnumerable<object> ConvertToKeys(FormattableString input)
        {
            for (int i = 0; i < input.Format.Length; i++)
            {
                var ch = input.Format[i];

                if (ch == '{'
                    && i + 2 < input.Format.Length
                    && char.IsDigit(input.Format[i + 1])
                    && input.Format[i + 2] == '}')
                {
                    yield return input.GetArgument((int)char.GetNumericValue(input.Format[i + 1]));
                    i += 2;
                }
                else
                {
                    yield return ch;
                }
            }
        }
    }
}
