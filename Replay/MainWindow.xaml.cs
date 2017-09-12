using ICSharpCode.AvalonEdit;
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
        readonly ReplModel Model = new ReplModel();
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
            var result = new EvaluationResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var stdout = new ConsoleOutputWriter();
            Console.SetOut(stdout);
            try
            {
                result.ScriptResult = await scriptEvaluator.Evaluate(text);
                if(result.ScriptResult.Exception != null)
                {
                    result.Exception = result.ScriptResult.Exception;
                }
            }
            catch (Exception exception)
            {
                result.Exception = exception;
            }
            result.StandardOutput = stdout.GetOutputOrNull();
            return result;
        }

        private static void Output(TextEditor repl, EvaluationResult result)
        {
            var outputs = ((Panel)repl.Parent).Children.OfType<TextBlock>().ToList();
            var resultPanel = outputs.Single(text => (string)text.Tag == "result");
            var stdoutPanel = outputs.Single(text => (string)text.Tag == "stdout");
            if(result.StandardOutput == null)
            {
                stdoutPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                stdoutPanel.Text = result.StandardOutput;
                stdoutPanel.Visibility = Visibility.Visible;
            }
            if (result.Exception != null)
            {
                resultPanel.Text = result.Exception.Message;
                resultPanel.Foreground = Brushes.Red;
                resultPanel.Visibility = Visibility.Visible;
            }
            else if (result.ScriptResult.ReturnValue != null)
            {
                resultPanel.Text = result.ScriptResult.ReturnValue.ToString();
                resultPanel.Foreground = Brushes.White;
                resultPanel.Visibility = Visibility.Visible;
            }
            else
            {
                resultPanel.Visibility = Visibility.Collapsed;
                resultPanel.Text = null;
            }
        }

        private void MoveToNextLine(TextEditor repl)
        {
            int currentIndex = Model.Entries.IndexOf((ReplResult)repl.DataContext);
            if (currentIndex == Model.Entries.Count - 1)
            {
                Model.Entries.Add(new ReplResult());
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
