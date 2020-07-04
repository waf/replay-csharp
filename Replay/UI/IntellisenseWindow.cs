using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis.Text;
using Replay.Services;
using Replay.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Replay.UI
{
    class IntellisenseWindow : CompletionWindow
    {
        private readonly IntellisenseViewModel Model;

        public IntellisenseWindow(IntellisenseViewModel intellisensevm, TextArea textArea, IReadOnlyList<ReplCompletion> completions)
            : base(textArea)
        {
            this.DataContext = Model = intellisensevm;

            if (completions.Count == 0) return;

            TextSpan span = completions[0].CompletionItem.Span;
            string textBeingCompleted = textArea.Document.Text.Substring(span.Start, span.Length);

            SetCompletionBounds(textBeingCompleted, span);
            PopulateCompletionDropDown(completions, textBeingCompleted);

            if (PresentationSource.CurrentSources.OfType<object>().Any()) // render headless if we have no PresentationSource, for unit tests.
            {
                Show();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this.Model.IsOpen = false;
        }

        private void PopulateCompletionDropDown(IReadOnlyList<ReplCompletion> completions, string textBeingCompleted)
        {
            string longestWord = "";
            foreach (var completion in completions)
            {
                var completionItem = new RoslynCompletionSuggestion(completion);
                this.CompletionList.CompletionData.Add(completionItem);
                longestWord = completionItem.Text.Length > longestWord.Length ? completionItem.Text : longestWord;
            }
            this.Width = Math.Max(MeasureWord(longestWord).Width, 100);
            this.CompletionList.IsFiltering = true;
            this.CompletionList.SelectItem(textBeingCompleted);
        }

        private FormattedText MeasureWord(string maxWord) =>
            new FormattedText(
                maxWord,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(CompletionList.FontFamily, CompletionList.FontStyle, CompletionList.FontWeight, CompletionList.FontStretch),
                (this.CompletionList.FontSize + 6) * Model.Zoom, // "6" was unscientifically chosen as a number that isn't too cramped
                Brushes.Black,
                new NumberSubstitution(),
                VisualTreeHelper.GetDpi(this.CompletionList).PixelsPerDip
            );

        private void SetCompletionBounds(string textBeingCompleted, TextSpan span)
        {
            if (span.Start < this.StartOffset && this.StartOffset < span.End)
            {
                // handle when we're completing an already complete word, e.g. Console.WriteLi|ne
                this.StartOffset = span.Start;
                this.EndOffset = span.End;
            }
            else
            {
                // handle when we're completing partially typed word, e.g. Console.WriteLi|
                this.StartOffset -= textBeingCompleted.Length;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            // There's a weird case if the user holds ctrl, holds space, releases ctrl, release space.
            // This can happen if the user types ctrl-space too carelessly.
            // We get a "space" on KeyUp that never passed through key down and is not reflect in the textarea.
            // Ignore that space and any associated ctrl notifications.
            if (e.Key == Key.Space || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                return;
            }

            // close completion window if there is only 1 match that is exactly what the user already typed,
            // and the cursor is at the end of the match.
            string filter = this.TextArea.Document.Text.Substring(this.StartOffset, this.EndOffset - this.StartOffset);
            var matches = this.CompletionList.ListBox.ItemsSource
                .OfType<RoslynCompletionSuggestion>()
                .ToList();

            if (NoMatches() || OneFullyTypedExactMatch())
            {
                this.Close();
            }

            bool NoMatches() => matches.Count == 0;
            bool OneFullyTypedExactMatch() => matches.Count == 1
                && matches[0].Text.Equals(filter, StringComparison.CurrentCulture)
                && this.TextArea.Caret.VisualColumn == this.EndOffset;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.OemPeriod)
            {
                // while autocompleting, the user typed a period. This will trigger a new completion window, so this old one should close.
                this.Close();
            }
            else if (e.Key == Key.Space && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // while autocompleting, the user re-summoned intellisense. Since we're already open, swallow the space and do nothing.
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// A single suggestion in the Intellisense Window
    /// </summary>
    class RoslynCompletionSuggestion : ICompletionData
    {
        public RoslynCompletionSuggestion(ReplCompletion completion)
        {
            Completion = completion;
        }

        public ImageSource Image => null;

        public string Text => Completion.CompletionItem.DisplayText;

        /// <summary>
        /// The UIElement to render
        /// </summary>
        public object Content => Text;

        /// <summary>
        /// Help text in tooltip
        /// </summary>
        public object Description => Completion.QuickInfoTask.Value.Result; // warning, blocking code

        public double Priority => Completion.CompletionItem.Rules.MatchPriority;

        public ReplCompletion Completion { get; }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }
}
