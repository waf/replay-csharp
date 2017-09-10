using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Replay.Model
{
    public class ReplModel : INotifyPropertyChanged
    {
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
                    SetField(ref focusIndex, value);
                    focusIndex = value;
                    OnPropertyChanged(nameof(FocusIndex));
                    return;
                }

                if (focusIndex == value)
                {
                    return;
                }

                var oldFocusedItem = Entries[focusIndex.Value];
                var newFocusedItem = Entries[value];

                oldFocusedItem.IsFocused = false;
                newFocusedItem.IsFocused = true;

                focusIndex = value;
                OnPropertyChanged(nameof(FocusIndex));
            }
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