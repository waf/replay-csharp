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
        private readonly IReadOnlyList<string> supportedSaveFormats;

        public SaveDialog(ReplServices replServices)
        {
            this.replServices = replServices;
            this.supportedSaveFormats = replServices.GetSupportedSaveFormats();
        }

        public async Task SaveAsync(IEnumerable<LineEditorViewModel> lines)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = string.Join("|", supportedSaveFormats)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string chosenFormat = supportedSaveFormats[saveFileDialog.FilterIndex - 1]; // 1-based index? really?
                var linesToSave = lines
                    .Select(line => new LineToSave(line.Document.Text, line.Result, line.StandardOutput, line.Error))
                    .ToList();
                var message = await replServices.SaveSessionAsync(saveFileDialog.FileName, chosenFormat, linesToSave);

                MessageBox.Show(message);
            }
        }
    }
}
