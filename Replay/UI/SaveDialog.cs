using Microsoft.Win32;
using Replay.Model;
using Replay.Services;
using Replay.Services.SessionSavers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Replay.UI
{
    class SaveDialog
    {
        private readonly ReplServices replServices;

        public SaveDialog(ReplServices replServices)
        {
            this.replServices = replServices;
        }

        public async Task SaveAsync(IEnumerable<LineViewModel> lines)
        {
            var supportedSaveFormats = await replServices.GetSupportedSaveFormats();
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = string.Join("|", supportedSaveFormats)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var message = await replServices.SaveSessionAsync(
                    filename: saveFileDialog.FileName,
                    fileFormat: supportedSaveFormats[saveFileDialog.FilterIndex - 1], // 1-based index? really?
                    linesToSave: ConvertToSaveModel(lines)
                );

                MessageBox.Show(message);
            }
        }

        private static IReadOnlyCollection<LineToSave> ConvertToSaveModel(IEnumerable<LineViewModel> lines) => lines
            .Select(line => new LineToSave(
                line.Document.Text,
                line.Result,
                line.StandardOutput,
                line.Error)
            )
            .ToList();
    }
}
