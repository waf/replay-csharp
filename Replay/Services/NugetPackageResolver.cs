using Buildalyzer;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Replay.Services
{
    class NugetPackageResolver
    {
        private readonly string projectDirectory;
        private readonly string projectFile;
        private readonly ProjectAnalyzer analyzer;

        public NugetPackageResolver()
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            this.projectDirectory = Path.Combine(profile, ".replay", "NugetPlaceholder");
            this.projectFile = Path.Combine(projectDirectory, "NugetPlaceholder.csproj");
            Directory.CreateDirectory(projectDirectory);
            RunDotNetCommand("dotnet new classlib --output " + projectDirectory, new StringBuilder(), new StringBuilder());
            analyzer = new AnalyzerManager().GetProject(this.projectFile);
        }

        public NugetInstallationResult AddPackage(string name, string version = null, string source = null)
        {
            string versionCommand = string.IsNullOrEmpty(version) ? string.Empty : "--version " + version;
            string sourceCommand = string.IsNullOrEmpty(source) ? string.Empty : "--source " + source;
            string options = string.Join(" ", new[] { versionCommand, sourceCommand, name });

            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();

            // dotnet add <PROJECT> package [options] <PACKAGE_NAME>
            var exitCode = RunDotNetCommand($"dotnet add {this.projectDirectory} package {options}", stdout, stderr);

            string output = stdout.ToString();
            string error = stderr.ToString();
            return new NugetInstallationResult
            {
                References = exitCode == 0 ? ReadProjectReferences(name) : Array.Empty<MetadataReference>(),
                StandardOutput = stdout.ToString(),
                Exception = GetExceptionFromOutput(output, error)
            };
        }

        private IReadOnlyCollection<MetadataReference> ReadProjectReferences(string name)
        {
            return analyzer.Build()
                .Results.Single()
                .References.Where(reference => reference.Contains(name))
                .Select(file => MetadataReference.CreateFromFile(file))
                .ToArray();
        }

        private static int RunDotNetCommand(string text, StringBuilder outputBuffer, StringBuilder errorBuffer)
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
                return dotnet.ExitCode;
            }
        }

        private static Exception GetExceptionFromOutput(string output, string error)
        {
            output = output.Trim();
            error = error.Trim();

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

            return string.IsNullOrWhiteSpace(error) ? null : new Exception(error);
        }
    }

    class NugetInstallationResult
    {
        /// <summary>
        /// Result of the program
        /// </summary>
        public IReadOnlyCollection<MetadataReference> References { get; set; }

        /// <summary>
        /// Any errors when compiling or running the program
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Standard Output (i.e. stdout, console output) of the program
        /// </summary>
        public string StandardOutput { get; set; }

    }
}
