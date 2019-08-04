namespace Replay.Model
{
    internal class LineEvaluationResult
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
            this.FormattedInput = formattedInput;
            this.Result = result;
            this.Exception = error;
            this.StandardOutput = standardOutput;
        }
    }
}