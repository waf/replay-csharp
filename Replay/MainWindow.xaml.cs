using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis.Scripting;
using Replay.Model;
using Replay.Services;
using Replay.UI;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly SyntaxHighlighter syntaxHighlighter = new SyntaxHighlighter();
        readonly ScriptEvaluator scriptEvaluator = new ScriptEvaluator();
        readonly ReplViewModel Model = new ReplViewModel();
        private int index = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = Model;
            Task.Run(WarmpUp);
        }

        private void TextEditor_Initialized(object sender, EventArgs e)
        {
            var editor = (TextEditor)sender;
            AvalonSyntaxHighlightTransformer
                .Register(editor, syntaxHighlighter);
            AdornerLayer
                .GetAdornerLayer(editor)
                .Add(new PromptAdorner(editor));
            editor.TextArea.MouseWheel += TextArea_MouseWheel;
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

        private async void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // enter evaluates the script, but shift-enter is for a soft newline within the textarea
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                var repl = (TextEditor)sender;
                // read
                string text = repl.Text;
                // eval
                var result = await Evaluate(text);
                // print
                Output(repl, result);
                // loop
                if(result.Exception == null
                    && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) //ctrl-enter stays on the current line
                {
                    MoveToNextLine(repl);
                }
            }
        }

        /// <summary>
        /// Run the script and return the result, capturing any exceptions or standard output.
        /// </summary>
        private async Task<EvaluationResult> Evaluate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new EvaluationResult();
            }

            using(var stdout = new ConsoleOutputWriter())
            {
                var evaluated = await EvaluateCapturingError(text);
                return new EvaluationResult
                {
                    ScriptResult = evaluated.Result,
                    Exception = evaluated.Exception,
                    StandardOutput = stdout.GetOutputOrNull()
                };
            }
        }

        private async Task<(ScriptState<object> Result, Exception Exception)> EvaluateCapturingError(string text)
        {
            ScriptState<object> result = null;
            Exception exception = null;
            try
            {
                result = await scriptEvaluator.Evaluate(text);
                exception = result?.Exception;
            }
            catch (Exception e)
            {
                exception = e;
            }
            return (result, exception);
        }

        private static void Output(TextEditor repl, EvaluationResult result)
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
                this.index = currentIndex + 1;
            }
            else
            {
                Model.FocusIndex = currentIndex + 1;
            }
        }

        private async Task WarmpUp()
        {
            const string code = @"using System; Console.WriteLine(""Hello""); ""Hello""";
            syntaxHighlighter.Highlight(code);
            await scriptEvaluator.Evaluate(code);
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            Model.FocusIndex = index;
        }

        private void TextEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            ((TextEditor)sender).TextArea.MouseWheel -= TextArea_MouseWheel;
        }
    }
}
