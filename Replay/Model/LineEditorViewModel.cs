using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Replay.Model
{
    /// <summary>
    /// ViewModel for a single line of the repl.
    /// Child of <see cref="ReplViewModel"/>
    /// </summary>
    public class LineEditorViewModel : INotifyPropertyChanged
    {
        private static int incrementingId = 0;

        public int Id { get; } = Interlocked.Increment(ref incrementingId);

        bool isFocused;
        public bool IsFocused
        {
            get => isFocused;
            set => SetPropertyChanged(ref isFocused,  value);
        }

        // the input document of the current line editor.
        TextDocument document;
        public TextDocument Document
        {
            get => document;
            set => SetPropertyChanged(ref document,  value);
        }

        string result;
        public string Result
        {
            get => result;
            set => SetPropertyChanged(ref result,  value);
        }

        string error;
        public string Error
        {
            get => error;
            set => SetPropertyChanged(ref error,  value);
        }

        string output;
        public string StandardOutput
        {
            get => output;
            set => SetPropertyChanged(ref output,  value);
        }

        public void SetResult(EvaluationResult result)
        {
            this.Error = result.Exception?.Message;
            this.Result = result.ScriptResult?.ReturnValue?.ToString();
            this.StandardOutput = result.StandardOutput;
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
