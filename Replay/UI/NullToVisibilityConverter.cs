using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Replay.UI
{
    /// <summary>
    /// Converts a null object to Visibility.Collapsed.
    /// Note that we can't use a style instead, because `TextBox.Text = null` sets TextBox.Text to empty string.
    /// We want to hide when Text is null, but show when Text is empty string.
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
