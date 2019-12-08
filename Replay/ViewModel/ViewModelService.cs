using Replay.Model;
using Replay.Services;
using Replay.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Replay.ViewModel
{
    partial class ViewModelService
    {
        private readonly ReplServices services;

        public ViewModelService(ReplServices services)
        {
            this.services = services;
        }

        public async Task HandleKeyDown(ReplViewModel model, LineEditorViewModel lineEditor, KeyEventArgs e)
        {
            if (model.IsIntellisenseWindowOpen) return;

            int previousHistoryPointer = ResetHistoryCyclePointer(model);

            if(KeyboardShortcuts.MapToCommand(e) is ReplCommand command)
            {
                e.Handled = await HandleCommand(model, lineEditor, command, previousHistoryPointer);
            }
        }


        public async Task HandleKeyUp(ReplViewModel model, LineEditorViewModel line, KeyEventArgs e)
        {
            if (model.IsIntellisenseWindowOpen) return;

            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.OemPeriod // complete member accesses
                && !IsCompletingDigit()) // but don't complete decimal points in numbers
            {
                await CompleteCode(model, line);
            }

            bool IsCompletingDigit()
            {
                string text = line.Document.Text;
                return text.Length >= 2 && Char.IsDigit(text[text.Length - 2]);
            }
        }

        private async Task<bool> HandleCommand(ReplViewModel model, LineEditorViewModel lineEditor, ReplCommand cmd, int previousHistoryPointer)
        {
            switch (cmd)
            {
                case ReplCommand.EvaluateCurrentLine:
                    await ReadEvalPrintLoop(model, lineEditor, stayOnCurrentLine: false);
                    return true;
                case ReplCommand.ReevaluateCurrentLine:
                    await ReadEvalPrintLoop(model, lineEditor, stayOnCurrentLine: true);
                    return true;
                case ReplCommand.CyclePreviousLine:
                    CycleThroughHistory(model, lineEditor, previousHistoryPointer, -1);
                    return true;
                case ReplCommand.CycleNextLine:
                    CycleThroughHistory(model, lineEditor, previousHistoryPointer, +1);
                    return true;
                case ReplCommand.OpenIntellisense:
                    await CompleteCode(model, lineEditor);
                    return true;
                case ReplCommand.GoToFirstLine:
                    model.FocusIndex = 0;
                    return true;
                case ReplCommand.GoToLastLine:
                    model.FocusIndex = model.Entries.Count - 1;
                    return true;
                case ReplCommand.LineDown when lineEditor.IsCaretOnFinalLine():
                    model.FocusIndex++;
                    return true;
                case ReplCommand.LineUp when lineEditor.IsCaretOnFirstLine():
                    model.FocusIndex--;
                    return true;
                case ReplCommand.ClearScreen:
                    ClearScreen(model);
                    return true;
                case ReplCommand.SaveSession:
                    await new SaveDialog(services).SaveAsync(model.Entries);
                    return true;
                case ReplCommand.LineUp:
                case ReplCommand.LineDown:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException("Unknown command " + cmd);
            }
        }
    }
}
