using Replay.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Replay.Services.SessionSavers;
using System.Threading;
using NSubstitute;
using NSubstitute.Extensions;

namespace Replay.Tests.Services.SessionSavers
{
    public class MarkdownSessionSaverTest
    {
        private readonly MarkdownSessionSaver markdownSaver;
        private string writtenText;

        public MarkdownSessionSaverTest()
        {
            var io = Substitute.ForPartsOf<RealFileIO>();
            io
                .Configure()
                .WriteAllLinesAsync(default, default, default, default)
                .ReturnsForAnyArgs(Task.CompletedTask)
                .AndDoes(call => CaptureWrittenText(
                    call.ArgAt<string>(0),
                    call.ArgAt<IEnumerable<string>>(1),
                    call.ArgAt<Encoding>(2),
                    call.ArgAt<CancellationToken>(3)
                ));

            this.markdownSaver = new MarkdownSessionSaver(io);

            Task CaptureWrittenText(string path, IEnumerable<string> text, Encoding encoding, CancellationToken cancellationToken = default)
            {
                writtenText = string.Join(Environment.NewLine, text);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SaveSessionAsync_WithLeadingComment_ExportsMarkdown()
        {
            await this.markdownSaver.SaveAsync(
                "MyFile.md",
                new[]
                {
                    new LineToSave(
                        input: @"// Here is a variable" + Environment.NewLine + 
                               @"var a = 5;" + Environment.NewLine + 
                               @"Console.WriteLine(6);" + Environment.NewLine + 
                               @"a",
                        result: "5",
                        output: "6",
                        error: "Unexpected level of awesomeness")
                }
            );

            var expected = string.Join(Environment.NewLine,
                "---",
               $"date: {DateTime.Now:MMMM d, yyyy}",
                "---",
                "",
                "# Replay Session",
                "",
                "Here is a variable",
                "",
                "```csharp",
                "var a = 5;",
                "Console.WriteLine(6);",
                "a",
                "```",
                "> ERROR: Unexpected level of awesomeness",
                "> Result: 5",
                "> Output: 6"
            );

            Assert.Equal(expected, writtenText);
        }
    }
}
