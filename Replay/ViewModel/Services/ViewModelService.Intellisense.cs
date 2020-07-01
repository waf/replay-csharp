using System.Linq;
using System.Threading.Tasks;

namespace Replay.ViewModel.Services
{
    partial class ViewModelService
    {
        private async Task CompleteCode(WindowViewModel windowvm, LineViewModel linevm)
        {
            var completions = await services.CompleteCodeAsync(linevm.Id, linevm.Document.Text, linevm.CaretOffset);

            if (completions.Any())
            {
                windowvm.Intellisense.IsOpen = true;
                linevm.TriggerIntellisense(completions);
            }
        }
    }
}
