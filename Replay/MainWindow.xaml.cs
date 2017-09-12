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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            AvalonSyntaxHighlightTransformer.Register(editor, syntaxHighlighter);
            var promptLayer = AdornerLayer.GetAdornerLayer(editor);
            promptLayer.Add(new PromptAdorner(editor));
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
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                this.Scroll.RaiseEvent(eventArg);
            }
        }

        private async void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                var repl = (TextEditor)sender;
                ScriptState<object> result = null;
                Exception evaluatorException = null;
                var console = new ConsoleOutputWriter();
                try
                {
                    Console.SetOut(console);
                    result = await scriptEvaluator.Evaluate(repl.Text);
                }
                catch (Exception evalException)
                {
                    evaluatorException = evalException;
                }
                var exception = evaluatorException ?? result.Exception;
                string output = console.HasOutput ? console.GetStringBuilder().ToString() : null;
                Output(repl, result, output, exception);
                if(exception == null && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    int currentIndex = Model.Entries.IndexOf((ReplResult)repl.DataContext);
                    if(currentIndex == Model.Entries.Count - 1)
                    {
                        Model.Entries.Add(new ReplResult());
                        this.index = currentIndex + 1;
                    }
                    else
                    {
                        Model.FocusIndex = currentIndex + 1;
                    }
                }
            }
        }

        private static void Output(TextEditor repl, ScriptState<object> result, String stdout, Exception exception)
        {
            var outputs = ((Panel)repl.Parent).Children.OfType<TextBlock>().ToList();
            var resultPanel = outputs.Single(text => (string)text.Tag == "result");
            var stdoutPanel = outputs.Single(text => (string)text.Tag == "stdout");
            if(stdout == null)
            {
                stdoutPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                stdoutPanel.Text = stdout;
                stdoutPanel.Visibility = Visibility.Visible;
            }
            if (exception != null)
            {
                resultPanel.Text = exception.Message;
                resultPanel.Foreground = Brushes.Red;
                resultPanel.Visibility = Visibility.Visible;
            }
            else if (result.ReturnValue != null)
            {
                resultPanel.Text = result.ReturnValue.ToString();
                resultPanel.Foreground = Brushes.White;
                resultPanel.Visibility = Visibility.Visible;
            }
            else
            {
                resultPanel.Visibility = Visibility.Collapsed;
                resultPanel.Text = null;
            }
        }

        private async Task WarmpUp()
        {
            // import system (useful), and run something that both prints to stdout and returns a value.
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
