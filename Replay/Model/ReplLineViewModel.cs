using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Replay.Model
{
    /// <summary>
    /// ViewModel for a single line of the repl.
    /// Child of <see cref="ReplViewModel"/>
    /// </summary>
    public class ReplLineViewModel : INotifyPropertyChanged
    {
        bool isFocused;
        public bool IsFocused
        {
            get => isFocused;
            set => SetField(ref isFocused,  value);
        }

        string input;
        public string Input
        {
            get => input;
            set => SetField(ref input,  value);
        }

        string result;
        public string Result
        {
            get => result;
            set => SetField(ref result,  value);
        }

        string error;
        public string Error
        {
            get => error;
            set => SetField(ref error,  value);
        }

        string output;
        public string StandardOutput
        {
            get => output;
            set => SetField(ref output,  value);
        }

        public void SetResult(EvaluationResult result)
        {
            this.Result = result.ScriptResult?.ReturnValue?.ToString();
            this.Error = result.Exception?.Message;
            this.StandardOutput = result.StandardOutput;
        }

        #region INotifyPropertyChanged Helpers
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
