using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Scripting;
using Replay.Model;
using Replay.Services;
using Replay.UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Replay
{
    /// <summary>
    /// Represents a single REPL window.
    /// </summary>
    public partial class MainWindow : Window
    {
        CompletionWindow completionWindow;
        readonly ReplServices services = new ReplServices();
        readonly ReplViewModel Model = new ReplViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = Model;
        }

        private async void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var repl = (TextEditor)sender;

            // enter evaluates the script, but shift-enter is for a soft newline within the textarea
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                bool stayOnCurrentLine = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                await ReadEvalPrintLoop(repl, stayOnCurrentLine);
            }
            else if (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                var completions = await services.CompleteCode(repl.Text);
                if(completions.Any())
                {
                    completionWindow = new IntellisenseWindow(repl.TextArea, completions);
                }
            }
        }

        private async Task ReadEvalPrintLoop(TextEditor repl, bool stayOnCurrentLine)
        {
            // read
            string text = repl.Text;
            // eval
            var result = await services.Evaluate(text);
            // print
            Print(repl, result);
            // loop
            if (result.Exception == null && !stayOnCurrentLine)
            {
                MoveToNextLine(repl);
            }
        }

        private static void Print(TextEditor repl, EvaluationResult result)
        {
            var viewmodel = (ReplLineViewModel)repl.DataContext;
            viewmodel.SetResult(result);
        }

        private void MoveToNextLine(TextEditor repl)
        {
            int currentIndex = Model.Entries.IndexOf((ReplLineViewModel)repl.DataContext);
            if (currentIndex == Model.Entries.Count - 1)
            {
                Model.Entries.Add(new ReplLineViewModel());
            }
            Model.FocusIndex = currentIndex + 1;
        }

        private async void TextEditor_Initialized(object sender, EventArgs e)
        {
            var editor = (TextEditor)sender;
            AdornerLayer
                .GetAdornerLayer(editor)
                .Add(new PromptAdorner(editor));
            editor.TextArea.MouseWheel += TextArea_MouseWheel;
            await services.ConfigureSyntaxHighlighting(editor);
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var repl = (TextEditor)sender;
            var lineViewModel = (ReplLineViewModel)repl.DataContext;
            if(lineViewModel.IsFocused)
            {
                Keyboard.Focus(repl.TextArea);
            }
        }

        /// <summary>
        /// We want the scroll wheel to scroll the entire window.
        /// However, the textarea used for input can be multiline, and it
        /// swallows scroll events by default. So we listen for scroll events
        /// on the textarea, and re-raise them on our window's scrollview.
        /// </summary>
        private void TextArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            this.Scroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            });
        }

        private void TextEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            ((TextEditor)sender).TextArea.MouseWheel -= TextArea_MouseWheel;
        }
    }
}
