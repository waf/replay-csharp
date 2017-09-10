using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Replay.Model
{
    public class ReplResult : INotifyPropertyChanged
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

        string output;
        public string Output
        {
            get => output;
            set => SetField(ref output,  value);
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
