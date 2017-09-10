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
        private SyntaxHighlighter syntaxHighlighter = new SyntaxHighlighter();
        private ScriptEvaluator scriptEvaluator = new ScriptEvaluator();
        private ReplModel Model = new ReplModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = Model;
            Model.FocusIndex = 0;
            Task.Run(WarmpUp);
        }

        private void TextEditor_Initialized(object sender, EventArgs e) =>
            AvalonSyntaxHighlightTransformer.Register((TextEditor)sender, syntaxHighlighter);

        private async void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                var repl = (TextEditor)sender;
                ScriptState<object> result = null;
                Exception evaluatorException = null;
                StringBuilder consoleOutput = new StringBuilder();
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
                if(exception == null)
                {
                    int index = Model.Entries.IndexOf((ReplResult)repl.DataContext);
                    if(index == Model.Entries.Count - 1)
                    {
                        Model.Entries.Add(new ReplResult());
                    }
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    {
                        Model.Entries.Insert(index + 1, new ReplResult());
                    }
                    Model.FocusIndex = index + 1;
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
            }
            else if (result.ReturnValue != null)
            {
                resultPanel.Text = result.ReturnValue.ToString();
                resultPanel.Foreground = Brushes.White;
            }
            else
            {
                resultPanel.Text = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

        private void MinButton_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = WindowState.Minimized;

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaxButton.Content = "O";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaxButton.Content = "o";
            }
        }

        private async Task WarmpUp()
        {
            const string code = @"""Hello World""";
            syntaxHighlighter.Highlight(code);
            await scriptEvaluator.Evaluate(code);
        }
    }
}
