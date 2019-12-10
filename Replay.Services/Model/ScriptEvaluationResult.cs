using Microsoft.CodeAnalysis.Scripting;
using System;

namespace Replay.Services.Model
{
    /// <summary>
    /// The output of a <see cref="ReplSubmission"/>
    /// <see cref="LineEvaluationResult"/> for a user-friendly view.
    /// </summary>
    public class ScriptEvaluationResult
    {
        /// <summary>
        /// Result of the program
        /// </summary>
        public ScriptState<object> ScriptResult { get; set; }

        /// <summary>
        /// Any errors when compiling or running the program
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Standard Output (i.e. stdout, console output) of the program
        /// </summary>
        public string StandardOutput { get; set; }
    }
}
