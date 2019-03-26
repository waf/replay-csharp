using System.Windows.Media;

namespace Replay.Services
{
    public class UserConfiguration
    {
        public UserConfiguration(Color backgroundColor, Color foregroundColor)
        {
            BackgroundColor = backgroundColor;
            ForegroundColor = foregroundColor;
        }

        public Color BackgroundColor { get; }
        public Color ForegroundColor { get; }
    }
}