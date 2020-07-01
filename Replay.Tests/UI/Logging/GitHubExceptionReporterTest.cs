using Replay.Logging;
using System;
using Xunit;

namespace Replay.Tests.UI.Logging
{
    public class GitHubExceptionReporterTest
    {
        private readonly GitHubExceptionReporter reporter;
        private string reportedUrl;

        public GitHubExceptionReporterTest()
        {
            this.reporter = new GitHubExceptionReporter(url => reportedUrl = url);
        }

        [Fact]
        public void ReportException_ExceptionWithMessage_ReportsToGitHub()
        {
            var exception = new InvalidOperationException("Moon exploded :(");

            reporter.ReportException(new UnhandledExceptionEventArgs(exception, true));

            Assert.StartsWith("https://github.com/waf/replay-csharp/issues/new", reportedUrl);
            Assert.Contains(Uri.EscapeDataString(exception.Message), reportedUrl);
            Assert.Contains(Uri.EscapeDataString(exception.GetType().Name), reportedUrl);
        }

        [Fact]
        public void ReportException_EmptyException_ReportsToGitHub()
        {
            var exception = new Exception();

            reporter.ReportException(new UnhandledExceptionEventArgs(exception, true));

            Assert.StartsWith("https://github.com/waf/replay-csharp/issues/new", reportedUrl);
            Assert.Contains(Uri.EscapeDataString(exception.Message), reportedUrl);
            Assert.Contains(Uri.EscapeDataString(exception.GetType().Name), reportedUrl);
        }
    }
}
