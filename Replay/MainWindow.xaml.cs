using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Replay.Model;
using Replay.Services;
using Replay.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        private async void TextEditor_PreviewKeyDown(TextEditor lineEditor, KeyEventArgs e)
        {
            if (completionWindow?.IsVisible ?? false) return;
            var mapping = KeyboardShortcuts.MapToCommand(e);
            if (!mapping.HasValue) return;
            var command = mapping.Value;

            switch (command)
            {
                case ReplCommand.EvaluateCurrentLine:
                    await ReadEvalPrintLoop(lineEditor, stayOnCurrentLine: false);
                    return;
                case ReplCommand.ReevaluateCurrentLine:
                    await ReadEvalPrintLoop(lineEditor, stayOnCurrentLine: true);
                    return;
                case ReplCommand.OpenIntellisense:
                    await CompleteCode(lineEditor);
                    return;
                case ReplCommand.GoToFirstLine:
                    Model.FocusIndex = 0;
                    return;
                case ReplCommand.GoToLastLine:
                    Model.FocusIndex = Model.Entries.Count - 1;
                    return;
                case ReplCommand.LineDown:
                    if (!lineEditor.IsCaretOnFinalLine()) return;
                    e.Handled = true;
                    Model.FocusIndex++;
                    return;
                case ReplCommand.LineUp:
                    if (!lineEditor.IsCaretOnFirstLine()) return;
                    e.Handled = true;
                    Model.FocusIndex--;
                    return;
                default:
                    throw new ArgumentException("Unhandled editor command: " + command);
            }
        }

        private async void TextEditor_PreviewKeyUp(TextEditor lineEditor, KeyEventArgs e)
        {
            if (completionWindow?.IsVisible ?? false) return;

            // complete member accesses
            if (e.Key == Key.OemPeriod)
            {
                await CompleteCode(lineEditor);
            }
        }

        private async Task ReadEvalPrintLoop(TextEditor lineEditor, bool stayOnCurrentLine)
        {
            // read
            string text = lineEditor.Text;
            if (text == "exit")
            {
                Application.Current.Shutdown();
            }
            // eval
            var result = await services.Evaluate(text);
            // print
            Print(lineEditor, result);
            // loop
            if (result.Exception == null && !stayOnCurrentLine)
            {
                MoveToNextLine(lineEditor);
            }
        }

        private static void Print(TextEditor lineEditor, EvaluationResult result)
        {
            lineEditor.ViewModel().SetResult(result);
        }

        private void MoveToNextLine(TextEditor lineEditor)
        {
            int currentIndex = Model.Entries.IndexOf(lineEditor.ViewModel());
            if (currentIndex == Model.Entries.Count - 1)
            {
                Model.Entries.Add(new LineEditorViewModel());
            }
            Model.FocusIndex = currentIndex + 1;
        }

        private async Task CompleteCode(TextEditor lineEditor)
        {
            var completions = await services.CompleteCode(lineEditor.Text);
            if (completions.Any())
            {
                completionWindow = new IntellisenseWindow(lineEditor.TextArea, completions);
            }
        }

        private async void TextEditor_Initialized(TextEditor lineEditor, EventArgs e)
        {
            PromptAdorner.DrawPrompt(lineEditor);
            lineEditor.TextArea.MouseWheel += TextArea_MouseWheel;
            await services.ConfigureSyntaxHighlighting(lineEditor);
        }

        private void TextEditor_Loaded(TextEditor lineEditor, RoutedEventArgs e)
        {
            if (lineEditor.ViewModel().IsFocused)
            {
                Keyboard.Focus(lineEditor.TextArea);
            }
        }

        private void TextEditor_Unloaded(TextEditor lineEditor, RoutedEventArgs e)
        {
            lineEditor.TextArea.MouseWheel -= TextArea_MouseWheel;
        }

        /// <summary>
        /// We want the scroll wheel to scroll the entire window.
        /// However, the textarea used for input can be multiline, and it
        /// swallows scroll events by default. So we listen for scroll events
        /// on the textarea, and re-raise them on our window's scrollview.
        /// </summary>
        private void TextArea_MouseWheel(TextEditor lineEditor, MouseWheelEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;
            this.Scroll.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = lineEditor
            });
        }

        private void TextEditor_GotFocus(TextEditor lineEditor, RoutedEventArgs e)
        {
            // update our viewmodel if the user manually focuses a lineEditor (e.g. with the mouse).
            int newIndex = Model.Entries.IndexOf(lineEditor.ViewModel());
            if (Model.FocusIndex != newIndex)
            {
                Model.FocusIndex = newIndex;
            }
        }

        #region ugly casting of untyped event handlers
        private void TextEditor_GotFocus(object sender, RoutedEventArgs e) => TextEditor_GotFocus((TextEditor)sender, e);
        private void TextEditor_Unloaded(object sender, RoutedEventArgs e) => TextEditor_Unloaded((TextEditor)sender, e);
        private void TextArea_MouseWheel(object sender, MouseWheelEventArgs e) => TextArea_MouseWheel((TextEditor)sender, e);
        private void TextEditor_Loaded(object sender, RoutedEventArgs e) => TextEditor_Loaded((TextEditor)sender, e);
        private void TextEditor_Initialized(object sender, EventArgs e) => TextEditor_Initialized((TextEditor)sender, e);
        private void TextEditor_PreviewKeyUp(object sender, KeyEventArgs e) => TextEditor_PreviewKeyUp((TextEditor)sender, e);
        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e) => TextEditor_PreviewKeyDown((TextEditor)sender, e);
        #endregion
    }
}