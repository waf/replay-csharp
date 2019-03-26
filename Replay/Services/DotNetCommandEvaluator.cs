using Buildalyzer;
using Replay.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replay.Services
{
    class DotNetCommandEvaluator
    {
        /// <summary>
        /// Set the working directory to a unique folder under ~/.replay
        /// This makes it easier to work with dotnet operations, for example nuget package installation
        /// </summary>
        public string CreateWorkingDirectory()
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sessionDirectory = Path.Combine(profile, ".replay", $"ReplaySession{DateTime.Now:yyyyMMddHHmmss}", "Session");
            Directory.CreateDirectory(sessionDirectory);
            Directory.SetCurrentDirectory(sessionDirectory);
            return sessionDirectory;
        }

        public Task<EvaluationResult> EvaluateAsync(string text)
        {
            var outputBuffer = new StringBuilder();
            var errorBuffer = new StringBuilder();

            RunDotNetCommand(text, outputBuffer, errorBuffer);
            EvaluationResult result = ConvertOutputToEvaluationResult(outputBuffer, errorBuffer);

            return Task.FromResult(result);
        }

        private static void RunDotNetCommand(string text, StringBuilder outputBuffer, StringBuilder errorBuffer)
        {
            if (!text.StartsWith("dotnet ")) throw new ArgumentException("Unexpected dotnet command: " + text, nameof(text));

            using (Process dotnet = new Process())
            {
                dotnet.StartInfo.FileName = "dotnet";
                dotnet.StartInfo.Arguments = text.Substring("dotnet ".Length);
                dotnet.StartInfo.UseShellExecute = false;
                dotnet.StartInfo.CreateNoWindow = true;
                dotnet.StartInfo.RedirectStandardOutput = true;
                dotnet.StartInfo.RedirectStandardError = true;
                dotnet.OutputDataReceived += (sender, args) => outputBuffer.AppendLine(args.Data);
                dotnet.ErrorDataReceived += (sender, args) => errorBuffer.AppendLine(args.Data);
                dotnet.Start();
                dotnet.BeginOutputReadLine();
                dotnet.BeginErrorReadLine();

                dotnet.WaitForExit();

                dotnet.CancelOutputRead();
                dotnet.CancelErrorRead();
            }

            AnalyzerManager manager = new AnalyzerManager();
            ProjectAnalyzer analyzer = manager.GetProject(@"C:\Users\wafuqua\.replay\ReplaySession20190302153809\Session\Session.csproj");
            var result = analyzer.Build();
            ;
        }

        private static EvaluationResult ConvertOutputToEvaluationResult(StringBuilder outputBuffer, StringBuilder errorBuffer)
        {
            string output = outputBuffer.ToString().Trim();
            string error = errorBuffer.ToString().Trim();

            var errorsReportedToStandardOutput = string.Join(
                Environment.NewLine,
                output
                    .Split(Environment.NewLine.ToCharArray())
                    .Where(line => line.StartsWith("error: "))
            );

            if (errorsReportedToStandardOutput != string.Empty)
            {
                error = error + Environment.NewLine + errorsReportedToStandardOutput;
            }

            return new EvaluationResult
            {
                StandardOutput = output,
                Exception = string.IsNullOrWhiteSpace(error) ? null : new Exception(error),
                ScriptResult = null
            };
        }
    }
}
