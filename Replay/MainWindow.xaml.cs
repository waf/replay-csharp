using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using Replay.Logging;
using Replay.ViewModel;
using Replay.Services;
using Replay.UI;
using Replay.ViewModel.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Replay
{
    /// <summary>
    /// Represents a single REPL window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WindowViewModel Model;
        private readonly ViewModelService viewModelService; // "front-end" services that manipulate the viewmodel.
        private readonly IReplServices replServices; // "back-end" services that handle inspection / evaluation of code.

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = Model = new WindowViewModel();
            this.replServices = new ReplServices(new RealFileIO());
            this.viewModelService = new ViewModelService(replServices);

            replServices.UserConfigurationLoaded += ConfigureWindow;
            Task.Run(BackgroundInitializationAsync);
        }

        /// <summary>
        /// Callback for when user settings are loaded
        /// </summary>
        private void ConfigureWindow(object sender, UserConfiguration configuration)
        {
            var dispatcher = Application.Current?.Dispatcher
                ?? Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(() =>
            {
                Model.Background = new SolidColorBrush(configuration.BackgroundColor);
                Model.Foreground = new SolidColorBrush(configuration.ForegroundColor);
            });
        }

        private void TextEditor_Initialized(TextEditor lineEditor, EventArgs _)
        {
            PromptAdorner.AddTo(lineEditor);
            lineEditor.TextArea.MouseWheel += TextArea_MouseWheel;

            var line = lineEditor.ViewModel();
            line.SetEditor(lineEditor);
            line.TriggerIntellisense = (completions) =>
                new IntellisenseWindow(this.Model.Intellisense, lineEditor.TextArea, completions);
            lineEditor.TextArea.TextView.LineTransformers.Add(
                new AvalonSyntaxHighlightTransformer(replServices, line.Id)
            );
        }


        private void TextEditor_Loaded(TextEditor lineEditor, RoutedEventArgs _)
        {
            if (lineEditor.ViewModel().IsFocused)
            {
                Keyboard.Focus(lineEditor.TextArea);
            }
        }

        private async void TextEditor_PreviewKeyDown(TextEditor lineEditor, KeyEventArgs e)
        {
            await viewModelService.HandleKeyDown(Model, lineEditor.ViewModel(), e);
        }

        private async void TextEditor_PreviewKeyUp(TextEditor lineEditor, KeyEventArgs e)
        {
            await viewModelService.HandleKeyUp(Model, lineEditor.ViewModel(), e);
        }

        private void TextEditor_Unloaded(TextEditor lineEditor, RoutedEventArgs _)
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

        private void TextEditor_GotFocus(TextEditor lineEditor, RoutedEventArgs _)
        {
            // update our viewmodel if the user manually focuses a lineEditor (e.g. with the mouse).
            int newIndex = Model.Entries.IndexOf(lineEditor.ViewModel());
            if (Model.FocusIndex != newIndex)
            {
                Model.FocusIndex = newIndex;
            }
        }

        private void Window_PreviewMouseWheel(Window _, MouseWheelEventArgs e)
        {
            viewModelService.HandleWindowScroll(Model, Keyboard.Modifiers, e);
        }

        private void Scroll_ScrollChanged(ScrollViewer sender, ScrollChangedEventArgs e)
        {
            bool shouldAutoScrollToBottom = sender.Tag == null || (bool)sender.Tag;
            // set autoscroll mode when user scrolls, and store it in the ScrollViewer's Tag.
            if (e.ExtentHeightChange == 0)
            {
                shouldAutoScrollToBottom = sender.VerticalOffset == sender.ScrollableHeight;
                sender.Tag = shouldAutoScrollToBottom;
            }

            // autoscroll to bottom
            if (shouldAutoScrollToBottom && e.ExtentHeightChange != 0)
            {
                sender.ScrollToEnd();
            }
        }

        /// <summary>
        /// Roslyn can be a little bit slow to warm up, which can cause lag when the
        /// user first starts typing / evaluating code. Do the warm up in a background
        /// thread beforehand to improve the user experience.
        /// </summary>
        private Task BackgroundInitializationAsync()
        {
            const string usingCode = @"using System;";
            const string evaluationCode = @"Console.WriteLine(""Hello"");";
            const string completionCode = @"""World""";
            return Task.WhenAll(
                replServices.HighlightAsync(Guid.Empty, usingCode),
                replServices.AppendEvaluationAsync(Guid.Empty, evaluationCode, new NullLogger()),
                replServices.CompleteCodeAsync(Guid.Empty, completionCode, completionCode.Length)
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
        private void Scroll_ScrollChanged(object sender, ScrollChangedEventArgs e) => Scroll_ScrollChanged((ScrollViewer)sender, e);
        #endregion
    }
}