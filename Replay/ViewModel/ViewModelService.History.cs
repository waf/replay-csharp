using Replay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Replay.ViewModel
{
    partial class ViewModelService
    {
        private void CycleThroughHistory(ReplViewModel model, LineEditorViewModel lineEditorViewModel, int previousLinePointer, int delta)
        {
            var prospectiveLineIndex = model.FocusIndex + previousLinePointer + delta;

            if (prospectiveLineIndex < 0)
            {
                model.CycleHistoryLinePointer = 1 - model.Entries.Count;
            }
            else if (prospectiveLineIndex >= model.Entries.Count - 1)
            {
                model.CycleHistoryLinePointer = 0;
                lineEditorViewModel.Document.Text = string.Empty;
            }
            else
            {
                model.CycleHistoryLinePointer = previousLinePointer + delta;
                lineEditorViewModel.Document.Text = model.Entries[prospectiveLineIndex].Document.Text;
            }
        }

        private void ClearScreen(ReplViewModel model)
        {
            model.MinimumFocusIndex = model.FocusIndex;
            model.FocusIndex = model.Entries.Count - 1;
            foreach (var entry in model.Entries.SkipLast(1))
            {
                entry.IsVisible = false;
            }
        }
    }
}
