using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Replay.Services;
using Replay.Services.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Replay.Model
{
    /// <summary>
    /// ViewModel for a single line of the repl.
    /// Child of <see cref="WindowViewModel"/>
    /// </summary>
    internal class LineViewModel : INotifyPropertyChanged
    {
        private static int incrementingId = 0;

        public int Id { get; } = Interlocked.Increment(ref incrementingId);

        bool isFocused;
        public bool IsFocused
        {
            get => isFocused;
            set => SetPropertyChanged(ref isFocused, value);
        }

        // the input document of the current line editor.
        TextDocument document;
        public TextDocument Document
        {
            get => document;
            set => SetPropertyChanged(ref document, value);
        }

        string result;
        public string Result
        {
            get => result;
            set => SetPropertyChanged(ref result, value);
        }

        string error;
        public string Error
        {
            get => error;
            set => SetPropertyChanged(ref error, value);
        }

        string output;
        public string StandardOutput
        {
            get => output;
            set => SetPropertyChanged(ref output, value);
        }

        bool isVisible = true;
        public bool IsVisible
        {
            get => isVisible;
            set => SetPropertyChanged(ref isVisible, value);
        }

        // ideally we could databind to these editor properties, but we can't,
        // so instead we expose them as read-only properties on our view model.
        private TextEditor editor;
        public void SetEditor(TextEditor lineEditor) =>
            this.editor = lineEditor;
        public int SelectionStart => editor?.SelectionStart ?? 0;
        public int CaretOffset => editor?.CaretOffset ?? 0;

        public Action<IReadOnlyList<ReplCompletion>, Action> TriggerIntellisense { get; set; }

        public void SetResult(LineEvaluationResult output)
        {
            this.Result = output.Result;
            this.Error = output.Exception;
            this.StandardOutput = output.StandardOutput;
        }

        #region INotifyPropertyChanged Helpers
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetPropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
