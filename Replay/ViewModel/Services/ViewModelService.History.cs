using System.Linq;

namespace Replay.ViewModel.Services
{
    partial class ViewModelService
    {
        private void CycleThroughHistory(WindowViewModel windowvm, LineViewModel linevm, int previousLinePointer, int delta)
        {
            var prospectiveLineIndex = windowvm.FocusIndex + previousLinePointer + delta;

            if (prospectiveLineIndex < 0)
            {
                windowvm.CycleHistoryLinePointer = 1 - windowvm.Entries.Count;
            }
            else if (prospectiveLineIndex >= windowvm.Entries.Count - 1)
            {
                windowvm.CycleHistoryLinePointer = 0;
                linevm.Document.Text = string.Empty;
            }
            else
            {
                windowvm.CycleHistoryLinePointer = previousLinePointer + delta;
                linevm.Document.Text = windowvm.Entries[prospectiveLineIndex].Document.Text;
            }
        }

        private void ClearScreen(WindowViewModel windowvm)
        {
            windowvm.MinimumFocusIndex = windowvm.FocusIndex;
            windowvm.FocusIndex = windowvm.Entries.Count - 1;
            foreach (var entry in windowvm.Entries.SkipLast(1))
            {
                entry.IsVisible = false;
            }
        }
    }
}
