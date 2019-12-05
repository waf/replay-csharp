using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Replay.Services.AssemblyLoading
{
    class DotNetCoreInstallationLocator
    {
        private readonly Func<Process> process;

        public DotNetCoreInstallationLocator(Func<Process> process)
        {
            this.process = process;
        }

        public DotNetCoreInstallation GetReferenceAssemblyPath()
        {
            // I'm really not happy with this implementation, parsing `dotnet --info` is gross.
            // However I can't find a better way than this. TRUSTED_PLATFORM_ASSEMBLIES does not return what we want.

            ProcessStartInfo dotnetInfo = new ProcessStartInfo
            {
                FileName = "dotnet.exe",
                Arguments = "--info",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var proc = process();
            proc.StartInfo = dotnetInfo;
            proc.Start();

            return ReadReferenceAssemblyPath(proc);
        }

        private static DotNetCoreInstallation ReadReferenceAssemblyPath(Process process)
        {
            string basePath = null;
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();

                if (TryGetBasePath(line, out string path))
                {
                    basePath = path;
                }

                if (line.StartsWith("Host"))
                {
                    string versionLine = process.StandardOutput.ReadLine();
                    return TryGetHostVersion(versionLine, out string version)
                        ? new DotNetCoreInstallation(basePath, version)
                        : null;
                }
            }
            return null;
        }

        private static bool TryGetBasePath(string line, out string path)
        {
            if (line.Contains(" Base Path:"))
            {
                int index = line.IndexOf("dotnet") + 6;
                var substring = new Range(" Base Path: ".Length, index);
                path = line[substring].Trim();
                return true;
            }
            path = null;
            return false;
        }

        private static bool TryGetHostVersion(string line, out string version)
        {
            int index = line.IndexOf("Version:");
            if (index == -1)
            {
                version = null;
                return false;
            }
            version = line.Substring(index + 8).Trim();
            return true;
        }
    }
}
