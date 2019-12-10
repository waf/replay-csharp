using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Formatting;
using Replay.Services.Model;
using System.Threading.Tasks;

namespace Replay.Services
{
    /// <summary>
    /// Pretty-Printer for printing C# objects as strings
    /// </summary>
    class PrettyPrinter
    {
        private readonly CSharpObjectFormatter objectFormatter;

        public PrettyPrinter()
        {
            this.objectFormatter = CSharpObjectFormatter.Instance;
        }

        public async Task<LineEvaluationResult> FormatAsync(Document document, ScriptEvaluationResult evaluationResult)
        {
            // format the input
            var formattedDocument = await Formatter.FormatAsync(document);
            var formattedText = await formattedDocument.GetTextAsync();

            // format the output
            return new LineEvaluationResult(
                formattedText.ToString(),
                FormatObject(evaluationResult?.ScriptResult?.ReturnValue),
                evaluationResult?.Exception?.Message,
                evaluationResult?.StandardOutput
            );
        }

        private string FormatObject(object obj)
        {
            if (obj is null)
            {
                // right now there's no way to determine the difference between "no value" and "null value"
                // intercept all nulls and return null, instead of the string "null"
                // because otherwise every single assignment expression would print "null"
                return null;
            }
            return objectFormatter.FormatObject(obj);
        }
    }
}
