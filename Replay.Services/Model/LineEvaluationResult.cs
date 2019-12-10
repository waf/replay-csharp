namespace Replay.Services.Model
{
    /// <summary>
    /// The result of an evaluation
    /// </summary>
    public class LineEvaluationResult
    {
        public string FormattedInput { get; }
        public string Result { get; }
        public string Exception { get; }
        public string StandardOutput { get; }

        public static readonly LineEvaluationResult IncompleteInput = new LineEvaluationResult();
        public static readonly LineEvaluationResult NoOutput = new LineEvaluationResult();

        public LineEvaluationResult() { }

        public LineEvaluationResult(string formattedInput, string result, string error, string standardOutput)
        {
            FormattedInput = formattedInput;
            Result = result;
            Exception = error;
            StandardOutput = standardOutput;
        }
    }
}