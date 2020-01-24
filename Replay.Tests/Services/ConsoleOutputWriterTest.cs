using Replay.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Replay.Tests.Services
{
    public class ConsoleOutputWriterTest
    {
        private readonly ConsoleOutputWriter writer;

        public ConsoleOutputWriterTest()
        {
            this.writer = new ConsoleOutputWriter(captureStandardOut: false);
        }

        [Theory]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.Write), new object[] { 'A' } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.Write), new object[] { new[] { 'A', 'B' }, 0, 2 } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.Write), new object[] { "Hello" } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.WriteAsync), new object[] { 'A' } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.WriteAsync), new object[] { "Hello" } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.WriteAsync), new object[] { new[] { 'A', 'B' }, 0, 2 } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.WriteLineAsync), new object[] { 'A' } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.WriteLineAsync), new object[] { "Hello" } })]
        [InlineData(new object[] { nameof(ConsoleOutputWriter.WriteLineAsync), new object[] { new[] { 'A', 'B' }, 0, 2 } })]
        public async Task Write_WhenInvoked_CapturesInput(string method, object[] args)
        {
            var methodToTest = writer.GetType().GetMethod(
                method,
                args.Select(arg => arg.GetType()).ToArray()
            );

            Assert.Null(writer.GetOutputOrNull());

            // system under test
            var result = methodToTest.Invoke(writer, args);
            if(result is Task t)
            {
                await t;
            }

            var expected = args.First() is IEnumerable<char> sequence
                ? string.Join(string.Empty, sequence)
                : args.First().ToString();

            Assert.StartsWith(
                expected,
                writer.GetOutputOrNull()
            );
        }
    }
}
