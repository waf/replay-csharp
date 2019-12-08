using ICSharpCode.AvalonEdit;
using Replay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replay.ViewModel
{
    partial class ViewModelService
    {

        private async Task CompleteCode(ReplViewModel model, LineEditorViewModel line)
        {
            var completions = await services.CompleteCodeAsync(line.Id, line.Document.Text, line.CaretOffset);

            if (completions.Any())
            {
                model.IsIntellisenseWindowOpen = true;
                line.TriggerIntellisense(completions, () => model.IsIntellisenseWindowOpen = false);
            }
        }
    }
}
