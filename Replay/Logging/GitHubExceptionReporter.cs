using System;
using System.Linq;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Replay.Logging
{
    public class GitHubExceptionReporter
    {
        private readonly Action<string> showUrl;

        public GitHubExceptionReporter(Action<string> showUrl = null)
        {
            this.showUrl = showUrl ?? (url => Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            }));
        }

        public void ReportException(UnhandledExceptionEventArgs exceptionEvent) =>
            ReportException(exceptionEvent.ExceptionObject is Exception ex
                ? ex
                : new Exception("Unknown Error: " + exceptionEvent.ExceptionObject.ToString())
            );

        public void ReportException(Exception ex)
        {
            var queryString = new NameValueCollection
            {
                { "labels", "bug" },
                { "title", ex.Message },
                { "body", $"Replay is throwing the following {ex.GetType().Name}:\r\n```\r\n{ex}\r\n```\r\n" },
            };

            var url = "https://github.com/waf/replay-csharp/issues/new?"
                + string.Join("&", queryString.AllKeys.Select(key => key + "=" + Uri.EscapeDataString(queryString[key])));

            showUrl(url);
        }
    }
}
