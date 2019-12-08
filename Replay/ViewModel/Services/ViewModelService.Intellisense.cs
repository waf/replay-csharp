using Replay.Model;
using System.Linq;
using System.Threading.Tasks;

namespace Replay.ViewModel.Services
{
    partial class ViewModelService
    {
        private async Task CompleteCode(WindowViewModel model, LineViewModel line)
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
