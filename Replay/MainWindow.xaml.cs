using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
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
            Task.Run(BackgroundInitializationAsync);
        }

        private async void TextEditor_PreviewKeyDown(TextEditor lineEditor, KeyEventArgs e)
        {
            if (completionWindow?.IsVisible ?? false) return;

            var command = KeyboardShortcuts.MapToCommand(e);
            if (!command.HasValue) return;

            e.Handled = true;
            switch (command.Value)
            {
                case ReplCommand.EvaluateCurrentLine:
                    await ReadEvalPrintLoop(lineEditor.ViewModel(), stayOnCurrentLine: false);
                    return;
                case ReplCommand.ReevaluateCurrentLine:
                    await ReadEvalPrintLoop(lineEditor.ViewModel(), stayOnCurrentLine: true);
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
                case ReplCommand.LineDown when lineEditor.IsCaretOnFinalLine():
                    Model.FocusIndex++;
                    return;
                case ReplCommand.LineUp when lineEditor.IsCaretOnFirstLine():
                    Model.FocusIndex--;
                    return;
                default:
                    e.Handled = false;
                    break;
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

        private async Task ReadEvalPrintLoop(LineEditorViewModel line, bool stayOnCurrentLine)
        {
            // read
            string text = line.Document.Text;
            if (text.Trim() == "exit")
            {
                Application.Current.Shutdown();
            }
            // eval
            var result = await services.EvaluateAsync(line.Id, text);
            // print
            Print(line, result);
            // loop
            if (result.Exception == null && !stayOnCurrentLine)
            {
                MoveToNextLine(line);
            }
        }

        private static void Print(LineEditorViewModel lineEditor, EvaluationResult result)
        {
            lineEditor.SetResult(result);
        }

        private void MoveToNextLine(LineEditorViewModel lineEditor)
        {
            int currentIndex = Model.Entries.IndexOf(lineEditor);
            if (currentIndex == Model.Entries.Count - 1)
            {
                Model.Entries.Add(new LineEditorViewModel());
            }
            Model.FocusIndex = currentIndex + 1;
        }

        private async Task CompleteCode(TextEditor lineEditor)
        {
            var line = lineEditor.ViewModel();
            var completions = await services.CompleteCodeAsync(line.Id, line.Document.Text);
            if (completions.Any())
            {
                completionWindow = new IntellisenseWindow(lineEditor.TextArea, completions);
            }
        }

        private void TextEditor_Initialized(TextEditor lineEditor, EventArgs e)
        {
            PromptAdorner.AddTo(lineEditor);
            lineEditor.TextArea.MouseWheel += TextArea_MouseWheel;

            var lineNumber = lineEditor.ViewModel().Id;
            lineEditor.TextArea.TextView.LineTransformers.Add(
                new AvalonSyntaxHighlightTransformer(services, lineNumber)
            );
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
        private void TextArea_MouseWheel(TextArea lineEditor, MouseWheelEventArgs e)
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

        private void Window_PreviewMouseWheel(Window sender, MouseWheelEventArgs e)
        {
            // scale the font size
            if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                int delta = e.Delta / Math.Abs(e.Delta); // -1 or +1;
                this.Window.FontSize += delta;
                Thickness padding = this.ReplEntries.Padding;
                this.ReplEntries.Padding = new Thickness(padding.Left + delta, padding.Top, padding.Right, padding.Bottom);
            }
        }

        /// <summary>
        /// Roslyn can be a little bit slow to warm up, which can cause lag when the
        /// user first starts typing / evaluating code. Do the warm up in a background
        /// thread beforehand to improve the user experience.
        /// </summary>
        private Task BackgroundInitializationAsync()
        {
            const string initializationCode = @"using System; Console.WriteLine(""Hello""); ""World""";
            return Task.WhenAll(
                services.HighlightAsync(0, initializationCode),
                services.CompleteCodeAsync(0, initializationCode),
                services.EvaluateAsync(0, initializationCode)
            );
        }

        #region ugly casting of untyped event handlers
        private void TextEditor_GotFocus(object sender, RoutedEventArgs e) => TextEditor_GotFocus((TextEditor)sender, e);
        private void TextEditor_Unloaded(object sender, RoutedEventArgs e) => TextEditor_Unloaded((TextEditor)sender, e);
        private void TextEditor_Loaded(object sender, RoutedEventArgs e) => TextEditor_Loaded((TextEditor)sender, e);
        private void TextEditor_Initialized(object sender, EventArgs e) => TextEditor_Initialized((TextEditor)sender, e);
        private void TextEditor_PreviewKeyUp(object sender, KeyEventArgs e) => TextEditor_PreviewKeyUp((TextEditor)sender, e);
        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e) => TextEditor_PreviewKeyDown((TextEditor)sender, e);
        private void TextArea_MouseWheel(object sender, MouseWheelEventArgs e) => TextArea_MouseWheel((TextArea)sender, e);
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e) => Window_PreviewMouseWheel((Window)sender, e);
        #endregion
    }
}