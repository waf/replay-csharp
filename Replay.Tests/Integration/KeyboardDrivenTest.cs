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

        public KeyboardDrivenTest(ReplServicesFixture replServicesFixture)
        {
            this.viewModelService = new ViewModelService(replServicesFixture.ReplServices);
        }

        [WpfFact]
        public async Task ExecuteHelp_ThenTabComplete_ProvidesTabCompletions()
        {
            string input = "help~Enter~~Tab~";

            var vm = new WindowViewModel();
            IReadOnlyCollection<ReplCompletion> completions = null;

            // system under test
            await TypeInput(input, vm, (c, _) => completions = c);

            Assert.NotEmpty(completions);
        }

        [WpfFact]
        public async Task Execute()
        {
            string input = "~Enter~~Enter~\"HELLO\"~Enter~";

            var vm = new WindowViewModel();

            // system under test
            await TypeInput(input, vm);

            Assert.Equal("\"HELLO\"", vm.Entries[2].Result);
        }

        private async Task TypeInput(string input, WindowViewModel vm, TriggerIntellisense callback = null)
        {
            var keys = ConvertToKeys(input).ToList();
            var keyboard = Keyboard.PrimaryDevice;
            var source = Substitute.For<PresentationSource>();
            foreach (var key in keys)
            {
                foreach (var line in vm.Entries)
                {
                    line.Document ??= new TextDocument(); // this would normally be done by databinding
                    line.TriggerIntellisense = callback ?? ((items, onClose) => { });
                }

                // type the keys
                var currentLine = vm.Entries[vm.FocusIndex];
                if (!NoOutputKeys.Contains(key))
                {
                    currentLine.Document.Text += key switch
                    {
                        Key.OemQuotes => "\"",
                        _ => keyConverter.ConvertToString(key)
                    };
                }

                // fire the events
                var keyDown = new KeyEventArgs(keyboard, source, 0, key) { RoutedEvent = Keyboard.KeyDownEvent };
                await viewModelService.HandleKeyDown(vm, currentLine, keyDown);
                var keyUp = new KeyEventArgs(keyboard, source, 0, key) { RoutedEvent = Keyboard.KeyUpEvent };
                await viewModelService.HandleKeyUp(vm, currentLine, keyUp);
            }
        }

        /// <summary>
        /// Map a string to a series of key presses that would type that string.
        /// If the string contains a sequence like ~Enter~ or ~Tab~ it is mapped to that key.
        /// </summary>
        private IEnumerable<Key> ConvertToKeys(string input)
        {
            List<char> specialKey = null;
            foreach (var character in input)
            {
                if(character == '~')
                {
                    bool hasSpecialKey = specialKey is { };
                    if(hasSpecialKey)
                    {
                        yield return (Key)keyConverter.ConvertFromString(string.Join("", specialKey));
                        specialKey = null;
                        continue;
                    }
                    else
                    {
                        specialKey = new List<char>();
                        continue;
                    }
                }

                if (specialKey is { })
                {
                    specialKey.Add(character);
                }
                else
                {
                    if (character == '"') yield return Key.OemQuotes;
                    else yield return (Key)keyConverter.ConvertFromString(character.ToString());
                }
            }
        }
    }
}
