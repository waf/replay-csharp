namespace Replay.Model
{
    internal class LineOutput
    {
        public string Result { get; }
        public string Exception { get; }
        public string StandardOutput { get; }

        public LineOutput(string result, string error, string standardOutput)
        {
            this.Result = result;
            this.Exception = error;
            this.StandardOutput = standardOutput;
        }
    }
}