using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Replay.Model
{
    public class ReplModel : INotifyPropertyChanged
    {
        private WindowState windowState;
        public WindowState WindowState
        {
            get => windowState;
            set => SetField(ref windowState, value);
        }

        public ObservableCollection<ReplResult> Entries { get; } =
            new ObservableCollection<ReplResult> { new ReplResult() };


        private int? focusIndex;
        public int FocusIndex
        {
            get => focusIndex.GetValueOrDefault(0);
            set
            {
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