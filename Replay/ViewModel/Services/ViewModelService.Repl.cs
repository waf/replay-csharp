using Replay.Logging;
using Replay.Model;
using Replay.Services.Model;
using System;
using System.Threading.Tasks;

namespace Replay.ViewModel.Services
{
    partial class ViewModelService
    {
        /// <summary>
        /// The main REPL loop
        /// </summary>
        /// <param name="line">Current line being evaluated</param>
        /// <param name="stayOnCurrentLine">whether or not to progress to the next line of the REPL</param>
        private async Task ReadEvalPrintLoop(WindowViewModel model, LineViewModel line, bool stayOnCurrentLine)
        {
            ClearPreviousOutput(line);
            // read
            string text = line.Document.Text;

            // eval
            var result = await services.EvaluateAsync(line.Id, text, new Logger(line));
            if (result == LineEvaluationResult.IncompleteInput)
            {
                line.Document.Text += Environment.NewLine;
                return;
            }
            if (!string.IsNullOrEmpty(result.FormattedInput))
            {
                line.Document.Text = result.FormattedInput;
            }

            // print
            if (result != LineEvaluationResult.NoOutput)
            {
                Print(line, result);
            }

            // loop
            if (result.Exception == null && !stayOnCurrentLine)
            {
                var newLineId = MoveToNextLine(model, line);
                if(newLineId.HasValue)
                {
                    _ = services.EvaluateAsync(newLineId.Value, "", new NullLogger()); // run empty evaluation to create a corresponding compilation in roslyn
                }
            }
        }

        private static void ClearPreviousOutput(LineViewModel line) =>
            line.StandardOutput = line.Error = line.Result = string.Empty;

        private static void Print(LineViewModel lineEditor, LineEvaluationResult result) =>
            lineEditor.SetResult(result);


        private static int ResetHistoryCyclePointer(WindowViewModel model)
        {
            int previousLinePointer = model.CycleHistoryLinePointer;
            model.CycleHistoryLinePointer = 0;
            return previousLinePointer;
        }

        private static int? MoveToNextLine(WindowViewModel model, LineViewModel lineEditor)
        {
            int currentIndex = model.Entries.IndexOf(lineEditor);

            LineViewModel newLine = null;
            if (currentIndex == model.Entries.Count - 1)
            {
                newLine = new LineViewModel();
                model.Entries.Add(newLine);
            }

            model.FocusIndex = currentIndex + 1;
            return newLine?.Id;
        }

    }
}
