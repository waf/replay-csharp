using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Replay.ViewModel
{
    /// <summary>
    /// Root of the ViewModel -- one per repl window.
    /// </summary>
    public class WindowViewModel : INotifyPropertyChanged
    {
        public WindowViewModel()
        {
            FocusIndex = 0; // trigger focus on application start
            Intellisense = new IntellisenseViewModel(this);
        }

        /// <summary>
        /// Specifies whether a window is minimized, maximized, or restored.
        /// </summary>
        public WindowState WindowState
        {
            get => windowState;
            set => SetField(ref windowState, value);
        }
        private WindowState windowState;

        /// <summary>
        /// Background color
        /// </summary>
        public SolidColorBrush Background
        {
            get => background;
            set => SetField(ref background, value);
        }
        private SolidColorBrush background;

        /// <summary>
        /// Foreground color
        /// </summary>
        public SolidColorBrush Foreground
        {
            get => foreground;
            set => SetField(ref foreground, value);
        }
        private SolidColorBrush foreground;

        /// <summary>
        /// Lines in the REPL. Each line consists of input and output for that line.
        /// </summary>
        public ObservableCollection<LineViewModel> Entries { get; } =
            new ObservableCollection<LineViewModel> { new LineViewModel() };

        public double Zoom
        {
            get => zoom;
            set => SetField(ref zoom, value);
        }
        private double zoom = 1;

        /// <summary>
        /// The index of the REPL entry that is currently focused
        /// </summary>
        public int FocusIndex
        {
            get => focusIndex.GetValueOrDefault(0);
            set
            {
                if (value < MinimumFocusIndex || value == Entries.Count)
                {
                    return;
                }

                if (!focusIndex.HasValue)
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
        private int? focusIndex;

        /// <summary>
        /// The minimum allowed focus. This is greater than zero when
        /// the user has cleared the screen.
        /// </summary>
        public int MinimumFocusIndex { get; set; }

        /// <summary>
        /// As the user presses "alt-up/down" to cycle through their history,
        /// this index points to the current historical repl entry.
        /// Starts at 0, decrements to -1, -2, -3... etc as the user
        /// cycles back through their history.
        /// </summary>
        public int CycleHistoryLinePointer { get; set; }

        /// <summary>
        /// Popup intellisense window
        /// </summary>
        public IntellisenseViewModel Intellisense { get; }

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