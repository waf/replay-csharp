using Replay.Model;
using Replay.Services;
using Replay.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Replay.ViewModel.Services
{
    /// <summary>
    /// Handles viewmodel manipulation based on input events.
    /// This partial class is the main entry point for input events, and
    /// routes to the other partial classes for specific functionality.
    /// </summary>
    partial class ViewModelService
    {
        private readonly ReplServices services;

        public ViewModelService(ReplServices services)
        {
            this.services = services;
        }

        public async Task HandleKeyDown(WindowViewModel windowvm, LineViewModel linevm, KeyEventArgs e)
        {
            if (windowvm.IsIntellisenseWindowOpen) return;

            int previousHistoryPointer = ResetHistoryCyclePointer(windowvm);

            if(KeyboardShortcuts.MapToCommand(e) is ReplCommand command)
            {
                e.Handled = await HandleCommand(windowvm, linevm, command, previousHistoryPointer);
            }
        }

        public async Task HandleKeyUp(WindowViewModel windowvm, LineViewModel linevm, KeyEventArgs e)
        {
            if (windowvm.IsIntellisenseWindowOpen) return;

            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.OemPeriod // complete member accesses
                && !IsCompletingDigit()) // but don't complete decimal points in numbers
            {
                await CompleteCode(windowvm, linevm);
            }

            bool IsCompletingDigit()
            {
                string text = linevm.Document.Text;
                return text.Length >= 2 && Char.IsDigit(text[text.Length - 2]);
            }
        }

        private async Task<bool> HandleCommand(WindowViewModel windowvm, LineViewModel linevm, ReplCommand cmd, int previousHistoryPointer)
        {
            switch (cmd)
            {
                case ReplCommand.EvaluateCurrentLine:
                    await ReadEvalPrintLoop(windowvm, linevm, stayOnCurrentLine: false);
                    return true;
                case ReplCommand.ReevaluateCurrentLine:
                    await ReadEvalPrintLoop(windowvm, linevm, stayOnCurrentLine: true);
                    return true;
                case ReplCommand.CyclePreviousLine:
                    CycleThroughHistory(windowvm, linevm, previousHistoryPointer, -1);
                    return true;
                case ReplCommand.CycleNextLine:
                    CycleThroughHistory(windowvm, linevm, previousHistoryPointer, +1);
                    return true;
                case ReplCommand.OpenIntellisense:
                    await CompleteCode(windowvm, linevm);
                    return true;
                case ReplCommand.GoToFirstLine:
                    windowvm.FocusIndex = 0;
                    return true;
                case ReplCommand.GoToLastLine:
                    windowvm.FocusIndex = windowvm.Entries.Count - 1;
                    return true;
                case ReplCommand.LineDown when linevm.IsCaretOnFinalLine():
                    windowvm.FocusIndex++;
                    return true;
                case ReplCommand.LineUp when linevm.IsCaretOnFirstLine():
                    windowvm.FocusIndex--;
                    return true;
                case ReplCommand.ClearScreen:
                    ClearScreen(windowvm);
                    return true;
                case ReplCommand.SaveSession:
                    await new SaveDialog(services).SaveAsync(windowvm.Entries);
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
