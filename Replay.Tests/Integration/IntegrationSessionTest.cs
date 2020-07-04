using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Replay.Services;
using Replay.Logging;
using Replay.Services.Model;

namespace Replay.Tests.Integration
{
    public class IntegrationScriptTest
    {
        private readonly ReplServices replServices;

        public IntegrationScriptTest()
        {
            this.replServices = new ReplServices(new RealFileIO());
        }

        [Theory]
        [InlineData("Integration/Sessions/SimpleMath.txt")]
        [InlineData("Integration/Sessions/PrettyPrint.txt")]
        [InlineData("Integration/Sessions/AddReference.txt")]
        public async Task RunIntegrationSession(string path)
        {
            var submissions = await ReadSubmissions(path);

            for (int i = 0; i < submissions.Count; i++)
            {
                var submission = submissions[i];
                var result = await replServices.AppendEvaluationAsync(Guid.NewGuid(), submission.FormattedInput, new NullLogger());
                Assert.Equal(submission.Exception, result.Exception);
                Assert.Equal(submission.Result, result.Result);
                Assert.Equal(submission.StandardOutput, result.StandardOutput);
            }
        }

        private static async Task<List<LineEvaluationResult>> ReadSubmissions(string path)
        {
            var text = await File.ReadAllTextAsync(path);
            return Regex.Split(text, "^> ", RegexOptions.Multiline)
                .Where(line => !string.IsNullOrEmpty(line))
                .Select(line =>
                {
                    var lines = line.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                    return new LineEvaluationResult(
                        formattedInput: lines.First(),
                        result: GetRow(lines, "result:"),
                        error: GetRow(lines, "error:"),
                        standardOutput: GetRow(lines, "stdout:")
                    );
                })
                .ToList();
        }

        private static string GetRow(string[] lines, string prefix)
        {
            var row = lines.Single(line => line.StartsWith(prefix)).Substring(prefix.Length);
            return string.IsNullOrWhiteSpace(row) ? null : row.TrimStart();
        }
    }
}
