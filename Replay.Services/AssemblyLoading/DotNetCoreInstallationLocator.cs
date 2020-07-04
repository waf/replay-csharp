using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace Replay.Services.AssemblyLoading
{
    class DotNetCoreInstallationLocator
    {
        private readonly IFileIO io;

        public DotNetCoreInstallationLocator(IFileIO io)
        {
            this.io = io;
        }

        public DotNetCoreInstallation GetReferenceAssemblyPath()
        {
            // This method is highly suspect. It most likely has edge cases that don't work.
            // We need to get the runtime assemblies and associated xml documentation.
            //   - runtime path has assemblies needed for execution.
            //       - example is C:\Program Files\dotnet\shared\Microsoft.NETCore.App\3.1.4
            //   - documentation path has reference assemblies and xml documentation
            //       - example is C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string framework = GetFrameworkVersion();

            return new DotNetCoreInstallation(
                implementationPath: GetRuntimePath(root, framework),
                documentationPath: GetFrameworkPath(root, framework)
            );
        }

        private string GetRuntimePath(string root, string framework)
        {
            var runtimePath = GetLatestFramework(
                Path.Combine(root, "dotnet", "shared", "Microsoft.NETCore.App"),
                framework
            );
            return runtimePath;
        }

        private string GetFrameworkPath(string root, string framework)
        {
            // there are a few ways of getting the current framework name, but not all of them
            // work under xunit. This approach works both at runtime and under xunit.

            var frameworkPath = Path.Combine(
                GetLatestFramework(
                    Path.Combine(root, "dotnet", "packs", "Microsoft.NETCore.App.Ref"),
                    framework
                ),
                "ref"
            );

            return frameworkPath;
        }

        private string GetLatestFramework(string path, string framework) =>
            io.GetDirectories(
                path,
                framework + "*"
            )
            .Last();

        private static string GetFrameworkVersion()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                .OfType<TargetFrameworkAttribute>()
                .Single()
                .FrameworkName
                .Split("=v")
                .Last();
        }
    }
}
