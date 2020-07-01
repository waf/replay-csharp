namespace Replay.ViewModel
{
    public class IntellisenseViewModel
    {
        private readonly WindowViewModel windowvm;

        public IntellisenseViewModel(WindowViewModel windowvm)
        {
            this.windowvm = windowvm;
        }

        /// <summary>
        /// Zoom level of intellisense window. Locked to zoom level of main window.
        /// </summary>
        public double Zoom => windowvm.Zoom;

        /// <summary>
        /// Tracks if the intellisense window is open. Keyboard shortcuts
        /// behave differently if it's open, because all input is forwarded
        /// to the intelliense window.
        /// </summary>
        public bool IsOpen { get; set; }
    }
}
