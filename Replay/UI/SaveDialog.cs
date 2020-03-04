using Microsoft.Win32;
using Replay.ViewModel;
using Replay.Services;
using Replay.Services.SessionSavers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Replay.UI
{
    class SaveDialog
    {
        private readonly IReplServices replServices;

        // these fields only exist to abstract away WPF dialogs for testability.
        private readonly ShowSaveDialog showSaveDialog;
        private readonly Action<string> showMessageDialog;

        public SaveDialog(
            IReplServices replServices,
            ShowSaveDialog showSaveDialog = null,
            Action<string> showMessageDialog = null)
        {
            this.replServices = replServices;
            this.showSaveDialog = showSaveDialog ?? ((string saveFormats) =>
            {
                var saveFileDialog = new SaveFileDialog { Filter = saveFormats };
                var result = saveFileDialog.ShowDialog();
                return new SaveDialogResult(result, saveFileDialog.FileName, saveFileDialog.FilterIndex);
            });
            this.showMessageDialog = showMessageDialog ?? (text => MessageBox.Show(text));
        }

        public async Task SaveAsync(IReadOnlyCollection<LineViewModel> lines)
        {
            var supportedSaveFormats = await replServices.GetSupportedSaveFormats();
            var saveInput = showSaveDialog(string.Join("|", supportedSaveFormats));

            if (saveInput.SaveConfirmed.GetValueOrDefault(false))
            {
                var message = await replServices.SaveSessionAsync(
                    filename: saveInput.FileName,
                    fileFormat: supportedSaveFormats[saveInput.FilterIndex - 1], // 1-based index? really?
                    linesToSave: ConvertToSaveModel(lines)
                );

                showMessageDialog(message);
            }
        }

        private static IReadOnlyCollection<LineToSave> ConvertToSaveModel(IReadOnlyCollection<LineViewModel> lines) => lines
            .Select(line => new LineToSave(
                line.Document.Text,
                line.Result,
                line.StandardOutput,
                line.Error)
            )
            .ToList();

        public delegate SaveDialogResult ShowSaveDialog(string saveFormats);

        public class SaveDialogResult
        {
            public SaveDialogResult(bool? saveConfirmed, string fileName, int filterIndex)
            {
                SaveConfirmed = saveConfirmed;
                FileName = fileName;
                FilterIndex = filterIndex;
            }

            public bool? SaveConfirmed { get; }
            public string FileName { get; }
            public int FilterIndex { get; }
        }
    }
}
