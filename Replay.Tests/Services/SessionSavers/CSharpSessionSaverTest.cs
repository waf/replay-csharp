using Replay.Services;
using Replay.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Replay.Services.SessionSavers;
using System.Threading;
using NSubstitute;
using NSubstitute.Extensions;

namespace Replay.Tests.Services.SessionSavers
{
    public class CSharpSessionSaverTest : IAsyncLifetime
    {
        private readonly ReplServices replServices;
        private string writtenText;

        public CSharpSessionSaverTest()
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

            this.replServices = new ReplServices(io);

            Task CaptureWrittenText(string path, IEnumerable<string> text, Encoding encoding, CancellationToken cancellationToken = default)
            {
                writtenText = text.Single();
                return Task.CompletedTask;
            }
        }

        // warmup, simulating warmup that the actual application does
        public Task InitializeAsync() => replServices.AppendEvaluationAsync(Guid.Empty, "", new NullLogger());
        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task SaveSessionAsync_HelloWorld_SavesCSharpFile()
        {
            await this.replServices.AppendEvaluationAsync(
                lineId: Guid.NewGuid(),
                code: @"var a = ""Hello World"";"
                    + @"a",
                logger: new NullLogger()
            );

            // system under test
            await this.replServices.SaveSessionAsync("MyTest.cs", "C# File (*.cs)|*.cs", Array.Empty<LineToSave>());

            AssertProgramOutput(new[]
            {
                 @"            var a = ""Hello World"";",
                 @"            /* a */",
            }, writtenText);
        }

        private void AssertProgramOutput(string[] expected, string actual)
        {
            var expectedFullProgram = new[]
            {
                 @"using System;",
                 @"using System.Collections.Generic;",
                 @"using System.Linq;",
                 @"using System.Text;",
                 @"",
                 @"namespace Replay",
                 @"{",
                 @"    public class Program",
                 @"    {",
                 @"        public static void Main()",
                 @"        {",
            }
            .Concat(expected)
            .Concat(new[]
            {
                 @"        }",
                 @"    }",
                 @"}",
            });

            Assert.Equal(
                expected: string.Join(Environment.NewLine, expectedFullProgram),
                actual: actual
            );
        }
    }
}
