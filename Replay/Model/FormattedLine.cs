namespace Replay.Model
{
    internal class FormattedLine
    {
        public string Input { get; }
        public string Result { get; }
        public string Exception { get; }
        public string StandardOutput { get; }

        public FormattedLine(string input, string result, string error, string standardOutput)
        {
            this.Input = input;
            this.Result = result;
            this.Exception = error;
            this.StandardOutput = standardOutput;
        }
    }
}