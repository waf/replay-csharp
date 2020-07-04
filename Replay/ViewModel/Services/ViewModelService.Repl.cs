using Replay.Logging;
using Replay.Services.Model;
using System;
using System.Threading.Tasks;

namespace Replay.ViewModel.Services
{
    partial class ViewModelService
    {
        private enum LineOperation
        {
            NoEvaluate = 0,
            Evaluate = 1,
            Reevaluate = 2 | Evaluate,
        }

        /// <summary>
        /// The main REPL loop
        /// </summary>
        /// <param name="linevm">Current line being evaluated</param>
        /// <param name="stayOnCurrentLine">whether or not to progress to the next line of the REPL</param>
        private async Task ReadEvalPrintLoop(WindowViewModel windowvm, LineViewModel linevm, LineOperation mode)
        {
            // read
            string text = string.Empty;
            if(mode.HasFlag(LineOperation.Evaluate))
            {
                ClearPreviousOutput(linevm);
                text = linevm.Document.Text;
            }

            // eval
            var result = await services.AppendEvaluationAsync(linevm.Id, text, new Logger(linevm));
            if (mode.HasFlag(LineOperation.Evaluate))
            {
                if (result == LineEvaluationResult.IncompleteInput)
                {
                    linevm.Document.Text += Environment.NewLine;
                    return;
                }
                if (!string.IsNullOrEmpty(result.FormattedInput))
                {
                    linevm.Document.Text = result.FormattedInput;
                }
                // print
                if (result != LineEvaluationResult.NoOutput)
                {
                    Print(linevm, result);
                }
            }

            // loop
            if (!mode.HasFlag(LineOperation.Reevaluate) && result.Exception == null)
            {
                var newLineId = MoveToNextLine(windowvm, linevm);
                if(newLineId.HasValue)
                {
                    _ = services.AppendEvaluationAsync(newLineId.Value, "", new NullLogger()); // run empty evaluation to create a corresponding compilation in roslyn
                }
            }
        }

        private static void ClearPreviousOutput(LineViewModel linevm) =>
            linevm.StandardOutput = linevm.Error = linevm.Result = string.Empty;

        private static void Print(LineViewModel linevm, LineEvaluationResult result) =>
            linevm.SetResult(result);

        private static int ResetHistoryCyclePointer(WindowViewModel windowvm)
        {
            int previousLinePointer = windowvm.CycleHistoryLinePointer;
            windowvm.CycleHistoryLinePointer = 0;
            return previousLinePointer;
        }

        private static Guid? MoveToNextLine(WindowViewModel windowvm, LineViewModel linevm)
        {
            int currentIndex = windowvm.Entries.IndexOf(linevm);

            LineViewModel newLine = null;
            if (currentIndex == windowvm.Entries.Count - 1)
            {
                newLine = new LineViewModel();
                windowvm.Entries.Add(newLine);
            }

            windowvm.FocusIndex = currentIndex + 1;
            return newLine?.Id;
        }
    }
}
