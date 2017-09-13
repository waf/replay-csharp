using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replay.Model
{
    public class EvaluationResult
    {
        /// <summary>
        /// Result of the program
        /// </summary>
        public ScriptState<Object> ScriptResult { get; set; }

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
