using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Replay.Model
{
    /// <summary>
    /// Root of the ViewModel -- one per repl window.
    /// </summary>
    public class ReplViewModel : INotifyPropertyChanged
    {
        public ReplViewModel()
        {
            FocusIndex = 0;
        }

        private WindowState windowState;
        public WindowState WindowState
        {
            get => windowState;
            set => SetField(ref windowState, value);
        }

        public ObservableCollection<LineEditorViewModel> Entries { get; } =
            new ObservableCollection<LineEditorViewModel> { new LineEditorViewModel() };

        private int? focusIndex;
        public int FocusIndex
        {
            get => focusIndex.GetValueOrDefault(0);
            set
            {
                if (value == -1 || value == Entries.Count)
                {
                    return;
                }

                if(!focusIndex.HasValue)
                {
                    Entries[value].IsFocused = true;
                    SetField(ref focusIndex, value, updateOnlyWhenChanged: false);
                    return;
                }

                var oldFocusedItem = Entries[focusIndex.Value];
                var newFocusedItem = Entries[value];

                oldFocusedItem.IsFocused = false;
                newFocusedItem.IsFocused = true;

                SetField(ref focusIndex, value, updateOnlyWhenChanged: false);
            }
        }

        #region INotifyPropertyChanged Helpers
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null, bool updateOnlyWhenChanged = true)
        {
            if (updateOnlyWhenChanged && EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}