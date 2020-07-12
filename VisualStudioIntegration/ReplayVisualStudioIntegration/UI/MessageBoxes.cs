using System.Windows.Forms;

namespace ReplayVisualStudioIntegration.UI
{
    class MessageBoxes
    {
        public void ReplayExeNotFound() =>
            ShowOKMessageBox(
                "Replay.exe could not be found",
                "Please configure Replay.exe's location in Tools → Options → Replay."
            );

        internal void CouldNotStartReplay() =>
            ShowOKMessageBox(
                "Could not start Replay",
                "Replay.exe was successfully located but no listening pipe could be found."
            );


        private void ShowOKMessageBox(string title, string message) =>
            MessageBox.Show(message, title);
    }
}
