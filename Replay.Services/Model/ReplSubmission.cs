using Microsoft.CodeAnalysis;

namespace Replay.Services.Model
{
    /// <summary>
    /// A submission of a code to the REPL.
    /// This is used for Roslyn APIs that require a Document (and backing Project, Workspace, etc).
    /// It's surprisingly not used for the Roslyn's Scripting API, which works on strings.
    /// </summary>
    public class ReplSubmission
    {
        public ReplSubmission(string code, Document document)
        {
            Code = code;
            Document = document;
        }

        public string Code { get; }
        public Document Document { get; }

        public override string ToString() =>
            Document.Id + ": " + Code;
    }
}
