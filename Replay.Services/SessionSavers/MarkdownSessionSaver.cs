using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replay.Services.SessionSavers
{
    /// <summary>
    /// Saves the user's session as a markdown file, with the REPL lines
    /// as code blocks, and leading comments as plain text.
    /// </summary>
    public class MarkdownSessionSaver : ISessionSaver
    {
        private readonly IFileIO io;

        public MarkdownSessionSaver(IFileIO io)
        {
            this.io = io;
        }

        public string SaveFormat { get; } = "Markdown File (*.md)|*.md";

        public async Task<string> SaveAsync(string fileName, IReadOnlyCollection<LineToSave> linesToSave)
        {
            var lines = GenerateMarkdownLines(linesToSave);
            await io.WriteAllLinesAsync(
                fileName,
                lines,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            );
            return "Session has been saved as a markdown file.";
        }

        private IEnumerable<string> GenerateMarkdownLines(IReadOnlyCollection<LineToSave> linesToSave)
        {
            yield return "---";
            yield return $"date: {DateTime.Now:MMMM d, yyyy}";
            yield return "---";
            yield return "";
            yield return "# Replay Session";

            foreach (var line in linesToSave)
            {
                var lines = line.Input.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                // if there are leading comments, treat them as text
                foreach (var comment in ConvertLeadingCommentsToMarkdownText(lines))
                {
                    yield return comment;
                }

                foreach (var code in ConvertCSharpToMarkdownCode(lines))
                {
                    yield return code;
                }

                if (!string.IsNullOrEmpty(line.Error))
                {
                    yield return "> ERROR: " + line.Error;
                }
                if (!string.IsNullOrEmpty(line.Result))
                {
                    yield return "> Result: " + line.Result;
                }
                if (!string.IsNullOrEmpty(line.Output))
                {
                    yield return "> Output: " + line.Output;
                }
            }
        }

        private static IEnumerable<string> ConvertLeadingCommentsToMarkdownText(IEnumerable<string> lines)
        {
            var comments = lines.TakeWhile(IsComment).ToList();

            foreach (var comment in comments)
            {
                yield return "";
                yield return comment.TrimStart(new[] { '/', ' ' });
            }
        }

        private static IEnumerable<string> ConvertCSharpToMarkdownCode(IEnumerable<string> lines)
        {
            var code = lines.SkipWhile(IsComment).ToList();
            if (!code.Any())
            {
                yield break;
            }

            yield return "";
            yield return "```csharp";
            foreach (var snippet in code)
            {
                yield return snippet;
            }
            yield return "```";
        }

        private static bool IsComment(string line) =>
            line.StartsWith("//"); // this doesn't handle /* block comments */, that might be ok?
    }

    public class LineToSave
    {
        public LineToSave(string input, string result, string output, string error)
        {
            Input = input;
            Result = result;
            Output = output;
            Error = error;
        }

        public string Input { get; }
        public string Result { get; }
        public string Output { get; }
        public string Error { get; }
    }
}
