using Microsoft.CodeAnalysis;

namespace Replay.Model
{
    /// <summary>
    /// A submission of a code to the REPL.
    /// This is used for Roslyn APIs that require a Document (and backing Project, Workspace, etc).
    /// It's surprisingly not used for the Roslyn's Scripting API, which works on strings.
    /// </summary>
    public class ReplSubmission
    {
        public Document Document { get; set; }
        public string Code { get; set; }
    }
}
