using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Replay.Services;
using Replay.Services.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Replay.ViewModel
{
    /// <summary>
    /// ViewModel for a single line of the repl.
    /// Child of <see cref="WindowViewModel"/>
    /// </summary>
    public class LineViewModel : INotifyPropertyChanged
    {
        public Guid Id { get; } = Guid.NewGuid();

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
        // so instead we expose them as properties on our view model.
        private TextEditor editor;
        public void SetEditor(TextEditor lineEditor) =>
            this.editor = lineEditor;
        public int SelectionStart => editor?.SelectionStart ?? 0;
        public int SelectionLength => editor?.SelectionLength ?? 0;
        public int CaretOffset
        {
            get => editor?.CaretOffset ?? 0;
            set
            {
                if (editor is null) return;
                editor.CaretOffset = value;
            }
        }

        public TriggerIntellisense TriggerIntellisense { get; set; }

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

    public delegate void TriggerIntellisense(IReadOnlyList<ReplCompletion> completions);
}
